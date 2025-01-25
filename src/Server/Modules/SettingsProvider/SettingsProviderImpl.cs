using CloakTunnel.Common.Data;
using CloakTunnel.Server.Interfaces;

namespace CloakTunnel.Server.Modules.SettingsProvider;

internal class SettingsProviderImpl : ISettingsProvider
{
  public SettingsProviderImpl(UdpTunnelServerOptions _options)
  {
    Options = _options;
  }

  public UdpTunnelServerOptions Options { get; }

}
