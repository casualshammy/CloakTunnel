using SlowUdpPipe.Common.Data;
using SlowUdpPipe.MauiClient.Data;
using SlowUdpPipe.MauiClient.ViewModels;

namespace SlowUdpPipe.MauiClient.Interfaces;

public interface ITunnelsConfCtrl
{
  IObservable<Guid> TunnelConfRemoved { get; }
  IObservable<TunnelsConfEntry> TunnelConfAdded { get; }
  IObservable<TunnelsConfEntry> TunnelConfChanged { get; }
  IObservable<ICollection<TunnelsConfEntry>> TunnelsConf { get; }

  TunnelsConfEntry CreateTunnelConf();
  TunnelsConfEntry CreateTunnelConf(string _name, string _local, string _remote, EncryptionAlgorithm _alg, string _key, bool _enabled);
  IReadOnlyList<TunnelsConfEntry> GetConfs();
  void DeleteTunnelConf(Guid _tunnelGuid);
  void UpdateTunnel(TunnelEditViewModel _model);
  void DisableTunnel(Guid _tunnelGuid);
}
