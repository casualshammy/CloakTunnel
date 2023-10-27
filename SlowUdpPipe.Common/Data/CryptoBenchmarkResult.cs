namespace SlowUdpPipe.Common.Data;

public readonly record struct CryptoBenchmarkResult(EncryptionAlgorithm Algorithm, int WorkVolumeBytes, long? ResultMs);
