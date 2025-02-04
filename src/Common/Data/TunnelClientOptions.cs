namespace CloakTunnel.Common.Data;

public record TunnelClientOptions(
  EndpointType RemoteType,
  Uri BindUri,
  Uri ForwardUri,
  EncryptionAlgorithm Encryption,
  string PassKey);
