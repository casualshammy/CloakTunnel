using Ax.Fw.Attributes;
using Ax.Fw.Cache;
using Ax.Fw.Extensions;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.MauiClient.Interfaces;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.Json;
using static SlowUdpPipe.MauiClient.Data.AppConsts;

namespace SlowUdpPipe.MauiClient.Modules.PreferencesStorage;

[ExportClass(typeof(IPreferencesStorage), Singleton: true)]
internal class PreferencesStorageImpl : IPreferencesStorage
{
  private const string DEPRECATED_PREF_DB_LOCAL = "settings.local";
  private const string DEPRECATED_PREF_DB_REMOTE = "settings.remote";
  private const string DEPRECATED_PREF_DB_CIPHER = "settings.cipher";
  private const string DEPRECATED_PREF_DB_KEY = "settings.key";
  private const string DEPRECATED_PREF_DB_UP_TUNNEL_ON_APP_STARTUP = "settings.up-tunnel-on-app-startup";

  private readonly ILogger p_log;
  private readonly ITunnelsConfCtrl p_tunnelsConfCtrl;
  private readonly SyncCache<string, object?> p_cache = new(new SyncCacheSettings(100, 10, TimeSpan.FromHours(1)));
  private readonly ReplaySubject<Unit> p_prefChangedFlow = new(1);

  public PreferencesStorageImpl(
    ILogger _log,
    ITunnelsConfCtrl _tunnelsConfCtrl)
  {
    p_log = _log["pref-storage"];
    p_tunnelsConfCtrl = _tunnelsConfCtrl;

    SetupDefaultPreferences();
    MigratePreferences();

    p_prefChangedFlow.OnNext();
  }

  public IObservable<Unit> PreferencesChanged => p_prefChangedFlow;

  public T? GetValueOrDefault<T>(string _key)
  {
    if (p_cache.TryGet(_key, out var obj))
      return (T?)obj;

    var preferenceValue = Preferences.Default.Get(_key, (string?)null);
    if (preferenceValue == null)
      return default;

    obj = JsonSerializer.Deserialize<T>(preferenceValue);
    p_cache.Put(_key, obj);
    return (T?)obj;
  }

  public void SetValue<T>(string _key, T? _value)
  {
    var json = JsonSerializer.Serialize(_value);
    Preferences.Default.Set(_key, json);
    p_cache.Put(_key, _value);
    p_prefChangedFlow.OnNext();
  }

  public void RemoveValue(string _key)
  {
    Preferences.Default.Remove(_key);
    p_cache.TryRemove(_key, out _);
    p_prefChangedFlow.OnNext();
  }

  private void SetupDefaultPreferences()
  {
    if (GetValueOrDefault<int>(PREF_DB_VERSION) != default)
      return;

    SetValue(PREF_DB_VERSION, 1);
  }

  private void MigratePreferences()
  {
    var dbVersion = GetValueOrDefault<int>(PREF_DB_VERSION);
    if (!int.TryParse(AppInfo.Current.BuildString, out var appVersion))
    {
      p_log.Error($"Can't parse app version: '{AppInfo.Current.BuildString}'");
      return;
    }

    if (appVersion != dbVersion)
    {
      p_log.Info($"Application is updated - wiping cache...");
      var cacheDir = new DirectoryInfo(FileSystem.Current.CacheDirectory);
      foreach (var file in cacheDir.EnumerateFiles("*", SearchOption.AllDirectories))
        if (!file.TryDelete())
          p_log.Warn($"Can't delete cache file: '{file.FullName}'");

      p_log.Info($"Cache is wiped");
    }

    var migrations = GetMigrations();
    for (var i = dbVersion + 1; i <= appVersion; i++)
      if (migrations.TryGetValue(i, out var action))
      {
        p_log.Info($"Migrating db up to version -->> {i}");
        action();
        p_log.Info($"Db is migrated to version -->> {i}");
      }

    SetValue(PREF_DB_VERSION, appVersion);
  }

  private IReadOnlyDictionary<int, Action> GetMigrations()
  {
    var migrations = new Dictionary<int, Action>
    {
      {
        29,
        () => {
          var local = GetValueOrDefault<string>(DEPRECATED_PREF_DB_LOCAL);
          var remote = GetValueOrDefault<string>(DEPRECATED_PREF_DB_REMOTE);
          var alg = GetValueOrDefault<EncryptionAlgorithm>(DEPRECATED_PREF_DB_CIPHER);
          var key = GetValueOrDefault<string>(DEPRECATED_PREF_DB_KEY);
          var autoUpTunnel = GetValueOrDefault<bool>(DEPRECATED_PREF_DB_UP_TUNNEL_ON_APP_STARTUP);

          if (!local.IsNullOrEmpty() && !remote.IsNullOrEmpty() && !key.IsNullOrEmpty())
            p_tunnelsConfCtrl.CreateTunnelConf("main-tunnel", local, remote, alg, key, autoUpTunnel);
        }
      }
    };

    return migrations;
  }

}
