using Ax.Fw;
using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using CommandLine;
using JustLogger;
using JustLogger.Interfaces;
using SlowUdpPipe.Client.Interfaces;
using SlowUdpPipe.Client.Modules.SettingsProvider;
using SlowUdpPipe.Common.Toolkit;
using System.Reflection;

namespace SlowUdpPipe.Client;

public class Program
{
  public static async Task Main(string[] _args)
  {
    var assembly = Assembly.GetExecutingAssembly() ?? throw new Exception("Can't get assembly!");
    var workingDir = Path.GetDirectoryName(assembly.Location) ?? throw new Exception("Can't get working dir!");

    var lifetime = new Lifetime();
    using var logger = new ConsoleLogger();

    if (_args?.Length == 1 && _args[0] == "test")
    {
      logger.Info($"================ Running benchmark, please wait.. ================");
      var result = EncryptionAlgorithmsTest.Test(lifetime, null);
      var minScore = result.Min(_ => _.Value ?? long.MaxValue);
      logger.Warn($"=========== Performance per algorithm (more is better) ===========");
      foreach (var (algo, score) in result)
      {
        if (score == null)
          logger.Warn($"Ciphers '{algo}' is not supported on this platform");
        else
          logger.Info($"{algo}: {minScore * 100 / score}");
      }
      logger.Warn($"==================================================================");
      return;
    }

    if (_args?.Length == 1 && _args[0] == "genkey")
    {
      logger.Warn($"===== Use this key in '--key' argument =====");
      logger.Info(Utilities.GetRandomString(32, false));
      logger.Warn($"============================================");
      return;
    }

    var options = Parser.Default
      .ParseArguments<Options>(_args);

    if (options.Value == null)
      return;

    SettingsProviderImpl settingsProvider;
    try
    {
      settingsProvider = new SettingsProviderImpl(options.Value);
    }
    catch (Exception ex)
    {
      logger.Error(ex.Message);
      return;
    }

    lifetime.DoOnEnded(() =>
    {
      logger.Info($"-------------------------------------------");
      logger.Info($"Server stopped");
      logger.Info($"-------------------------------------------");
    });

    var version = new SerializableVersion(assembly.GetName().Version ?? new Version(0, 0, 0, 0));
    logger.Info($"-------------------------------------------");
    logger.Info($"SlowUdpPipe Client Started");
    logger.Info($"Version: {version}");
    logger.Info($"Remote: {options.Value.Remote}");
    logger.Info($"Local: {options.Value.Local}");
    logger.Info($"Cypher: {options.Value.Cipher ?? SettingsProviderImpl.DEFAULT_CYPHER}");
    logger.Info($"OS: {Environment.OSVersion} {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}");
    logger.Info($"-------------------------------------------");

    var depMgr = DependencyManagerBuilder
      .Create(lifetime, assembly)
      .AddSingleton<ILogger>(logger)
      .AddSingleton<ILoggerDisposable>(logger)
      .AddSingleton<ILifetime>(lifetime)
      .AddSingleton<IReadOnlyLifetime>(lifetime)
      .AddSingleton<ISettingsProvider>(settingsProvider)
      .Build();

    await Task.Delay(-1);

    lifetime.End();
  }

}

public class Options
{
  [Option("remote", Required = true, HelpText = "Processed data will be sent to this host")]
  public string? Remote { get; init; }

  [Option("local", Required = true, HelpText = "SlowUdpTunnel will listen this address for incoming data")]
  public string? Local { get; init; }

  [Option("cipher", Required = false, HelpText = "Cryptographic algorithm used for traffic encryption/decryption (default: aes-gcm-128)")]
  public string? Cipher { get; init; }

  [Option("key", Required = true, HelpText = "Key used for encryption")]
  public string? Key { get; init; }

}