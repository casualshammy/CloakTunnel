using Grace.DependencyInjection;
using SlowUdpPipe.MauiClient.Interfaces;

namespace SlowUdpPipe.MauiClient.Toolkit;

public abstract class CMauiApplication : Application, IMauiApp
{
  protected CMauiApplication() : base()
  {
    Container = MauiProgram.Container;
  }

  public IInjectionScope Container { get; }

}
