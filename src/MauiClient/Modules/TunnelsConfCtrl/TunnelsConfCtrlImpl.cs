using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Data;
using CloakTunnel.Common.Toolkit;
using CloakTunnel.MauiClient.Data;
using CloakTunnel.MauiClient.Interfaces;
using CloakTunnel.MauiClient.ViewModels;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloakTunnel.MauiClient.Modules.TunnelsConfCtrl;

public class TunnelsConfCtrlImpl : ITunnelsConfCtrl, IAppModule<ITunnelsConfCtrl>
{
  public static ITunnelsConfCtrl ExportInstance(IAppDependencyCtx _ctx)
  {
    return _ctx.CreateInstance((
      IReadOnlyLifetime _lifetime,
      ILog _log) => new TunnelsConfCtrlImpl(_lifetime, _log["tunnels-conf-ctrl"]));
  }

  private readonly ConcurrentDictionary<Guid, TunnelsConfEntry> p_tunnels;
  private readonly ReplaySubject<ICollection<TunnelsConfEntry>> p_confsSubj = new(1);
  private readonly Subject<Guid> p_confRemovedSubj = new();
  private readonly Subject<TunnelsConfEntry> p_confAddedSubj = new();
  private readonly Subject<TunnelsConfEntry> p_confChangedSubj = new();
  private readonly ILog p_log;

  public TunnelsConfCtrlImpl(
    IReadOnlyLifetime _lifetime,
    ILog _log)
  {
    p_log = _log;

    p_tunnels = ReadFromConfigFile();
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

  public IReadOnlyList<TunnelsConfEntry> GetConfs() => [.. p_tunnels.Values];

  public void DeleteTunnelConf(Guid _tunnelGuid)
  {
    p_tunnels.TryRemove(_tunnelGuid, out _);
    WriteToConfigFile(p_tunnels);
    p_confRemovedSubj.OnNext(_tunnelGuid);
    p_confsSubj.OnNext(p_tunnels.Values);
  }

  public TunnelsConfEntry CreateTunnelConf()
  {
    var guid = Guid.NewGuid();
    var conf = new TunnelsConfEntry(
      guid, 
      "New Tunnel", 
      "udp://127.0.0.1:51820", 
      "udp://1.1.1.1:1935", 
      EncryptionAlgorithm.AesGcm256, 
      "example-key", 
      false);

    p_tunnels.TryAdd(guid, conf);
    WriteToConfigFile(p_tunnels);

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
    WriteToConfigFile(p_tunnels);

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
    WriteToConfigFile(p_tunnels);

    p_confChangedSubj.OnNext(newConf);
    p_confsSubj.OnNext(p_tunnels.Values);
  }

  public void DisableTunnel(Guid _tunnelGuid)
  {
    if (!p_tunnels.TryGetValue(_tunnelGuid, out var tunnel))
      return;

    var newConf = tunnel with { Enabled = false };

    p_tunnels[_tunnelGuid] = newConf;
    WriteToConfigFile(p_tunnels);

    p_confChangedSubj.OnNext(newConf);
    p_confsSubj.OnNext(p_tunnels.Values);
  }

  private ConcurrentDictionary<Guid, TunnelsConfEntry> ReadFromConfigFile()
  {
    var tunnelsConfPath = Path.Combine(FileSystem.Current.AppDataDirectory, "tunnels.json");
    if (!File.Exists(tunnelsConfPath))
      return new();

    var json = File.ReadAllText(tunnelsConfPath);
    try
    {
      var conf = JsonSerializer.Deserialize(
        json, 
        TunnelsConfigFileJsonSerializationContext.Default.ConcurrentDictionaryGuidTunnelsConfEntry);

      return conf ?? new();
    }
    catch (Exception ex)
    {
      p_log.Error($"Can't parse tunnels config file: {ex}");
      return new();
    }
  }

  private static void WriteToConfigFile(ConcurrentDictionary<Guid, TunnelsConfEntry> _conf)
  {
    var json = JsonSerializer.Serialize(
        _conf,
        TunnelsConfigFileJsonSerializationContext.Default.ConcurrentDictionaryGuidTunnelsConfEntry);

    var tunnelsConfPath = Path.Combine(FileSystem.Current.AppDataDirectory, "tunnels.json");
    File.WriteAllText(tunnelsConfPath, json);
  }

}

[JsonSerializable(typeof(ConcurrentDictionary<Guid, TunnelsConfEntry>))]
internal partial class TunnelsConfigFileJsonSerializationContext : JsonSerializerContext
{

}
