namespace SlowUdpPipe.Common.Toolkit;

public static class Converters
{
  public static string BytesPerSecondToString(ulong _bytesPerSecond) => BytesPerSecondToString((double)_bytesPerSecond);

  public static string BytesPerSecondToString(double _bytesPerSecond)
  {
    var bitsPerSecond = _bytesPerSecond * 8;

    if (bitsPerSecond > 1024UL * 1024 * 1024 * 1024)
      return $"{bitsPerSecond / (1024UL * 1024 * 1024 * 1024):F2} Tbps";
    if (bitsPerSecond > 1024 * 1024 * 1024)
      return $"{bitsPerSecond / (1024 * 1024 * 1024):F2} Gbps";
    if (bitsPerSecond > 1024 * 1024)
      return $"{bitsPerSecond / (1024 * 1024):F2} Mbps";
    if (bitsPerSecond > 1024)
      return $"{bitsPerSecond / 1024:F2} Kbps";

    return $"{bitsPerSecond:F2} bps";
  }

}
