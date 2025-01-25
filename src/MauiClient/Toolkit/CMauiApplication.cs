using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.MauiClient.Interfaces;

namespace CloakTunnel.MauiClient.Toolkit;

public abstract class CMauiApplication : Application, IMauiApp
{
  protected CMauiApplication() : base()
  {
    Container = MauiProgram.Container;
  }

  public IReadOnlyDependencyContainer Container { get; }

}
