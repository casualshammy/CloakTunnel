using Ax.Fw;
using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using CommandLine;
using JustLogger;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Toolkit;
using SlowUdpPipe.Interfaces;
using SlowUdpPipe.Modules.SettingsProvider;
using SlowUdpPipe.Server.Data;
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
      logger.Warn($"===== Use this key in tunnel's description =====");
      logger.Info(Utilities.GetRandomString(32, false));
      logger.Warn($"================================================");
      return;
    }

    var options = Parser.Default
      .ParseArguments<CommandLineOptions>(_args);

    if (options.Value == null)
      return;

    SettingsProviderImpl settingsProvider;
    try
    {
      settingsProvider = new SettingsProviderImpl(options.Value, lifetime);
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
