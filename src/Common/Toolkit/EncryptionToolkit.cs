using Ax.Fw.Crypto;
using Ax.Fw.SharedTypes.Data.Crypto;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Data;
using System.Text;

namespace CloakTunnel.Common.Toolkit;

public static class EncryptionToolkit
{
  public const int MAX_UDP_PACKET_PAYLOAD_SIZE = 508;
  public const EncryptionAlgorithm DEFAULT_ENCRYPTION = EncryptionAlgorithm.AesGcm128;

  public static readonly IReadOnlyDictionary<EncryptionAlgorithm, string> ENCRYPTION_ALG_SLUG;
  public static readonly IReadOnlyDictionary<string, EncryptionAlgorithm> ENCRYPTION_ALG_SLUG_REVERSE;
  public static readonly IReadOnlyList<EncryptionAlgorithm> ALL_CYPHERS;

  static EncryptionToolkit()
  {
    ENCRYPTION_ALG_SLUG = new Dictionary<EncryptionAlgorithm, string>() {
      { EncryptionAlgorithm.AesGcm128, "aes-gcm-128" },
      { EncryptionAlgorithm.AesGcm256, "aes-gcm-256" },
      { EncryptionAlgorithm.AesGcmObfs128, "aes-gcm-obfs-128" },
      { EncryptionAlgorithm.AesGcmObfs256, "aes-gcm-obfs-256" },
      { EncryptionAlgorithm.ChaCha20Poly1305, "chacha20-poly1305" },
      { EncryptionAlgorithm.Xor, "xor" }
    };

    ENCRYPTION_ALG_SLUG_REVERSE = new Dictionary<string, EncryptionAlgorithm>() {
      { "aes-gcm-128", EncryptionAlgorithm.AesGcm128 },
      { "aes-gcm-256", EncryptionAlgorithm.AesGcm256 },
      { "aes-gcm-obfs-128", EncryptionAlgorithm.AesGcmObfs128 },
      { "aes-gcm-obfs-256", EncryptionAlgorithm.AesGcmObfs256 },
      { "chacha20-poly1305", EncryptionAlgorithm.ChaCha20Poly1305 },
      { "xor", EncryptionAlgorithm.Xor }
    };

    ALL_CYPHERS = new EncryptionAlgorithm[] {
      EncryptionAlgorithm.AesGcm128,
      EncryptionAlgorithm.AesGcm256,
      EncryptionAlgorithm.AesGcmObfs128,
      EncryptionAlgorithm.AesGcmObfs256,
      EncryptionAlgorithm.ChaCha20Poly1305,
      EncryptionAlgorithm.Xor
    };
  }

  public static ICryptoAlgorithm GetCrypto(EncryptionAlgorithm _algo, IReadOnlyLifetime _lifetime, string _key)
  {
    return _algo switch
    {
      EncryptionAlgorithm.AesGcm128 => _lifetime.ToDisposeOnEnding(new AesWithGcm(_key, EncryptionKeyLength.Bits128)),
      EncryptionAlgorithm.AesGcm256 => _lifetime.ToDisposeOnEnding(new AesWithGcm(_key, EncryptionKeyLength.Bits256)),
      EncryptionAlgorithm.AesGcmObfs128 => _lifetime.ToDisposeOnEnding(new AesWithGcmObfs(_key, MAX_UDP_PACKET_PAYLOAD_SIZE, EncryptionKeyLength.Bits128)),
      EncryptionAlgorithm.AesGcmObfs256 => _lifetime.ToDisposeOnEnding(new AesWithGcmObfs(_key, MAX_UDP_PACKET_PAYLOAD_SIZE, EncryptionKeyLength.Bits256)),
      EncryptionAlgorithm.ChaCha20Poly1305 => _lifetime.ToDisposeOnEnding(new ChaCha20WithPoly1305(_key)),
      EncryptionAlgorithm.Xor => new Xor(Encoding.UTF8.GetBytes(_key)),
      _ => throw new NotSupportedException()
    };
  }

}
