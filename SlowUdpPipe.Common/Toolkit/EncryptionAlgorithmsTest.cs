using Ax.Fw;
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
    _logger.Info($"================ Running benchmark, please wait.. ================");
    var result = Test(_lifetime, null);
    var minScore = result.Min(_ => _.Value ?? long.MaxValue);
    _logger.Warn($"=========== Performance per algorithm (more is better) ===========");
    foreach (var (algo, score) in result)
    {
      if (score == null)
        _logger.Warn($"Ciphers '{algo}' is not supported on this platform");
      else if (algo == EncryptionAlgorithm.Xor)
        _logger.Info($"{algo} (may be detectable): {minScore * 100 / score}");
      else
        _logger.Info($"{algo}: {minScore * 100 / score}");
    }
    _logger.Warn($"==================================================================");
  }

  public static Dictionary<EncryptionAlgorithm, long?> Test(IReadOnlyLifetime _lifetime, Action<double>? _progress)
  {
    TestInternal(_lifetime, _p => _progress?.Invoke(_p / 2));
    var result = TestInternal(_lifetime, _p => _progress?.Invoke(0.5 + _p / 2));
    return result;
  }

  private static Dictionary<EncryptionAlgorithm, long?> TestInternal(IReadOnlyLifetime _lifetime, Action<double>? _progress)
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
    var sw = Stopwatch.StartNew();
    var result = new Dictionary<EncryptionAlgorithm, long?>();
    foreach (var (algorithm, algorithmFactory) in algos)
    {
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

        result[algorithm] = sw.ElapsedMilliseconds;
      }
      catch
      {
        _progress?.Invoke((iteration + iterations) / workCount);
        result[algorithm] = null;
      }
    }

    return result;
  }
}
