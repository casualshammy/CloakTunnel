using Ax.Fw.Attributes;
using Ax.Fw.Cache;
using Ax.Fw.Extensions;
using JustLogger.Interfaces;
using Newtonsoft.Json;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.MauiClient.Interfaces;
using System.Reactive;
using System.Reactive.Subjects;
using static SlowUdpPipe.MauiClient.Data.Consts;

namespace SlowUdpPipe.MauiClient.Modules.PreferencesStorage;

[ExportClass(typeof(IPreferencesStorage), Singleton: true)]
internal class PreferencesStorageImpl : IPreferencesStorage
{
  private readonly ILogger p_log;
  private readonly SyncCache<string, object?> p_cache = new(new SyncCacheSettings(100, 10, TimeSpan.FromHours(1)));
  private readonly ReplaySubject<Unit> p_prefChangedFlow = new(1);

  public PreferencesStorageImpl(ILogger _log)
  {
    p_log = _log["pref-storage"];

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

    obj = JsonConvert.DeserializeObject<T>(preferenceValue);
    p_cache.Put(_key, obj);
    return (T?)obj;
  }

  public void SetValue<T>(string _key, T? _value)
  {
    var json = JsonConvert.SerializeObject(_value);
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

    SetValue(PREF_DB_LOCAL, "127.0.0.1:51820");
    SetValue(PREF_DB_REMOTE, "1.1.1.1:51820");
    SetValue(PREF_DB_CIPHER, EncryptionAlgorithm.Aes256);
    SetValue(PREF_DB_KEY, "example-key");
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
    var migrations = new Dictionary<int, Action>();

    migrations.Add(175, () =>
    {
      
    });

    return migrations;
  }

}
