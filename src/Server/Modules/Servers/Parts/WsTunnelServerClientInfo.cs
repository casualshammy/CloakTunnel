using CloakTunnel.Server.Modules.WebSocketController.Parts;

namespace CloakTunnel.Server.Modules.Servers.Parts;

internal record WsTunnelServerClientInfo(
  WsServer WsServer, 
  WebSocketSession WsSession) : ITunnelServerClientInfo
{
  public string ClientId => WsSession.Id.ToString();
  public void Send(Span<byte> _data) => WsServer.EnqueueMsg(WsSession, _data.ToArray());
}
