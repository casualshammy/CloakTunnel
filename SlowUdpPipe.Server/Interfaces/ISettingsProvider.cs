using SlowUdpPipe.Common.Data;
using System.Net;

namespace SlowUdpPipe.Interfaces;

public interface ISettingsProvider
{
  IPEndPoint Remote { get; }
  IPEndPoint Local { get; }
  string Key { get; }
  IReadOnlyList<EncryptionAlgorithm> Algorithms { get; }
}
