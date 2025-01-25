namespace CloakTunnel.Common.Data;

public readonly record struct UdpTunnelStat(
  ulong TxBytePerSecond, 
  ulong RxBytePerSecond);
