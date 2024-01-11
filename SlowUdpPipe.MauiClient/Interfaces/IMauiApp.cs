using Ax.Fw.SharedTypes.Interfaces;

namespace SlowUdpPipe.MauiClient.Interfaces;

public interface IMauiApp
{
  IReadOnlyDependencyContainer Container { get; }
}
