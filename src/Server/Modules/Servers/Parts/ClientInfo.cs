using Ax.Fw.SharedTypes.Interfaces;
using System.Net.Sockets;

namespace CloakTunnel.Server.Modules.Servers.Parts;

public record ClientInfo(
  Socket Socket, 
  ILifetime Lifetime, 
  ICryptoAlgorithm Decryptor)
{
  public long Timestamp { get; set; }
};
