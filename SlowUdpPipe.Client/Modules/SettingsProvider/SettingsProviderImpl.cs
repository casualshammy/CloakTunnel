using Ax.Fw.Extensions;
using SlowUdpPipe.Client.Interfaces;
using SlowUdpPipe.Common.Data;
using System.Net;

namespace SlowUdpPipe.Client.Modules.SettingsProvider;

internal class SettingsProviderImpl : ISettingsProvider
{
  internal static readonly string DEFAULT_CYPHER = "aes-gcm-128";

  public SettingsProviderImpl(Options _options)
  {
    if (_options.Remote.IsNullOrEmpty() || !IPEndPoint.TryParse(_options.Remote, out var remote))
      throw new InvalidDataException("Remote address is missing!");
    if (_options.Local.IsNullOrEmpty() || !IPEndPoint.TryParse(_options.Local, out var local))
      throw new InvalidDataException("Local address is missing!");

    Remote = remote;
    Local = local;
    Key = _options.Key ?? throw new InvalidDataException("Key is missing!");

    var alg = _options.Cipher ?? DEFAULT_CYPHER;
    if (alg == "aes-128")
      Algorithm = EncryptionAlgorithm.Aes128;
    else if (alg == "aes-256")
      Algorithm = EncryptionAlgorithm.Aes256;
    else if (alg == "aes-gcm-128")
      Algorithm = EncryptionAlgorithm.AesGcm128;
    else if (alg == "aes-gcm-256")
      Algorithm = EncryptionAlgorithm.AesGcm256;
    else if (alg == "chacha20-poly1305")
      Algorithm = EncryptionAlgorithm.ChaCha20Poly1305;
    else
      Algorithm = EncryptionAlgorithm.AesGcm128;
  }

  public IPEndPoint Remote { get; }
  public IPEndPoint Local { get; }
  public string Key { get; }
  public EncryptionAlgorithm Algorithm { get; }

}
