namespace CloakTunnel.Common.Data;

public record UdpTunnelClientOptions(
  EndpointType RemoteEndpointType,
  Uri LocalEndpoint,
  Uri RemoteEndpoint,
  EncryptionAlgorithm EncryptionAlgorithm,
  string PassKey);
