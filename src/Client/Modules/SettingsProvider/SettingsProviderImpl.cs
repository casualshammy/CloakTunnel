using CloakTunnel.Client.Interfaces;
using CloakTunnel.Common.Data;

namespace CloakTunnel.Client.Modules.SettingsProvider;

internal class SettingsProviderImpl : ISettingsProvider
{
  public SettingsProviderImpl(TunnelClientOptions _options)
  {
    Options = _options;
  }

  public TunnelClientOptions Options { get; }

}