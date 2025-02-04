using Ax.Fw.SharedTypes.Interfaces;
using System.Net;
using System.Net.Sockets;

namespace CloakTunnel.Common.Clients.Parts;

internal record UdpClientInfo(
  ILifetime Lifetime,
  Socket Socket,
  EndPoint ForwardEndpoint) : ITunnelClientInfo
{
  public long Timestamp { get; set; }
  public void Send(Span<byte> _data) => Socket.SendTo(_data, SocketFlags.None, ForwardEndpoint);
};
