using Ax.Fw.Attributes;
using Ax.Fw.DependencyInjection;
using Ax.Fw.JsonStorages;
using Ax.Fw.SharedTypes.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.Common.Toolkit;
using SlowUdpPipe.MauiClient.Data;
using SlowUdpPipe.MauiClient.Interfaces;
using SlowUdpPipe.MauiClient.ViewModels;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Subjects;
using System.Text.Json.Serialization;

namespace SlowUdpPipe.MauiClient.Modules.TunnelsConfCtrl;

public class TunnelsConfCtrlImpl : ITunnelsConfCtrl, IAppModule<TunnelsConfCtrlImpl>
{
  public static TunnelsConfCtrlImpl ExportInstance(IAppDependencyCtx _ctx)
  {
    return _ctx.CreateInstance((IReadOnlyLifetime _lifetime) => new TunnelsConfCtrlImpl(_lifetime));
  }

  private readonly ConcurrentDictionary<Guid, TunnelsConfEntry> p_tunnels;
  private readonly JsonStorage<ConcurrentDictionary<Guid, TunnelsConfEntry>> p_storage;
  private readonly ReplaySubject<ICollection<TunnelsConfEntry>> p_confsSubj = new(1);
  private readonly Subject<Guid> p_confRemovedSubj = new();
  private readonly Subject<TunnelsConfEntry> p_confAddedSubj = new();
  private readonly Subject<TunnelsConfEntry> p_confChangedSubj = new();

  public TunnelsConfCtrlImpl(IReadOnlyLifetime _lifetime)
  {
    var tunnelsConfPath = Path.Combine(FileSystem.Current.AppDataDirectory, "tunnels.json");
    p_storage = new(tunnelsConfPath, TunnelsConfigFileJsonSerializationContext.Default.ConcurrentDictionaryGuidTunnelsConfEntry, _lifetime);

    p_tunnels = p_storage.Read(() => new ConcurrentDictionary<Guid, TunnelsConfEntry>());
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
    p_storage.Write(p_tunnels);
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
    p_storage.Write(p_tunnels);

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
    p_storage.Write(p_tunnels);

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
      EncryptionToolkit.ENCRYPTION_ALG_SLUG_REVERSE[_model.EncryptionAlgo],
      _model.Key,
      _model.Enabled);

    p_tunnels[_model.Guid] = newConf;
    p_storage.Write(p_tunnels);

    p_confChangedSubj.OnNext(newConf);
    p_confsSubj.OnNext(p_tunnels.Values);
  }

  public void DisableTunnel(Guid _tunnelGuid)
  {
    if (!p_tunnels.TryGetValue(_tunnelGuid, out var tunnel))
      return;

    var newConf = tunnel with { Enabled = false };

    p_tunnels[_tunnelGuid] = newConf;
    p_storage.Write(p_tunnels);

    p_confChangedSubj.OnNext(newConf);
    p_confsSubj.OnNext(p_tunnels.Values);
  }

}

[JsonSerializable(typeof(ConcurrentDictionary<Guid, TunnelsConfEntry>))]
internal partial class TunnelsConfigFileJsonSerializationContext : JsonSerializerContext
{

}
