using Ax.Fw.SharedTypes.Interfaces;

namespace CloakTunnel.MauiClient.Interfaces;

public interface IMauiApp
{
  IReadOnlyDependencyContainer Container { get; }
}
