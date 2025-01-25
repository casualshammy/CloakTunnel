using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Data;
using System.Diagnostics;

namespace CloakTunnel.Common.Toolkit;

public static class EncryptionAlgorithmsTest
{
  public static void TestAndPrintInConsole(IReadOnlyLifetime _lifetime, ILog _logger)
  {
    _logger.Warn($"================ Running benchmark, please wait.. ================");
    _logger.Warn($"=========== Performance per algorithm (more is better) ===========");
    foreach (var result in Benchmark(_lifetime, null))
    {
      var algoSlug = EncryptionToolkit.ENCRYPTION_ALG_SLUG[result.Algorithm];
      if (result.ResultMs == null)
        _logger.Warn($"Algorithm '{algoSlug}' is not supported on this platform");
      else
        _logger.Info($"{algoSlug}: {Converters.BytesPerSecondToString(result.WorkVolumeBytes / (result.ResultMs.Value / 1000d))}");
    }
    _logger.Warn($"==================================================================");
  }

  public static IEnumerable<CryptoBenchmarkResult> Benchmark(IReadOnlyLifetime _lifetime, Action<double>? _progress)
  {
    var key = "123456789";

    const int iterations = 1000;
    var workCount = EncryptionToolkit.ALL_CYPHERS.Count * iterations;
    var iteration = 0d;

    var bigBuffer = new byte[128 * 1024];
    Random.Shared.NextBytes(bigBuffer);
    var smallBuffer = new byte[EncryptionToolkit.MAX_UDP_PACKET_PAYLOAD_SIZE / 2];
    Random.Shared.NextBytes(smallBuffer);

    var totalWorkBytes = iterations * (bigBuffer.Length + smallBuffer.Length);
    var sw = Stopwatch.StartNew();
    foreach (var algo in EncryptionToolkit.ALL_CYPHERS)
    {
      long? result = null;
      try
      {
        var transform = EncryptionToolkit.GetCrypto(algo, _lifetime, key);
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
          var encryptedBig = transform.Encrypt(bigBuffer);
          transform.Decrypt(encryptedBig);

          var encryptedSmall = transform.Encrypt(smallBuffer);
          transform.Decrypt(encryptedSmall);

          _progress?.Invoke(++iteration / workCount);
        }
        result = sw.ElapsedMilliseconds;
      }
      catch
      {
        iteration += iterations;
        _progress?.Invoke(iteration / workCount);
      }

      yield return new CryptoBenchmarkResult(algo, totalWorkBytes, result);
    }
  }
}
