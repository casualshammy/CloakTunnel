using Ax.Fw.Crypto;
using Ax.Fw.SharedTypes.Interfaces;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Data;
using System.Diagnostics;
using System.Text;

namespace SlowUdpPipe.Common.Toolkit;

public static class EncryptionAlgorithmsTest
{
  public static void TestAndPrintInConsole(IReadOnlyLifetime _lifetime, ILogger _logger)
  {
    _logger.Warn($"================ Running benchmark, please wait.. ================");
    _logger.Warn($"=========== Performance per algorithm (more is better) ===========");
    foreach (var result in Benchmark(_lifetime, null))
    {
      var algoSlug = Consts.ENCRYPTION_ALG_SLUG[result.Algorithm];
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
    var algos = new Dictionary<EncryptionAlgorithm, Func<ICryptoAlgorithm>>()
    {
      { EncryptionAlgorithm.Aes128, () => new AesCbc(_lifetime, key, 128) },
      { EncryptionAlgorithm.Aes256,() =>  new AesCbc(_lifetime, key, 256) },
      { EncryptionAlgorithm.AesGcm128,() =>  new AesWithGcm(_lifetime, key, 128) },
      { EncryptionAlgorithm.AesGcm256, () => new AesWithGcm(_lifetime, key, 256) },
      { EncryptionAlgorithm.ChaCha20Poly1305,() =>  new ChaCha20WithPoly1305(_lifetime, key) },
      { EncryptionAlgorithm.Xor, () => new Xor(Encoding.UTF8.GetBytes(key)) }
    };

    const int iterations = 1000;
    var workCount = algos.Count * iterations;
    var iteration = 0d;

    var buffer = new byte[128 * 1024];
    Random.Shared.NextBytes(buffer);
    var totalWorkBytes = iterations * buffer.Length;
    var sw = Stopwatch.StartNew();
    foreach (var (algorithm, algorithmFactory) in algos)
    {
      long? result = null;
      try
      {
        var algo = algorithmFactory();
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
          var encrypted = algo.Encrypt(buffer);
          algo.Decrypt(encrypted);
          _progress?.Invoke(++iteration / workCount);
        }
        result = sw.ElapsedMilliseconds;
      }
      catch
      {
        _progress?.Invoke((iteration + iterations) / workCount);
      }

      yield return new CryptoBenchmarkResult(algorithm, totalWorkBytes, result);
    }
  }
}
