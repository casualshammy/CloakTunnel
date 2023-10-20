using Ax.Fw.Attributes;
using Ax.Fw.SharedTypes.Interfaces;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.Common.Modules;
using SlowUdpPipe.Interfaces;

namespace SlowUdpPipe.Modules.UdpProxy;

[ExportClass(typeof(UdpProxyImpl), Singleton: true, ActivateOnStart: true)]
internal class UdpProxyImpl
{
  private readonly UdpTunnelServer p_udpTunnel;

  public UdpProxyImpl(
    ISettingsProvider _settingsProvider,
    IReadOnlyLifetime _lifetime,
    ILogger _logger)
  {
    var udpTunnelOptions = new UdpTunnelServerOptions(
      _settingsProvider.Remote,
      _settingsProvider.Local,
      _settingsProvider.Algorithms,
      _settingsProvider.Key);

    p_udpTunnel = new UdpTunnelServer(udpTunnelOptions, _lifetime, _logger);
  }

}
