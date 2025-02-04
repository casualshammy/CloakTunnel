using CloakTunnel.Common.Data;

namespace CloakTunnel.Common.Interfaces;

public interface ITunnelClient
{
  public IObservable<TunnelStat> Stats { get; }

  public void DropAllClients();
}
