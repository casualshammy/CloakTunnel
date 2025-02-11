using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Data;
using CloakTunnel.Server.Modules.Servers.Parts;
using CloakTunnel.Server.Modules.WebServer;
using CloakTunnel.Server.Modules.WebSocketController.Parts;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace CloakTunnel.Server.Modules.Servers;

public class WsTunnelServer : CommonTunnelServer
{
  protected readonly ConcurrentDictionary<Guid, ClientInfo> p_clients = new();
  private readonly WsServer p_ws;

  public WsTunnelServer(
    TunnelServerOptions _options,
    IReadOnlyLifetime _lifetime,
    ILog _logger) : base(_lifetime, _logger, _options)
  {
    _lifetime.DoOnEnded(() => _logger.Info($"**Websocket tunnel** is __closed__"));

    var webServer = new WebServerImpl(_options, _logger, _lifetime);
    p_ws = webServer.WsServer;

    CreateInputWsMsgRoutine(_lifetime);

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

  private void CreateInputWsMsgRoutine(
    IReadOnlyLifetime _lifetime)
  {
    Span<byte> buffer = new byte[128 * 1024];
    EndPoint remoteClientEndpoint = new IPEndPoint(IPAddress.Any, short.MaxValue);
    var remoteEndpoint = IPEndPoint.Parse($"{p_options.ForwardUri.Host}:{p_options.ForwardUri.Port}");
    var log = p_log["ws-input"];

    log.Info($"**Input ws routine** is __started__");
    _lifetime.DoOnEnded(() => log.Info($"**Input ws routine** is __ended__"));

    p_ws.IncomingMessages
      .Subscribe(_msg =>
      {
        try
        {
          Interlocked.Add(ref p_byteRecvCount, (ulong)_msg.Data.Length);

          if (!p_clients.TryGetValue(_msg.Session.Id, out var client))
          {
            log.Info($"**New client** __'{_msg.Session.Id}'__ is **trying to connect**...");

            var newClient = AllocateNewClient(
              new WsTunnelServerClientInfo(p_ws, _msg.Session),
              _msg.Data,
              out var encryption);

            if (newClient == null)
            {
              p_clientUnknownEncryptionSubj.OnNext(_msg.Session.Id.ToString());
              return;
            }

            p_clients.TryAdd(_msg.Session.Id, client = newClient);
            log.Info($"Client __'{_msg.Session.Id}'__ **connected** (enc: {encryption}); total clients: {p_clients.Count}");
          }

          var dataToSend = client.Decryptor.Decrypt(_msg.Data);
          client.Timestamp = Environment.TickCount64;
          client.Socket.SendTo(dataToSend, SocketFlags.None, remoteEndpoint);
        }
        catch (SocketException sex) when (sex.ErrorCode == 10004 || sex.ErrorCode == 4) // Interrupted function call
        {
          // ignore (caused be client cleanup)
          log.Warn($"Interrupted function call (code: {sex.ErrorCode})");
        }
        catch (SocketException sex) when (sex.ErrorCode == 10054) // Connection reset by peer.
        {
          // ignore (caused by client disconnection)
          log.Warn($"Connection reset by peer (code: {sex.ErrorCode})");
        }
        catch (SocketException sexi)
        {
          log.Error($"SocketException error (code: {sexi.ErrorCode})", sexi);
        }
        catch (Exception ex)
        {
          log.Error($"Generic error", ex);
        }
      });
  }
  
}
