using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Data;
using CloakTunnel.Common.Toolkit;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CloakTunnel.Server.Modules.Servers.Parts;

public abstract class CommonTunnelServer
{
  protected readonly ILog p_log;
  protected readonly IReadOnlyLifetime p_lifetime;
  protected readonly TunnelServerOptions p_options;
  protected readonly ReplaySubject<TunnelStat> p_stats;
  protected readonly Subject<string> p_clientUnknownEncryptionSubj;
  protected ulong p_byteRecvCount;
  protected ulong p_byteSentCount;

  public CommonTunnelServer(
    IReadOnlyLifetime _lifetime,
    ILog _log,
    TunnelServerOptions _options)
  {
    p_lifetime = _lifetime;
    p_log = _log;
    p_options = _options;
    p_stats = _lifetime.ToDisposeOnEnded(new ReplaySubject<TunnelStat>(1));
    p_clientUnknownEncryptionSubj = _lifetime.ToDisposeOnEnded(new Subject<string>());

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

    p_clientUnknownEncryptionSubj
      .Buffer(TimeSpan.FromSeconds(5))
      .Subscribe(_errors =>
      {
        if (!_errors.Any())
          return;

        foreach (var client in _errors.Distinct())
          _log.Warn($"Client '{client}' tried to connect with unknown encryption algorithm (or it wasn't a {nameof(CloakTunnel)} client)");
      }, _lifetime);
  }

  public IObservable<TunnelStat> Stats => p_stats;

  protected ClientInfo? AllocateNewClient(
    ITunnelServerClientInfo _tunnelServerClientInfo,
    Span<byte> _firstChunk,
    out EncryptionAlgorithm _encryptionAlgorithm)
  {
    var lifetime = p_lifetime.GetChildLifetime();
    if (lifetime != null)
    {
      if (!TryGuessEncryption(_firstChunk, out var _algo))
      {
        _encryptionAlgorithm = EncryptionAlgorithm.None;
        return null;
      }

      var decryptor = EncryptionToolkit.GetCrypto(_algo, lifetime, p_options.PassKey);

      var localServiceSocket = lifetime.ToDisposeOnEnding(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp));
      var thread = new Thread(() => CreateForwardSocketRoutine(
                   localServiceSocket,
                   _tunnelServerClientInfo,
                   _algo,
                   lifetime))
      { Priority = ThreadPriority.Highest };
      thread.Start();

      _encryptionAlgorithm = _algo;
      return new(localServiceSocket, lifetime, decryptor) { Timestamp = Environment.TickCount64 };
    }

    throw new OperationCanceledException();
  }

  private bool TryGuessEncryption(
    Span<byte> _span, 
    out EncryptionAlgorithm _encryption)
  {
    using var lifetime = p_lifetime.GetChildLifetime();
    if (lifetime == null)
    {
      _encryption = EncryptionAlgorithm.None;
      return false;
    }

    foreach (var algo in EncryptionToolkit.ALL_CYPHERS)
    {
      try
      {
        var decryptor = EncryptionToolkit.GetCrypto(algo, lifetime, p_options.PassKey);
        decryptor.Decrypt(_span);
        _encryption = algo;
        return true;
      }
      catch { }
    }

    _encryption = EncryptionAlgorithm.None;
    return false;
  }

  private void CreateForwardSocketRoutine(
    Socket _forwardSocket,
    ITunnelServerClientInfo _clientInfo,
    EncryptionAlgorithm _algorithm,
    IReadOnlyLifetime _lifetime)
  {
    Span<byte> buffer = new byte[128 * 1024];
    EndPoint _ = new IPEndPoint(IPAddress.Any, short.MaxValue);
    var encryptor = EncryptionToolkit.GetCrypto(_algorithm, _lifetime, p_options.PassKey);
    var log = p_log["udp-output"][_clientInfo.ClientId];

    log.Info($"**Forward socket routine** is __started__");

    while (!_lifetime.IsCancellationRequested)
    {
      try
      {
        if (!_forwardSocket.IsBound)
        {
          log.Warn($"Can't connect to forward address: socket is not bound");
          Thread.Sleep(1000);
          continue;
        }

        var receivedBytes = _forwardSocket.ReceiveFrom(buffer, SocketFlags.None, ref _);
        if (receivedBytes > 0)
        {
          var dataToSend = encryptor.Encrypt(buffer[..receivedBytes]);
          _clientInfo.Send(dataToSend);
          Interlocked.Add(ref p_byteSentCount, (ulong)dataToSend.Length);
        }
      }
      catch (SocketException sex) when (sex.ErrorCode == 10004 || sex.ErrorCode == 4) // Interrupted function call
      {
        // ignore (caused by client cleanup)
        log.Warn($"Interrupted function call (code: {sex.ErrorCode})");
      }
      catch (SocketException sex)
      {
        log.Error($"SocketException error (code: {sex.ErrorCode})", sex);
      }
      catch (Exception ex)
      {
        log.Error($"Generic error", ex);
      }
    }

    log.Info($"**Forward socket routine** is __ended__");
  }

}
