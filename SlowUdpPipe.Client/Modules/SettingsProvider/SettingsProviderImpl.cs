using Ax.Fw.Extensions;
using Ax.Fw.JsonStorages;
using Ax.Fw.SharedTypes.Interfaces;
using SlowUdpPipe.Client.Data;
using SlowUdpPipe.Client.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    var jsonOptions = new JsonSerializerOptions()
    {
      PropertyNameCaseInsensitive = true,
      AllowTrailingCommas = true,
    };
    var jsonCtx = new ConfigFileJsonSerializationContext(jsonOptions);

    var config = new JsonStorage<IReadOnlyDictionary<string, UdpTunnelClientRawOptions>>(_options.ConfigFilePath, jsonCtx.IReadOnlyDictionaryStringUdpTunnelClientRawOptions, lifetime);
    Definitions = config;
  }

  public IObservable<IReadOnlyDictionary<string, UdpTunnelClientRawOptions>?> Definitions { get; }

}

[JsonSerializable(typeof(IReadOnlyDictionary<string, UdpTunnelClientRawOptions>))]
internal partial class ConfigFileJsonSerializationContext : JsonSerializerContext
{

}