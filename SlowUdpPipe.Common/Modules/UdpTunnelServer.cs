using Ax.Fw.Crypto;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Data;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace SlowUdpPipe.Common.Modules;

public class UdpTunnelServer
{
  record ClientInfo(Socket Socket, ILifetime Lifetime, ICryptoAlgorithm Decryptor)
  {
    public long Timestamp { get; set; }
  };

  private readonly UdpTunnelServerOptions p_options;
  private readonly IReadOnlyLifetime p_lifetime;
  private readonly ILogger p_logger;
  private readonly ConcurrentDictionary<EndPoint, ClientInfo> p_clients = new();
  private readonly ReplaySubject<UdpTunnelStat> p_stats;
  private readonly Subject<EndPoint> p_clientUnknownEncryptionSubj;
  private ulong p_byteRecvCount;
  private ulong p_byteSentCount;

  public UdpTunnelServer(
    UdpTunnelServerOptions _options,
    IReadOnlyLifetime _lifetime,
    ILogger _logger)
  {
    _lifetime.DoOnEnded(() => _logger.Info($"Udp tunnel is closed"));

    p_options = _options;
    p_lifetime = _lifetime;
    p_logger = _logger;
    p_stats = _lifetime.ToDisposeOnEnded(new ReplaySubject<UdpTunnelStat>(1));
    p_clientUnknownEncryptionSubj = _lifetime.ToDisposeOnEnded(new Subject<EndPoint>());

    var acceptClientsSocket = _lifetime.ToDisposeOnEnded(new Socket(p_options.Local.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp));
    try
    {
      acceptClientsSocket.Bind(p_options.Local);
    }
    catch (SocketException sex) when (sex.SocketErrorCode == SocketError.AddressAlreadyInUse)
    {
      p_logger.Error($"Can't bind to address {p_options.Local}: already in use");
      return;
    }
    catch (Exception ex)
    {
      p_logger.Error($"Can't bind to address {p_options.Local}: {ex.Message}");
      return;
    }

    var remoteClientsRoutineThread = new Thread(() => CreateAcceptClientsListener(acceptClientsSocket, _lifetime)) { Priority = ThreadPriority.Highest };
    remoteClientsRoutineThread.Start();

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
        p_stats.OnNext(new UdpTunnelStat(txDelta, rxDelta));

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
              p_logger.Info($"[{endPoint}] Client is disconnected due to inactivity; total clients: {p_clients.Count}");
            }
      }, _lifetime);

    p_clientUnknownEncryptionSubj
      .Buffer(TimeSpan.FromSeconds(5))
      .Subscribe(_errors =>
      {
        if (!_errors.Any())
          return;

        foreach (var endpoint in _errors.Distinct())
          _logger.Warn($"[{endpoint}] Client tried to connect with unknown encryption algorithm (or it wasn't a slowudppipe client)");
      }, _lifetime);
  }

  public IObservable<UdpTunnelStat> Stats => p_stats;

  private void CreateAcceptClientsListener(
    Socket _remoteClientsSocket,
    IReadOnlyLifetime _lifetime)
  {
    Span<byte> buffer = new byte[128 * 1024];
    EndPoint remoteClientEndpoint = new IPEndPoint(IPAddress.Any, short.MaxValue);
    var remoteEndpoint = p_options.Remote;

    p_logger.Info($"Accept clients listener is started");

    while (!_lifetime.IsCancellationRequested)
    {
      try
      {
        var receivedBytes = _remoteClientsSocket.ReceiveFrom(buffer, SocketFlags.None, ref remoteClientEndpoint);
        if (receivedBytes > 0)
        {
          Interlocked.Add(ref p_byteRecvCount, (ulong)receivedBytes);
          var receivedSpan = buffer.Slice(0, receivedBytes);

          if (!p_clients.TryGetValue(remoteClientEndpoint, out var client))
          {
            var newClient = AllocateNewClient(_remoteClientsSocket, remoteClientEndpoint, receivedSpan, out var alg);
            if (newClient == null)
            {
              p_clientUnknownEncryptionSubj.OnNext(remoteClientEndpoint);
              continue;
            }

            p_clients.TryAdd(remoteClientEndpoint, client = newClient);
            p_logger.Info($"[{remoteClientEndpoint}] Client connected (alg: {alg}); total clients: {p_clients.Count}");
          }

          var dataToSend = client.Decryptor.Decrypt(receivedSpan);
          client.Timestamp = Environment.TickCount64;
          client.Socket.SendTo(dataToSend, SocketFlags.None, remoteEndpoint);
        }
      }
      catch (SocketException sex0) when (sex0.ErrorCode == 10004 || sex0.ErrorCode == 4) // Interrupted function call
      {
        // ignore (caused be client cleanup)
      }
      catch (SocketException sex) when (sex.ErrorCode == 10054) // Connection reset by peer.
      {
        // ignore (caused by client disconnection)
      }
      catch (SocketException sexi)
      {
        p_logger.Error($"Accept clients listener SocketException error; code: '{sexi.ErrorCode}'", sexi);
      }
      catch (Exception ex)
      {
        p_logger.Error("Accept clients listener error", ex);
      }
    }

    p_logger.Info($"Accept clients listener is ended");
  }

  private void CreateLocalServiceRoutine(
    Socket _localServiceSocket,
    Socket _remoteClientsSocket,
    EndPoint _remoteClientEndpoint,
    EncryptionAlgorithm _algorithm,
    IReadOnlyLifetime _lifetime)
  {
    Span<byte> buffer = new byte[128 * 1024];
    EndPoint localServiceEndpoint = new IPEndPoint(IPAddress.Any, short.MaxValue);
    var encryptor = GetCrypto(_lifetime, _algorithm);

    p_logger.Info($"[{_remoteClientEndpoint}] Local service tunnel is started");

    while (!_lifetime.IsCancellationRequested)
    {
      try
      {
        if (!_localServiceSocket.IsBound)
        {
          Thread.Sleep(100);
          continue;
        }

        var receivedBytes = _localServiceSocket.ReceiveFrom(buffer, SocketFlags.None, ref localServiceEndpoint);
        if (receivedBytes > 0)
        {
          var dataToSend = encryptor.Encrypt(buffer.Slice(0, receivedBytes));
          _remoteClientsSocket.SendTo(dataToSend, SocketFlags.None, _remoteClientEndpoint);
          Interlocked.Add(ref p_byteSentCount, (ulong)dataToSend.Length);
        }
      }
      catch (SocketException sex) when (sex.ErrorCode == 10004 || sex.ErrorCode == 4) // Interrupted function call
      {
        // ignore (caused by client cleanup)
      }
      catch (SocketException sexi)
      {
        p_logger.Error($"Local service tunnel SocketException error; code: '{sexi.ErrorCode}'", sexi);
      }
      catch (Exception ex)
      {
        p_logger.Error("Local service tunnel error", ex);
      }
    }

    p_logger.Info($"[{_remoteClientEndpoint}] Local service tunnel routine is ended");
  }

  private ClientInfo? AllocateNewClient(
    Socket _remoteClientsSocket,
    EndPoint _remoteClientEndpoint,
    Span<byte> _firstChunk,
    out EncryptionAlgorithm _encryptionAlgorithm)
  {
    var lifetime = p_lifetime.GetChildLifetime();
    if (lifetime != null)
    {
      if (!TryGuessEncryptionAlgorithm(_firstChunk, out EncryptionAlgorithm? alg))
      {
        _encryptionAlgorithm = EncryptionAlgorithm.None;
        return null;
      }

      var decryptor = GetCrypto(lifetime, alg.Value);

      var localServiceSocket = lifetime.ToDisposeOnEnding(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp));
      var thread = new Thread(() => CreateLocalServiceRoutine(
                   localServiceSocket,
                   _remoteClientsSocket,
                   _remoteClientEndpoint,
                   alg.Value,
                   lifetime))
      { Priority = ThreadPriority.Highest };
      thread.Start();

      _encryptionAlgorithm = alg.Value;
      return new(localServiceSocket, lifetime, decryptor) { Timestamp = Environment.TickCount64 };
    }

    throw new OperationCanceledException();
  }

  private ICryptoAlgorithm GetCrypto(IReadOnlyLifetime _lifetime, EncryptionAlgorithm _alg)
  {
    return _alg switch
    {
      EncryptionAlgorithm.Aes128 => new AesCbc(_lifetime, p_options.Key, 128),
      EncryptionAlgorithm.Aes256 => new AesCbc(_lifetime, p_options.Key, 256),
      EncryptionAlgorithm.AesGcm128 => new AesWithGcm(_lifetime, p_options.Key, 128),
      EncryptionAlgorithm.AesGcm256 => new AesWithGcm(_lifetime, p_options.Key, 256),
      EncryptionAlgorithm.AesGcmObfs128 => new AesWithGcmObfs(_lifetime, p_options.Key, Consts.MAX_UDP_PACKET_PAYLOAD_SIZE, 128),
      EncryptionAlgorithm.AesGcmObfs256 => new AesWithGcmObfs(_lifetime, p_options.Key, Consts.MAX_UDP_PACKET_PAYLOAD_SIZE, 256),
      EncryptionAlgorithm.ChaCha20Poly1305 => new ChaCha20WithPoly1305(_lifetime, p_options.Key),
      EncryptionAlgorithm.Xor => new Xor(Encoding.UTF8.GetBytes(p_options.Key)),
      _ => throw new InvalidOperationException($"Crypto algorithm is not specified!"),
    };
  }

  private bool TryGuessEncryptionAlgorithm(Span<byte> _span, [NotNullWhen(true)] out EncryptionAlgorithm? _algo)
  {
    _algo = null;

    using var lifetime = p_lifetime.GetChildLifetime();
    if (lifetime == null)
      return false;

    var key = p_options.Key;
    var algorithms = new Dictionary<EncryptionAlgorithm, Func<ICryptoAlgorithm>>()
    {
      { EncryptionAlgorithm.Aes128, () => new AesCbc(lifetime, key, 128) },
      { EncryptionAlgorithm.Aes256,() =>  new AesCbc(lifetime, key, 256) },
      { EncryptionAlgorithm.AesGcm128,() =>  new AesWithGcm(lifetime, key, 128) },
      { EncryptionAlgorithm.AesGcm256, () => new AesWithGcm(lifetime, key, 256) },
      { EncryptionAlgorithm.AesGcmObfs128, () => new AesWithGcmObfs(lifetime, key, Consts.MAX_UDP_PACKET_PAYLOAD_SIZE, 128) },
      { EncryptionAlgorithm.AesGcmObfs256, () => new AesWithGcmObfs(lifetime, key, Consts.MAX_UDP_PACKET_PAYLOAD_SIZE, 256) },
      { EncryptionAlgorithm.ChaCha20Poly1305,() => new ChaCha20WithPoly1305(lifetime, key) },
      { EncryptionAlgorithm.Xor, () => new Xor(Encoding.UTF8.GetBytes(key)) }
    };

    foreach (var (algName, algFactory) in algorithms)
    {
      if (!p_options.Algorithms.Contains(algName))
        continue;

      try
      {
        var alg = algFactory();
        alg.Decrypt(_span);
        _algo = algName;
        return true;
      }
      catch { }
    }

    return false;
  }

}
