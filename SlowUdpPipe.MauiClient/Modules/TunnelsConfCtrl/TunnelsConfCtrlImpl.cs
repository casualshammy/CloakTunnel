using Ax.Fw.Attributes;
using Ax.Fw.JsonStorages;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.MauiClient.ViewModels;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Subjects;

namespace SlowUdpPipe.MauiClient.Modules.TunnelsConfCtrl;

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

public record TunnelsConfEntry(
    Guid Guid,
    string Name,
    string LocalAddress,
    string RemoteAddress,
    EncryptionAlgorithm EncryptionAlgo,
    string Key,
    bool Enabled)
{
  public static TunnelsConfEntry FromModel(TunnelEditViewModel _model)
  {
    return new TunnelsConfEntry(
      _model.Guid,
      _model.Name,
      _model.LocalAddress,
      _model.RemoteAddress,
      Consts.ENCRYPTION_ALG_SLUG_REVERSE[_model.EncryptionAlgo],
      _model.Key,
      _model.Enabled);
  }
};

[ExportClass(typeof(ITunnelsConfCtrl), Singleton: true)]
public class TunnelsConfCtrlImpl : ITunnelsConfCtrl
{
  private readonly ConcurrentDictionary<Guid, TunnelsConfEntry> p_tunnels;
  private readonly JsonStorage<ConcurrentDictionary<Guid, TunnelsConfEntry>> p_storage;
  private readonly ReplaySubject<ICollection<TunnelsConfEntry>> p_confsSubj = new(1);
  private readonly Subject<Guid> p_confRemovedSubj = new();
  private readonly Subject<TunnelsConfEntry> p_confAddedSubj = new();
  private readonly Subject<TunnelsConfEntry> p_confChangedSubj = new();

  public TunnelsConfCtrlImpl()
  {
    var tunnelsConfPath = Path.Combine(FileSystem.Current.AppDataDirectory, "tunnels.json");
    p_storage = new(tunnelsConfPath);

    p_tunnels = p_storage.Load(() => new ConcurrentDictionary<Guid, TunnelsConfEntry>());
    p_confsSubj.OnNext(p_tunnels.Values);

    TunnelConfRemoved = p_confRemovedSubj;
    TunnelConfAdded = p_confAddedSubj;
    TunnelConfChanged = p_confChangedSubj;
    TunnelsConf = p_confsSubj;
  }

  public IObservable<Guid> TunnelConfRemoved { get; }
  public IObservable<TunnelsConfEntry> TunnelConfAdded { get; }
  public IObservable<TunnelsConfEntry> TunnelConfChanged { get; }

  public IObservable<ICollection<TunnelsConfEntry>> TunnelsConf { get; }

  public IReadOnlyList<TunnelsConfEntry> GetConfs() => p_tunnels.Values.ToImmutableList();

  public void DeleteTunnelConf(Guid _tunnelGuid)
  {
    p_tunnels.TryRemove(_tunnelGuid, out _);
    p_storage.Save(p_tunnels, true);
    p_confRemovedSubj.OnNext(_tunnelGuid);
    p_confsSubj.OnNext(p_tunnels.Values);
  }

  public TunnelsConfEntry CreateTunnelConf()
  {
    var guid = Guid.NewGuid();
    var conf = new TunnelsConfEntry(
      guid, 
      "New Tunnel", 
      "127.0.0.1:51820", 
      "1.1.1.1:1935", 
      EncryptionAlgorithm.AesGcm256, 
      "example-key", 
      false);

    p_tunnels.TryAdd(guid, conf);
    p_storage.Save(p_tunnels, true);

    p_confAddedSubj.OnNext(conf);
    p_confsSubj.OnNext(p_tunnels.Values);
    return conf;
  }

  public TunnelsConfEntry CreateTunnelConf(
    string _name, 
    string _local, 
    string _remote, 
    EncryptionAlgorithm _alg, 
    string _key, 
    bool _enabled)
  {
    var guid = Guid.NewGuid();
    var conf = new TunnelsConfEntry(
      guid,
      _name,
      _local,
      _remote,
      _alg,
      _key,
      _enabled);

    p_tunnels.TryAdd(guid, conf);
    p_storage.Save(p_tunnels, true);

    p_confAddedSubj.OnNext(conf);
    p_confsSubj.OnNext(p_tunnels.Values);
    return conf;
  }

  public void UpdateTunnel(TunnelEditViewModel _model)
  {
    if (!p_tunnels.TryGetValue(_model.Guid, out _))
      return;

    var newConf = new TunnelsConfEntry(
      _model.Guid,
      _model.Name,
      _model.LocalAddress,
      _model.RemoteAddress,
      Consts.ENCRYPTION_ALG_SLUG_REVERSE[_model.EncryptionAlgo],
      _model.Key,
      _model.Enabled);

    p_tunnels[_model.Guid] = newConf;
    p_storage.Save(p_tunnels, true);

    p_confChangedSubj.OnNext(newConf);
    p_confsSubj.OnNext(p_tunnels.Values);
  }

  public void DisableTunnel(Guid _tunnelGuid)
  {
    if (!p_tunnels.TryGetValue(_tunnelGuid, out var tunnel))
      return;

    var newConf = tunnel with { Enabled = false };

    p_tunnels[_tunnelGuid] = newConf;
    p_storage.Save(p_tunnels, true);

    p_confChangedSubj.OnNext(newConf);
    p_confsSubj.OnNext(p_tunnels.Values);
  }

}
