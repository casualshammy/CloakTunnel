using System.Net;

namespace SlowUdpPipe.Common.Data;

public record UdpTunnelClientOptions(
  IPEndPoint Remote,
  IPEndPoint Local,
  EncryptionAlgorithm Cipher,
  string Key);
