using SlowUdpPipe.Common.Data;

namespace SlowUdpPipe.MauiClient.Interfaces;

public interface IUdpTunnelCtrl
{
  IObservable<UdpTunnelStat> TunnelStats { get; }
  IObservable<bool> State { get; }

  Task<bool> GetStateAsync();
  void SetState(bool _state);
}
