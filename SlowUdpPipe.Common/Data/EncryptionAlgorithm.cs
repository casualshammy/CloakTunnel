namespace SlowUdpPipe.Common.Data;

public enum EncryptionAlgorithm
{
  None = 0,
  Aes128,
  Aes256,
  AesGcm128,
  AesGcm256,
  ChaCha20Poly1305,
  Xor
}
