using Ax.Fw.Extensions;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.Interfaces;
using System.Net;

namespace SlowUdpPipe.Modules.SettingsProvider;

internal class SettingsProviderImpl : ISettingsProvider
{
  private static readonly EncryptionAlgorithm[] DEFAULT_CYPHERS = new[] {
    EncryptionAlgorithm.Aes128,
    EncryptionAlgorithm.Aes256,
    EncryptionAlgorithm.AesGcm128,
    EncryptionAlgorithm.AesGcm256,
    EncryptionAlgorithm.ChaCha20Poly1305,
    EncryptionAlgorithm.Xor
  };

  public SettingsProviderImpl(Options _options)
  {
    if (_options.Remote.IsNullOrEmpty() || !IPEndPoint.TryParse(_options.Remote, out var remote))
      throw new InvalidDataException("Remote address is missing!");
    if (_options.Local.IsNullOrEmpty() || !IPEndPoint.TryParse(_options.Local, out var local))
      throw new InvalidDataException("Local address is missing!");

    Remote = remote;
    Local = local;
    Key = _options.Key ?? throw new InvalidDataException("Key is missing!");

    var algs = new List<EncryptionAlgorithm>();
    if (_options.Ciphers?.Contains("aes-128") == true)
      algs.Add( EncryptionAlgorithm.Aes128);
    if (_options.Ciphers?.Contains("aes-256") == true)
      algs.Add(EncryptionAlgorithm.Aes256);
    if (_options.Ciphers?.Contains("aes-gcm-128") == true)
      algs.Add(EncryptionAlgorithm.AesGcm128);
    if (_options.Ciphers?.Contains("aes-gcm-256") == true)
      algs.Add(EncryptionAlgorithm.AesGcm256);
    if (_options.Ciphers?.Contains("chacha20-poly1305") == true)
      algs.Add(EncryptionAlgorithm.ChaCha20Poly1305);
    if (_options.Ciphers?.Contains("xor") == true)
      algs.Add(EncryptionAlgorithm.Xor);

    if (algs.Any())
      Algorithms = algs;
    else
      Algorithms = DEFAULT_CYPHERS;
  }

  public IPEndPoint Remote { get; }
  public IPEndPoint Local { get; }
  public string Key { get; }
  public IReadOnlyList<EncryptionAlgorithm> Algorithms { get; }

}
