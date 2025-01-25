using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CloakTunnel.Server.Modules.WebSocketController.Parts;

internal class WsServer
{
  record EnqueuedMsg(WebSocketSession Session, ReadOnlyMemory<byte> Data);

  private readonly Subject<WsIncomingMsg> p_incomingMsgs = new();
  private readonly Subject<WebSocketSession> p_clientConnectedFlow = new();
  private readonly Subject<EnqueuedMsg> p_enqueuedMsgSubj = new();
  private readonly IReadOnlyLifetime p_lifetime;
  private readonly int p_connectionMaxIdleTimeMs;
  private readonly Action<string>? p_onError;
  private readonly ConcurrentDictionary<int, WebSocketSession> p_sessions = new();
  private int p_sessionsCount = 0;

  public WsServer(
    IReadOnlyLifetime _lifetime,
    int _connectionMaxIdleTimeMs = 60 * 60 * 1000,
    Action<string>? _onError = null)
  {
    p_lifetime = _lifetime;
    p_connectionMaxIdleTimeMs = _connectionMaxIdleTimeMs;
    p_onError = _onError;

    var enqueuedMsgScheduler = new EventLoopScheduler();

    p_enqueuedMsgSubj
      .ObserveOn(enqueuedMsgScheduler)
      .SelectAsync(async (_, _ct) => await SendMsgAsync(_.Session, _.Data, _ct), enqueuedMsgScheduler)
      .Subscribe(_lifetime);
  }

  public IObservable<WsIncomingMsg> IncomingMessages => p_incomingMsgs;
  public IObservable<WebSocketSession> ClientConnected => p_clientConnectedFlow;
  public IReadOnlyList<WebSocketSession> Sessions => new List<WebSocketSession>(p_sessions.Values);

  public async Task<bool> AcceptSocketAsync(Guid _id, WebSocket _webSocket)
  {
    if (_webSocket.State != WebSocketState.Open)
      return false;

    var session = new WebSocketSession(_id, _webSocket);
    using var semaphore = new SemaphoreSlim(0, 1);
    using var scheduler = new EventLoopScheduler();

    scheduler.ScheduleAsync(async (_s, _ct) => await CreateNewLoopAsync(session, semaphore));

    try
    {
      await semaphore.WaitAsync(p_lifetime.Token);
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
      p_onError?.Invoke($"Waiting for loop is failed: {ex}");
    }

    return true;
  }

  public async Task SendMsgAsync(
    WebSocketSession _session,
    ReadOnlyMemory<byte> _msg,
    CancellationToken _ct)
  {
    try
    {
      if (_session.Socket.State == WebSocketState.Open)
        await _session.Socket.SendAsync(_msg, WebSocketMessageType.Binary, true, _ct);
    }
    catch (Exception ex)
    {
      p_onError?.Invoke($"Can't send msg to socket: {ex}");
    }
  }

  public void EnqueueMsg(
    WebSocketSession _session,
    byte[] _msg) => p_enqueuedMsgSubj.OnNext(new EnqueuedMsg(_session, _msg));

  private async Task CreateNewLoopAsync(
    WebSocketSession _session,
    SemaphoreSlim _completeSignal)
  {
    var session = _session;
    var sessionIndex = Interlocked.Increment(ref p_sessionsCount);
    p_sessions.TryAdd(sessionIndex, session);

    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(p_connectionMaxIdleTimeMs));

    WebSocketReceiveResult? receiveResult = null;

    var buffer = ArrayPool<byte>.Shared.Rent(100 * 1024);
    var totalRecieved = 0L;

    try
    {
      p_clientConnectedFlow.OnNext(session);

      receiveResult = await session.Socket.ReceiveAsync(buffer, cts.Token);

      while (!receiveResult.CloseStatus.HasValue && !cts.IsCancellationRequested)
      {
        cts.CancelAfter(TimeSpan.FromMilliseconds(p_connectionMaxIdleTimeMs));

        try
        {
          p_incomingMsgs.OnNext(new WsIncomingMsg(_session, buffer[..receiveResult.Count]));
          totalRecieved += receiveResult.Count;
        }
        finally
        {
          receiveResult = await session.Socket.ReceiveAsync(buffer, cts.Token);
        }
      }
    }
    catch (OperationCanceledException)
    {
      // don't care
    }
    catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
    {
      // don't care
    }
    catch (Exception ex)
    {
      p_onError?.Invoke($"Error occured in loop: {ex}");
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(buffer, false);
      p_sessions.TryRemove(sessionIndex, out _);
    }

    try
    {
      if (session.Socket.State == WebSocketState.Open)
      {
        if (receiveResult is not null)
          await session.Socket.CloseAsync(receiveResult.CloseStatus ?? WebSocketCloseStatus.NormalClosure, receiveResult.CloseStatusDescription, CancellationToken.None);
        else
          await session.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, $"Closed normally (session: '{sessionIndex}')", CancellationToken.None);
      }
    }
    catch (Exception ex)
    {
      p_onError?.Invoke($"Error occured while closing websocket: {ex}");
    }

    _completeSignal.Release();
  }

}
