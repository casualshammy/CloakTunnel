using Ax.Fw.Crypto;
using Ax.Fw.SharedTypes.Interfaces;
using SlowUdpPipe.Common.Data;
using System.Diagnostics;

namespace SlowUdpPipe.Common.Toolkit;

public static class EncryptionAlgorithmsTest
{
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
      { EncryptionAlgorithm.ChaCha20Poly1305,() =>  new ChaCha20WithPoly1305(_lifetime, key) }
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
