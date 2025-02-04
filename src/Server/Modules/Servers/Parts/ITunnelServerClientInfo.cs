namespace CloakTunnel.Server.Modules.Servers.Parts;

public interface ITunnelServerClientInfo
{
  string ClientId { get; }
  void Send(Span<byte> _data);
}
