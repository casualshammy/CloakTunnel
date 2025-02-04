namespace CloakTunnel.Common.Data;

public readonly record struct TunnelStat(
  ulong TxBytePerSecond, 
  ulong RxBytePerSecond,
  ulong TotalBytesSent,
  ulong TotalBytesReceived);
