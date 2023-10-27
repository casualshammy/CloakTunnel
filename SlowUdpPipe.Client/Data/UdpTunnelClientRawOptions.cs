namespace SlowUdpPipe.Client.Data;

internal record UdpTunnelClientRawOptions(
  string Remote,
  string Local,
  string? Cipher,
  string Key);
