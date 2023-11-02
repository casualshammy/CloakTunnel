using SlowUdpPipe.Common.Data;

namespace SlowUdpPipe.MauiClient.Data;

internal record TunnelDef(
  string Name,
  string LocalAddress,
  string RemoteAddress,
  EncryptionAlgorithm Encryption,
  string Key,
  bool Enabled);
