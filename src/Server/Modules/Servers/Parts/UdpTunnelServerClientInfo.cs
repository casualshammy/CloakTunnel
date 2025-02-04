using System.Net;
using System.Net.Sockets;

namespace CloakTunnel.Server.Modules.Servers.Parts;

internal record UdpTunnelServerClientInfo(
  Socket InputSocket,
  EndPoint InputEndpoint) : ITunnelServerClientInfo
{
  public string ClientId => $"{InputEndpoint}";
  public void Send(Span<byte> _data) => InputSocket.SendTo(_data, SocketFlags.None, InputEndpoint);
}
