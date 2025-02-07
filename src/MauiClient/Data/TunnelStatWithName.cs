namespace CloakTunnel.MauiClient.Data;

public record TunnelStatWithName(
  Guid TunnelGuid,
  string TunnelName, 
  long TxBytePerSecond, 
  long RxBytePerSecond,
  long TotalTxBytes,
  long TotalRxBytes);