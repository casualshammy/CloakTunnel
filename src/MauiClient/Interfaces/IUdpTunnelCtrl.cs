using CloakTunnel.MauiClient.Data;

namespace CloakTunnel.MauiClient.Interfaces;

public interface IUdpTunnelCtrl
{
  IObservable<TunnelStatWithName> TunnelsStats { get; }
}
