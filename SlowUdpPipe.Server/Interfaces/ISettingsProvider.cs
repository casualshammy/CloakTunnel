using SlowUdpPipe.Server.Data;

namespace SlowUdpPipe.Interfaces;

internal interface ISettingsProvider
{
  IObservable<IReadOnlyDictionary<string, UdpTunnelServerRawOptions>?> Definitions { get; }
}
