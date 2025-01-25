using System.Net.WebSockets;

namespace CloakTunnel.Server.Modules.WebSocketController.Parts;

public record WebSocketSession(
  Guid Id,
  WebSocket Socket);
