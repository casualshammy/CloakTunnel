using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Clients.Parts;
using CloakTunnel.Common.Data;
using CloakTunnel.Common.Toolkit;
using System.Net;
using System.Net.Sockets;

namespace CloakTunnel.Common.Clients;

public class UdpTunnelClient : CommonTunnelClient
{
  public UdpTunnelClient(
    TunnelClientOptions _options,
    IReadOnlyLifetime _lifetime,
    ILog _logger) : base(_options, _lifetime, _logger)
  { }

  private void CreateOutputRoutine(
    Socket _forwardUdpSocket,
    Socket _inputUdpSocket,
    EndPoint _inputClientEndpoint,
    IReadOnlyLifetime _lifetime)
  {
    Span<byte> buffer = new byte[128 * 1024];
    EndPoint _ = new IPEndPoint(IPAddress.Any, short.MaxValue);
    var cryptor = EncryptionToolkit.GetCrypto(p_options.Encryption, _lifetime, p_options.PassKey);
    var log = p_log["udp-output"][$"{_inputClientEndpoint}"];

    log.Info($"**Output socket routine** is __started__");

    while (!_lifetime.IsCancellationRequested)
    {
      try
      {
        if (!_forwardUdpSocket.IsBound)
        {
          log.Warn($"Can't connect to forward address: socket is not bound");
          Thread.Sleep(1000);
          continue;
        }

        var receivedBytes = _forwardUdpSocket.ReceiveFrom(buffer, SocketFlags.None, ref _);
        if (receivedBytes > 0)
        {
          Interlocked.Add(ref p_byteRecvCount, (ulong)receivedBytes);
          var dataToSend = cryptor.Decrypt(buffer[..receivedBytes]);
          _inputUdpSocket.SendTo(dataToSend, SocketFlags.None, _inputClientEndpoint);
        }
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
    }

    log.Info($"**Output socket routine** is __ended__");
  }

  protected override ITunnelClientInfo AllocateNewClient(
    Socket _inputUdpSocket,
    EndPoint _inputClientEndpoint)
  {
    var lifetime = p_lifetime.GetChildLifetime();
    if (lifetime != null)
    {
      var forwardUdpSocket = lifetime.ToDisposeOnEnding(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp));
      var thread = new Thread(() => CreateOutputRoutine(
                   forwardUdpSocket,
                   _inputUdpSocket,
                   _inputClientEndpoint,
                   lifetime))
      { Priority = ThreadPriority.Highest };
      thread.Start();

      var forwardEndPoint = IPEndPoint.Parse($"{p_options.ForwardUri.Host}:{p_options.ForwardUri.Port}");
      return new UdpClientInfo(lifetime, forwardUdpSocket, forwardEndPoint) { Timestamp = Environment.TickCount64 };
    }

    throw new OperationCanceledException();
  }

}
