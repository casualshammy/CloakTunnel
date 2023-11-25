namespace SlowUdpPipe.Common.Data;

public static class Consts
{
  public const int MAX_UDP_PACKET_PAYLOAD_SIZE = 508;
  public const EncryptionAlgorithm DEFAULT_ENCRYPTION = EncryptionAlgorithm.AesGcm128;

  public static readonly IReadOnlyDictionary<EncryptionAlgorithm, string> ENCRYPTION_ALG_SLUG;
  public static readonly IReadOnlyDictionary<string, EncryptionAlgorithm> ENCRYPTION_ALG_SLUG_REVERSE;
  public static readonly IReadOnlyList<EncryptionAlgorithm> ALL_CYPHERS;

  static Consts()
  {
    ENCRYPTION_ALG_SLUG = new Dictionary<EncryptionAlgorithm, string>() {
      { EncryptionAlgorithm.Aes128, "aes-128" },
      { EncryptionAlgorithm.Aes256, "aes-256" },
      { EncryptionAlgorithm.AesGcm128, "aes-gcm-128" },
      { EncryptionAlgorithm.AesGcm256, "aes-gcm-256" },
      { EncryptionAlgorithm.AesGcmObfs128, "aes-gcm-obfs-128" },
      { EncryptionAlgorithm.AesGcmObfs256, "aes-gcm-obfs-256" },
      { EncryptionAlgorithm.ChaCha20Poly1305, "chacha20-poly1305" },
      { EncryptionAlgorithm.Xor, "xor" }
    };

    ENCRYPTION_ALG_SLUG_REVERSE = new Dictionary<string, EncryptionAlgorithm>() {
      { "aes-128", EncryptionAlgorithm.Aes128 },
      { "aes-256", EncryptionAlgorithm.Aes256 },
      { "aes-gcm-128", EncryptionAlgorithm.AesGcm128 },
      { "aes-gcm-256", EncryptionAlgorithm.AesGcm256 },
      { "aes-gcm-obfs-128", EncryptionAlgorithm.AesGcmObfs128 },
      { "aes-gcm-obfs-256", EncryptionAlgorithm.AesGcmObfs256 },
      { "chacha20-poly1305", EncryptionAlgorithm.ChaCha20Poly1305 },
      { "xor", EncryptionAlgorithm.Xor }
    };

    ALL_CYPHERS = new EncryptionAlgorithm[] {
      EncryptionAlgorithm.Aes128,
      EncryptionAlgorithm.Aes256,
      EncryptionAlgorithm.AesGcm128,
      EncryptionAlgorithm.AesGcm256,
      EncryptionAlgorithm.AesGcmObfs128,
      EncryptionAlgorithm.AesGcmObfs256,
      EncryptionAlgorithm.ChaCha20Poly1305,
      EncryptionAlgorithm.Xor
    };
  }

}
