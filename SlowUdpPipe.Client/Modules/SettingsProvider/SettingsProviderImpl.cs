using Ax.Fw.Extensions;
using Ax.Fw.JsonStorages;
using Ax.Fw.SharedTypes.Interfaces;
using SlowUdpPipe.Client.Data;
using SlowUdpPipe.Client.Interfaces;

namespace SlowUdpPipe.Client.Modules.SettingsProvider;

internal class SettingsProviderImpl : ISettingsProvider
{
  public SettingsProviderImpl(CommandLineOptions _options, IReadOnlyLifetime _lifetime)
  {
    if (_options.ConfigFilePath.IsNullOrWhiteSpace())
      throw new InvalidDataException("Path to config file is empty!");

    _ = File.ReadAllBytes(_options.ConfigFilePath);

    var lifetime = _lifetime.GetChildLifetime();
    if (lifetime == null)
      throw new InvalidOperationException($"Lifetime is already ended");

    var config = new JsonObservableStorage<IReadOnlyDictionary<string, UdpTunnelClientRawOptions>>(lifetime, _options.ConfigFilePath);
    Definitions = config.Changes;
  }

  public IObservable<IReadOnlyDictionary<string, UdpTunnelClientRawOptions>?> Definitions { get; }

}
