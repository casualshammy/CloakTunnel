using Android.Util;
using Ax.Fw.Log;
using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CloakTunnel.MauiClient.Platforms.Android.Toolkit;

internal class AndroidLogger : ILog
{
  private readonly string p_tag;
  private readonly ConcurrentDictionary<LogEntryType, long> p_stats = new();

  public AndroidLogger(string _tag)
  {
    p_tag = _tag;
    Scope = _tag;
  }

  public string? Scope { get; }

  public ILog this[string _scope] => new GenericLog();

  public void Error(string _text)
  {
    p_stats.AddOrUpdate(LogEntryType.ERROR, 1, (_, _prevValue) => ++_prevValue);
    Log.Error(p_tag, _text);
  }

  public void Error(string _text, Exception? _ex)
  {
    p_stats.AddOrUpdate(LogEntryType.ERROR, 1, (_, _prevValue) => ++_prevValue);
    Log.Error(p_tag, $"{_text}{Environment.NewLine}{_ex?.Message}");
  }

  public void Warn(string _text)
  {
    p_stats.AddOrUpdate(LogEntryType.WARN, 1, (_, _prevValue) => ++_prevValue);
    Log.Warn(p_tag, _text);
  }

  public void Info(string _text)
  {
    p_stats.AddOrUpdate(LogEntryType.INFO, 1, (_, _prevValue) => ++_prevValue);
    Log.Info(p_tag, _text);
  }

  public void InfoJson<T>(string _text, T _object) where T : notnull
  {
    p_stats.AddOrUpdate(LogEntryType.INFO, 1, (_, _prevValue) => ++_prevValue);
    var json = JsonSerializer.Serialize(_object);
    Log.Info(p_tag, $"{_text}{Environment.NewLine}{json}");
  }

  public void WarnJson<T>(string _text, T _object) where T : notnull
  {
    p_stats.AddOrUpdate(LogEntryType.WARN, 1, (_, _prevValue) => ++_prevValue);
    var json = JsonSerializer.Serialize(_object);
    Log.Warn(p_tag, $"{_text}{Environment.NewLine}{json}");
  }

  public void ErrorJson<T>(string _text, T _object) where T : notnull
  {
    p_stats.AddOrUpdate(LogEntryType.ERROR, 1, (_, _prevValue) => ++_prevValue);
    var json = JsonSerializer.Serialize(_object);
    Log.Error(p_tag, $"{_text}{Environment.NewLine}{json}");
  }

  public long GetEntriesCount(LogEntryType _type)
  {
    if (p_stats.TryGetValue(_type, out var count))
      return count;

    return 0L;
  }

  public void Flush() { }

}
