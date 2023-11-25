using Android.Util;
using JustLogger;
using JustLogger.Interfaces;
using JustLogger.Toolkit;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SlowUdpPipe.MauiClient.Platforms.Android.Toolkit;

internal class AndroidLogger : ILogger
{
  private readonly string p_tag;
  private readonly ConcurrentDictionary<LogEntryType, long> p_stats = new();
  

  public AndroidLogger(string _tag)
  {
    p_tag = _tag;
  }

  public ILogger this[string _scope] => new NamedLogger(this, _scope);

  public void Error(string _text, string? _name = null)
  {
    p_stats.AddOrUpdate(LogEntryType.ERROR, 1, (_, _prevValue) => ++_prevValue);
    Log.Error(p_tag, _text);
  }

  public void Error(string _text, Exception _ex, string? _name = null)
  {
    p_stats.AddOrUpdate(LogEntryType.ERROR, 1, (_, _prevValue) => ++_prevValue);
    Log.Error(p_tag, $"{_text}{Environment.NewLine}{_ex.Message}");
  }

  public void Warn(string _text, string? _name = null)
  {
    p_stats.AddOrUpdate(LogEntryType.WARN, 1, (_, _prevValue) => ++_prevValue);
    Log.Warn(p_tag, _text);
  }

  public void Info(string _text, string? _name = null)
  {
    p_stats.AddOrUpdate(LogEntryType.INFO, 1, (_, _prevValue) => ++_prevValue);
    Log.Info(p_tag, _text);
  }

  public void InfoJson<T>(string _text, T _object, string? _scope = null) where T : notnull
  {
    p_stats.AddOrUpdate(LogEntryType.INFO, 1, (_, _prevValue) => ++_prevValue);
    var json = JsonSerializer.Serialize(_object);
    Log.Info(p_tag, $"{_text}{Environment.NewLine}{json}");
  }

  public void WarnJson<T>(string _text, T _object, string? _scope = null) where T : notnull
  {
    p_stats.AddOrUpdate(LogEntryType.WARN, 1, (_, _prevValue) => ++_prevValue);
    var json = JsonSerializer.Serialize(_object);
    Log.Warn(p_tag, $"{_text}{Environment.NewLine}{json}");
  }

  public void ErrorJson<T>(string _text, T _object, string? _scope = null) where T : notnull
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
