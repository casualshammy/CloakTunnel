using CloakTunnel.Common.Data;
using CloakTunnel.Server.Interfaces;

namespace CloakTunnel.Server.Modules.SettingsProvider;

internal class SettingsProviderImpl : ISettingsProvider
{
  public SettingsProviderImpl(TunnelServerOptions _options)
  {
    Options = _options;
  }

  public TunnelServerOptions Options { get; }

}
