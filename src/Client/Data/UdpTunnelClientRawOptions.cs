namespace CloakTunnel.Client.Data;

internal record UdpTunnelClientRawOptions(
  string Remote,
  string Local,
  string? Cipher,
  string Key);
