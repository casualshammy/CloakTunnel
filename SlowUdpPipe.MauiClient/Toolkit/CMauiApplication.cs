using Ax.Fw.SharedTypes.Interfaces;
using SlowUdpPipe.MauiClient.Interfaces;

namespace SlowUdpPipe.MauiClient.Toolkit;

public abstract class CMauiApplication : Application, IMauiApp
{
  protected CMauiApplication() : base()
  {
    Container = MauiProgram.Container;
  }

  public IReadOnlyDependencyContainer Container { get; }

}
