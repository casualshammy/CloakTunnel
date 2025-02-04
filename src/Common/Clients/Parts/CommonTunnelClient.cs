using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Data;
using CloakTunnel.Common.Interfaces;
using CloakTunnel.Common.Toolkit;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CloakTunnel.Common.Clients.Parts;

public abstract class CommonTunnelClient : ITunnelClient
{
  private readonly ReplaySubject<TunnelStat> p_stats;
  private readonly ConcurrentDictionary<EndPoint, ITunnelClientInfo> p_clients = new();
  protected readonly ILog p_log;
  protected readonly TunnelClientOptions p_options;
  protected readonly IReadOnlyLifetime p_lifetime;
  protected ulong p_byteRecvCount;
  private ulong p_byteSentCount;

  public CommonTunnelClient(
    TunnelClientOptions _options,
    IReadOnlyLifetime _lifetime,
    ILog _log)
  {
    p_log = _log;
    p_options = _options;
    p_lifetime = _lifetime;

    p_stats = _lifetime.ToDisposeOnEnded(new ReplaySubject<TunnelStat>(1));

    var log = p_log["udp-input"];
    var bindEndpoint = IPEndPoint.Parse($"{p_options.BindUri.Host}:{p_options.BindUri.Port}");
    var inputUdpSocket = _lifetime.ToDisposeOnEnded(new Socket(bindEndpoint.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp));
    try
    {
      log.Info($"**Binding** to __{bindEndpoint}__...");
      inputUdpSocket.Bind(bindEndpoint);
      log.Info($"**Bond** to __{bindEndpoint}__");
    }
    catch (SocketException sex) when (sex.SocketErrorCode == SocketError.AddressAlreadyInUse)
    {
      log.Error($"Can't bind to address {bindEndpoint}: already in use");
      return;
    }
    catch (Exception ex)
    {
      log.Error($"Can't bind to address {bindEndpoint}: {ex.Message}");
      return;
    }

    var localServiceSocketRoutineThread = new Thread(() => CreateInputUdpSocketRoutine(_lifetime, log, inputUdpSocket, _options)) { Priority = ThreadPriority.Highest };
    localServiceSocketRoutineThread.Start();

    Observable
      .Interval(TimeSpan.FromSeconds(1))
      .StartWithDefault()
      .Scan((Sent: 0UL, Recv: 0UL, Timestamp: 0L), (_acc, _) =>
      {
        var tickCount = Environment.TickCount64;
        var tx = Interlocked.Read(ref p_byteSentCount);
        var rx = Interlocked.Read(ref p_byteRecvCount);

        var delta = TimeSpan.FromMilliseconds(tickCount - _acc.Timestamp).TotalSeconds;
        if (delta == 0d)
          return _acc;

        var txDelta = (ulong)((tx - _acc.Sent) / delta);
        var rxDelta = (ulong)((rx - _acc.Recv) / delta);
        p_stats.OnNext(new TunnelStat(txDelta, rxDelta, tx, rx));

        return (tx, rx, tickCount);
      })
      .Subscribe(_lifetime);

    Observable
      .Interval(TimeSpan.FromSeconds(60))
      .Subscribe(__ =>
      {
        var now = Environment.TickCount64;
        foreach (var (endPoint, clientInfo) in p_clients)
          if (now - clientInfo.Timestamp > 2 * 60 * 1000)
            if (p_clients.TryRemove(endPoint, out var removedClientInfo))
            {
              removedClientInfo.Lifetime.End();
              p_log.Info($"Client '{endPoint}' is disconnected due to inactivity; total clients: {p_clients.Count}");
            }
      }, _lifetime);
  }

  public IObservable<TunnelStat> Stats => p_stats;

  public void DropAllClients()
  {
    p_log.Warn($"Requested to drop all output (forward) connections...");
    foreach (var (endPoint, _) in p_clients)
      if (p_clients.TryRemove(endPoint, out var removedClientInfo))
        removedClientInfo.Lifetime.End();

    p_log.Warn($"All output (forward) connections are dropped");
  }

  protected abstract ITunnelClientInfo AllocateNewClient(
    Socket _inputUdpSocket,
    EndPoint _inputClientEndpoint);

  protected void CreateInputUdpSocketRoutine(
    IReadOnlyLifetime _lifetime,
    ILog _log,
    Socket _inputUdpSocket,
    TunnelClientOptions _tunnelOptions)
  {
    Span<byte> buffer = new byte[128 * 1024];
    EndPoint inputClientEndpoint = new IPEndPoint(IPAddress.Any, short.MaxValue);
    var cryptor = EncryptionToolkit.GetCrypto(_tunnelOptions.Encryption, _lifetime, _tunnelOptions.PassKey);

    _log.Info($"**Input socket routine** is __started__");

    while (!_lifetime.IsCancellationRequested)
    {
      try
      {
        var receivedBytes = _inputUdpSocket.ReceiveFrom(buffer, SocketFlags.None, ref inputClientEndpoint);
        if (receivedBytes > 0)
        {
          var dataToSend = cryptor.Encrypt(buffer[..receivedBytes]);

          if (!p_clients.TryGetValue(inputClientEndpoint, out var client))
          {
            _log.Info($"**New client** __'{inputClientEndpoint}'__ is **trying to connect**...");
            p_clients.TryAdd(inputClientEndpoint, client = AllocateNewClient(_inputUdpSocket, inputClientEndpoint));
            _log.Info($"Client __'{inputClientEndpoint}'__ **connected**; total clients: {p_clients.Count}");
          }

          client.Timestamp = Environment.TickCount64;
          client.Send(dataToSend);
          Interlocked.Add(ref p_byteSentCount, (ulong)dataToSend.Length);
        }
      }
      catch (SocketException sex) when (sex.ErrorCode == 10051 || sex.ErrorCode == 101) // Network unavailable
      {
        _log.Warn($"Host/network unreachable (code: {sex.ErrorCode})");
      }
      catch (SocketException sex) when (sex.ErrorCode == 10004 || sex.ErrorCode == 4) // Interrupted function call
      {
        // ignore (caused by client disconnection)
        _log.Warn($"Interrupted function call (code: {sex.ErrorCode})");
      }
      catch (SocketException sex) when (sex.ErrorCode == 10054) // Connection reset by peer.
      {
        // ignore (caused by client disconnection)
        _log.Warn($"Connection reset by peer (code: {sex.ErrorCode})");
      }
      catch (SocketException sex)
      {
        _log.Error($"SocketException error (code: {sex.ErrorCode})", sex);
      }
      catch (Exception ex)
      {
        _log.Error("Generic error", ex);
      }
    }

    _log.Info($"**Input socket routine** is __ended__");
  }

}
