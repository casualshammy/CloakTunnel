using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Data;
using CloakTunnel.Server.Modules.Servers.Parts;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace CloakTunnel.Server.Modules.Servers;

public class UdpTunnelServer : CommonTunnelServer
{
  protected readonly ConcurrentDictionary<EndPoint, ClientInfo> p_clients = new();

  public UdpTunnelServer(
    TunnelServerOptions _options,
    IReadOnlyLifetime _lifetime,
    ILog _logger) : base(_lifetime, _logger, _options)
  {
    _lifetime.DoOnEnded(() => _logger.Info($"**Udp tunnel** is __closed__"));

    var log = p_log["udp-input"];
    var bindEndpoint = IPEndPoint.Parse($"{p_options.BindUri.Host}:{p_options.BindUri.Port}");
    var inputSocket = _lifetime.ToDisposeOnEnded(new Socket(bindEndpoint.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp));
    try
    {
      log.Info($"**Binding** to __{bindEndpoint}__...");
      inputSocket.Bind(bindEndpoint);
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

    var remoteClientsRoutineThread = new Thread(() => CreateInputSocketRoutine(_lifetime, log, inputSocket)) { Priority = ThreadPriority.Highest };
    remoteClientsRoutineThread.Start();

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

  private void CreateInputSocketRoutine(
    IReadOnlyLifetime _lifetime,
    ILog _log,
    Socket _inputSocket)
  {
    Span<byte> buffer = new byte[128 * 1024];
    EndPoint inputClientEndpoint = new IPEndPoint(IPAddress.Any, short.MaxValue);
    var forwardEndpoint = IPEndPoint.Parse($"{p_options.ForwardUri.Host}:{p_options.ForwardUri.Port}");

    _log.Info($"**Input socket routine** is __started__");

    while (!_lifetime.IsCancellationRequested)
    {
      try
      {
        var receivedBytes = _inputSocket.ReceiveFrom(buffer, SocketFlags.None, ref inputClientEndpoint);
        if (receivedBytes > 0)
        {
          Interlocked.Add(ref p_byteRecvCount, (ulong)receivedBytes);
          var receivedSpan = buffer[..receivedBytes];

          if (!p_clients.TryGetValue(inputClientEndpoint, out var client))
          {
            _log.Info($"**New client** __'{inputClientEndpoint}'__ is **trying to connect**...");

            var newClient = AllocateNewClient(
              new UdpTunnelServerClientInfo(_inputSocket, inputClientEndpoint), 
              receivedSpan, 
              out var alg);

            if (newClient == null)
            {
              p_clientUnknownEncryptionSubj.OnNext($"{inputClientEndpoint}");
              continue;
            }

            p_clients.TryAdd(inputClientEndpoint, client = newClient);
            _log.Info($"Client __'{inputClientEndpoint}'__ **connected**; total clients: {p_clients.Count}");
          }

          var dataToSend = client.Decryptor.Decrypt(receivedSpan);
          client.Timestamp = Environment.TickCount64;
          client.Socket.SendTo(dataToSend, SocketFlags.None, forwardEndpoint);
        }
      }
      catch (SocketException sex) when (sex.ErrorCode == 10004 || sex.ErrorCode == 4) // Interrupted function call
      {
        // ignore (caused be client cleanup)
        _log.Warn($"Interrupted function call (code: {sex.ErrorCode})");
      }
      catch (SocketException sex) when (sex.ErrorCode == 10054) // Connection reset by peer.
      {
        // ignore (caused by client disconnection)
        _log.Warn($"Connection reset by peer (code: {sex.ErrorCode})");
      }
      catch (SocketException sexi)
      {
        _log.Error($"SocketException error (code: {sexi.ErrorCode})", sexi);
      }
      catch (Exception ex)
      {
        _log.Error($"Generic error", ex);
      }
    }

    _log.Info($"**Input socket routine** is __ended__");
  }

}
