using System.Net;

namespace SlowUdpPipe.Common.Data;

public record UdpTunnelServerOptions(
  IPEndPoint Remote,
  IPEndPoint Local,
  IReadOnlyList<EncryptionAlgorithm> Algorithms,
  string Key);
