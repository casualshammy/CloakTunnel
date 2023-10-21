using Ax.Fw;
using Ax.Fw.Crypto;
using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using CommandLine;
using JustLogger;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Toolkit;
using SlowUdpPipe.Interfaces;
using SlowUdpPipe.Modules.SettingsProvider;
using System.Diagnostics;
using System.Reflection;

namespace SlowUdpPipe;

public class Program
{
  public static async Task Main(string[] _args)
  {
    var assembly = Assembly.GetExecutingAssembly() ?? throw new Exception("Can't get assembly!");
    var workingDir = Path.GetDirectoryName(assembly.Location) ?? throw new Exception("Can't get working dir!");

    var lifetime = new Lifetime();
    var logger = new ConsoleLogger();

    if (_args?.Length == 1 && _args[0] == "test")
    {
      EncryptionAlgorithmsTest.TestAndPrintInConsole(lifetime, logger);
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
    logger.Info($"SlowUdpPipe Server Started");
    logger.Info($"Version: {version}");
    logger.Info($"Remote: {options.Value.Remote}");
    logger.Info($"Local: {options.Value.Local}");
    logger.Info($"Cyphers: {options.Value.Ciphers ?? "all available"}");
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

    void onCancelKeyPress(object? _o, ConsoleCancelEventArgs _e)
    {
      _e.Cancel = true;
      Console.CancelKeyPress -= onCancelKeyPress;
      lifetime.End();
    }
    Console.CancelKeyPress += onCancelKeyPress;

    try
    {
      await Task.Delay(-1, lifetime.Token);
    }
    catch (TaskCanceledException)
    {
      // ignore
    }
  }

}

public class Options
{
  [Option("remote", Required = true, HelpText = "Processed data will be sent to this host")]
  public string? Remote { get; init; }

  [Option("local", Required = true, HelpText = "SlowUdpTunnel will listen this address for incoming data")]
  public string? Local { get; init; }

  [Option("ciphers", Required = false, HelpText = "Cryptographic algorithms used for traffic encryption/decryption (default: all)")]
  public string? Ciphers { get; init; }

  [Option("key", Required = true, HelpText = "Key used for encryption")]
  public string? Key { get; init; }

}