using CloakTunnel.Common.Data;

namespace CloakTunnel.Client.Interfaces;

internal interface ISettingsProvider
{
  public TunnelClientOptions Options { get; }
}
