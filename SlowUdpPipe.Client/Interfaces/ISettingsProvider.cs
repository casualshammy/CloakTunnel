using SlowUdpPipe.Client.Data;

namespace SlowUdpPipe.Client.Interfaces;

internal interface ISettingsProvider
{
  IObservable<IReadOnlyDictionary<string, UdpTunnelClientRawOptions>?> Definitions { get; }
}
