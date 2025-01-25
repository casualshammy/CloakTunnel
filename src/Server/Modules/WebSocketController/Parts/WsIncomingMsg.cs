namespace CloakTunnel.Server.Modules.WebSocketController.Parts;

internal record WsIncomingMsg(
  WebSocketSession Session,
  byte[] Data);
