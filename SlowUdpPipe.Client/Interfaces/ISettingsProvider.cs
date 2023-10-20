using SlowUdpPipe.Common.Data;
using System.Net;

namespace SlowUdpPipe.Client.Interfaces;

public interface ISettingsProvider
{
  IPEndPoint Remote { get; }
  IPEndPoint Local { get; }
  string Key { get; }
  EncryptionAlgorithm Algorithm { get; }
}
