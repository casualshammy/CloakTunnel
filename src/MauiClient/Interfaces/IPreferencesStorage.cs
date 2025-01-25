using System.Reactive;

namespace CloakTunnel.MauiClient.Interfaces;

public interface IPreferencesStorage
{
  IObservable<Unit> PreferencesChanged { get; }
  T? GetValueOrDefault<T>(string _key);
  void RemoveValue(string _key);
  void SetValue<T>(string _key, T _value);
}
