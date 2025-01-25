using CloakTunnel.Common.Data;

namespace CloakTunnel.Client.Interfaces;

internal interface ISettingsProvider
{
  public UdpTunnelClientOptions Options { get; }
}
