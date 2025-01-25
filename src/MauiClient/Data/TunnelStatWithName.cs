namespace CloakTunnel.MauiClient.Data;

public record TunnelStatWithName(
  Guid TunnelGuid,
  string TunnelName, 
  ulong TxBytePerSecond, 
  ulong RxBytePerSecond);