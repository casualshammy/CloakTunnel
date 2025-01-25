using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Data;
using CloakTunnel.Server.Interfaces;
using CloakTunnel.Server.Modules.Servers;

namespace CloakTunnel.Server.Modules.UdpProxy;

internal class TunnelControllerImpl : IAppModule<TunnelControllerImpl>
{
  public static TunnelControllerImpl ExportInstance(IAppDependencyCtx _ctx)
  {
    return _ctx.CreateInstance((
      ISettingsProvider _settingsProvider,
      IReadOnlyLifetime _lifetime,
      ILog _logger) => new TunnelControllerImpl(_settingsProvider, _lifetime, _logger));
  }

  public TunnelControllerImpl(
    ISettingsProvider _settingsProvider,
    IReadOnlyLifetime _lifetime,
    ILog _logger)
  {
    var options = _settingsProvider.Options;

    _logger.Warn($"Launching udp tunnel L:{options.LocalEndpoint} > R:{options.RemoteEndpoint}; algorithm: {options.EncryptionAlgorithm}...");
    if (options.LocalEndpointType == EndpointType.Udp)
      _ = new UdpTunnelServer(options, _lifetime, _logger);
    else if (options.LocalEndpointType == EndpointType.Websocket)
      _ = new WsTunnelServer(options, _lifetime, _logger);
  }

}
