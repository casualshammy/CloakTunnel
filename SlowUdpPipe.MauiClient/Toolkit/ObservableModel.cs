using Ax.Fw.SharedTypes.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SlowUdpPipe.MauiClient.Toolkit;

public abstract class ObservableModel : INotifyPropertyChanged
{
  public event PropertyChangedEventHandler? PropertyChanged;

  public ObservableModel()
  {
    if (Application.Current is not CMauiApplication app)
      throw new ApplicationException($"Application is not {nameof(CMauiApplication)}");

    Container = app.Container;
  }

  public IReadOnlyDependencyContainer Container { get; }

  protected bool SetProperty<T>(
    ref T _backingStore,
    T _value,
    [CallerMemberName] string _propertyName = "",
    Action? _onChanged = null,
    params string[] _additionalPropertyNames)
  {
    if (EqualityComparer<T>.Default.Equals(_backingStore, _value))
      return false;

    _backingStore = _value;
    _onChanged?.Invoke();
    OnPropertyChanged(_propertyName);
    foreach (var propName in _additionalPropertyNames)
      OnPropertyChanged(propName);

    return true;
  }

  protected void OnPropertyChanged([CallerMemberName] string _propertyName = "")
  {
    var changed = PropertyChanged;
    if (changed == null)
      return;

    changed.Invoke(this, new PropertyChangedEventArgs(_propertyName));
  }

}
