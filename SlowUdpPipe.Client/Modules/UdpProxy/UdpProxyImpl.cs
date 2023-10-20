using Ax.Fw.Attributes;
using Ax.Fw.SharedTypes.Interfaces;
using JustLogger.Interfaces;
using SlowUdpPipe.Client.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.Common.Modules;

namespace SlowUdpPipe.Client.Modules.UdpProxy;

[ExportClass(typeof(UdpProxyImpl), Singleton: true, ActivateOnStart: true)]
internal class UdpProxyImpl
{
  private readonly UdpTunnelClient p_udpTunnel;

  public UdpProxyImpl(
    ISettingsProvider _settingsProvider,
    IReadOnlyLifetime _lifetime,
    ILogger _logger)
  {
    var udpTunnelOptions = new UdpTunnelClientOptions(
      _settingsProvider.Remote,
      _settingsProvider.Local,
      _settingsProvider.Algorithm,
      _settingsProvider.Key);

    p_udpTunnel = new UdpTunnelClient(udpTunnelOptions, _lifetime, _logger);
  }

}
