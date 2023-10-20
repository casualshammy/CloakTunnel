using Grace.DependencyInjection;

namespace SlowUdpPipe.MauiClient.Interfaces;

public interface IMauiApp
{
  IInjectionScope Container { get; }
}
