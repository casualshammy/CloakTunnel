using Ax.Fw;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Clients.Parts;
using CloakTunnel.Common.Data;
using CloakTunnel.Common.Toolkit;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using Websocket.Client;

namespace CloakTunnel.Common.Clients;

public class WsTunnelClient : CommonTunnelClient
{
  public WsTunnelClient(
    TunnelClientOptions _options,
    IReadOnlyLifetime _lifetime,
    ILog _logger) : base(_options, _lifetime, _logger)
  { }

  private async Task CreateOutputRoutineAsync(
    WebsocketClient _wsClient,
    Socket _inputUdpSocket,
    EndPoint _inputClientEndpoint,
    IReadOnlyLifetime _lifetime)
  {
    var cryptor = EncryptionToolkit.GetCrypto(p_options.Encryption, _lifetime, p_options.PassKey);
    var log = p_log["ws-output"][$"{_inputClientEndpoint}"];

    log.Info($"**Output ws routine** is __started__");
    _lifetime.DoOnEnded(() => log.Info($"**Output ws routine** is __ended__"));

    await _wsClient.StartOrFail();

    _wsClient.MessageReceived
      .Subscribe(_msg =>
      {
        try
        {
          Span<byte> bytes = _msg.Binary;
          if (bytes == null)
            return;

          Interlocked.Add(ref p_byteRecvCount, (ulong)bytes.Length);
          var dataToSend = cryptor.Decrypt(bytes);
          _inputUdpSocket.SendTo(dataToSend, SocketFlags.None, _inputClientEndpoint);
        }
        catch (SocketException sex0) when (sex0.ErrorCode == 10004) // Interrupted function call
        {
          // ignore (caused by client cleanup)
          log.Warn($"Interrupted function call (code: {sex0.ErrorCode})");
        }
        catch (SocketException sex1) when (sex1.ErrorCode == 10054) // Connection reset by peer
        {
          log.Error($"Host is not responding");
        }
        catch (SocketException sex)
        {
          log.Error($"SocketException error (code: {sex.ErrorCode})", sex);
        }
        catch (Exception ex)
        {
          log.Error("Generic error", ex);
        }
      }, p_lifetime);
  }

  protected override ITunnelClientInfo AllocateNewClient(
    Socket _localServiceSocket,
    EndPoint _localServiceEndpoint)
  {
    var lifetime = p_lifetime.GetChildLifetime();
    if (lifetime != null)
    {
      var keyHash = Cryptography.CalculateSHAHash(p_options.PassKey, HashComplexity.Bit512);
      var wsClient = new WebsocketClient(new Uri($"{p_options.ForwardUri}?key={keyHash}"))
      {
        IsReconnectionEnabled = true,
        ReconnectTimeout = TimeSpan.FromMinutes(5),
      };

      lifetime.ToDisposeOnEnding(wsClient);

      _ = CreateOutputRoutineAsync(
        wsClient,
        _localServiceSocket,
        _localServiceEndpoint,
        lifetime);

      return new WsClientInfo(lifetime, wsClient) { Timestamp = Environment.TickCount64 };
    }

    throw new OperationCanceledException();
  }

}
