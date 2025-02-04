using Ax.Fw.SharedTypes.Interfaces;

namespace CloakTunnel.Common.Clients.Parts;

public interface ITunnelClientInfo
{
  long Timestamp { get; set; }
  ILifetime Lifetime { get; }

  void Send(Span<byte> _data);
}
