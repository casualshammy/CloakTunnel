namespace SlowUdpPipe.Server.Data;

internal record UdpTunnelServerRawOptions(
  string Remote,
  string Local,
  string[]? Ciphers,
  string Key);
