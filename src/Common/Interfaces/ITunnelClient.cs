using CloakTunnel.Common.Data;

namespace CloakTunnel.Common.Interfaces;

public interface ITunnelClient
{
  public IObservable<UdpTunnelStat> Stats { get; }

  public void DropAllClients();
}
