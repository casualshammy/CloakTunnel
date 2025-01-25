using CloakTunnel.Common.Data;

namespace CloakTunnel.Server.Interfaces;

internal interface ISettingsProvider
{
  public UdpTunnelServerOptions Options { get; }
}
