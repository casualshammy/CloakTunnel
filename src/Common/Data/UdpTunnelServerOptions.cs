namespace CloakTunnel.Common.Data;

public record UdpTunnelServerOptions(
  EndpointType LocalEndpointType,
  Uri LocalEndpoint,
  Uri RemoteEndpoint,
  EncryptionAlgorithm EncryptionAlgorithm,
  string PassKey);
