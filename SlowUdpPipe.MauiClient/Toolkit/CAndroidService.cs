using Grace.DependencyInjection;
using JustLogger.Interfaces;

namespace SlowUdpPipe.MauiClient.Toolkit;

public abstract class CAndroidService : Android.App.Service
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
  public IInjectionScope Container { get; private set; }
  public ILogger Log { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  public override void OnCreate()
  {
    base.OnCreate();
    Container = MauiProgram.Container;

    Log = Container.Locate<ILogger>()[Class.SimpleName];

    Container.Inject(this);
  }
  public override void OnDestroy()
  {
    base.OnDestroy();
  }
}
