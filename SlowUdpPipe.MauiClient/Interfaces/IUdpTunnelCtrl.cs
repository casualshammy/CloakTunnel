using SlowUdpPipe.Common.Data;
using SlowUdpPipe.MauiClient.Data;

namespace SlowUdpPipe.MauiClient.Interfaces;

public interface IUdpTunnelCtrl
{
  IObservable<TunnelStatWithName> TunnelsStats { get; }
}
