using Android.Util;
using JustLogger;
using JustLogger.Interfaces;
using JustLogger.Toolkit;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace SlowUdpPipe.MauiClient.Platforms.Android.Toolkit;

internal class AndroidLogger : ILogger
{
  private readonly string p_tag;
  private readonly ConcurrentDictionary<LogEntryType, long> p_stats = new();

  public AndroidLogger(string _tag)
  {
    p_tag = _tag;
  }

  public NamedLogger this[string _name] => new(this, _name);

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

  public void InfoJson(string _text, JToken _object, string? _name = null)
  {
    p_stats.AddOrUpdate(LogEntryType.INFO, 1, (_, _prevValue) => ++_prevValue);
    Log.Info(p_tag, $"{_text}{Environment.NewLine}{_object.ToString(Newtonsoft.Json.Formatting.Indented)}");
  }

  public long GetEntriesCount(LogEntryType _type)
  {
    if (p_stats.TryGetValue(_type, out var count))
      return count;

    return 0L;
  }

  public void NewEvent(LogEntryType _type, string _text)
  {
    p_stats.AddOrUpdate(_type, 1, (_, _prevValue) => ++_prevValue);

    if (_type == LogEntryType.INFO)
      Info(_text);
    else if (_type == LogEntryType.ERROR)
      Error(_text);
    else if (_type == LogEntryType.WARN)
      Warn(_text);
  }

  public void Flush() { }

}
