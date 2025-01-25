using CloakTunnel.Client.Interfaces;
using CloakTunnel.Common.Data;

namespace CloakTunnel.Client.Modules.SettingsProvider;

internal class SettingsProviderImpl : ISettingsProvider
{
  public SettingsProviderImpl(UdpTunnelClientOptions _options)
  {
    Options = _options;
  }

  public UdpTunnelClientOptions Options { get; }

}