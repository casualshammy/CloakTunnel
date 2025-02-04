namespace CloakTunnel.Common.Data;

public record TunnelServerOptions(
  EndpointType BindType,
  Uri BindUri,
  Uri ForwardUri,
  EncryptionAlgorithm Encryption,
  string PassKey);
