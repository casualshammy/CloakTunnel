using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Client.Interfaces;
using CloakTunnel.Common.Clients;
using CloakTunnel.Common.Data;

namespace CloakTunnel.Client.Modules.TunnelController;

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

    _logger.Warn($"Launching {options.RemoteType.ToString().ToLowerInvariant()} tunnel {options.BindUri} > {options.ForwardUri}; algorithm: {options.Encryption}...");
    if (options.RemoteType == EndpointType.Udp)
      _ = new UdpTunnelClient(options, _lifetime, _logger);
    else if (options.RemoteType == EndpointType.Websocket)
      _ = new WsTunnelClient(options, _lifetime, _logger);
  }

}
