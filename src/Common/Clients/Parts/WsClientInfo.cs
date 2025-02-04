using Ax.Fw.SharedTypes.Interfaces;
using Websocket.Client;

namespace CloakTunnel.Common.Clients.Parts;

internal record WsClientInfo(
  ILifetime Lifetime,
  WebsocketClient WsClient) : ITunnelClientInfo
{
  public long Timestamp { get; set; }
  public void Send(Span<byte> _data) => WsClient.SendInstant(_data.ToArray());
};
