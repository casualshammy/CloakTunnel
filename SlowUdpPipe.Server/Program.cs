using Ax.Fw;
using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using FluentArgs;
using JustLogger;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Toolkit;
using SlowUdpPipe.Interfaces;
using SlowUdpPipe.Modules.SettingsProvider;
using SlowUdpPipe.Modules.UdpProxy;
using System.Reflection;

namespace SlowUdpPipe;

public class Program
{
  public static async Task Main(string[] _args)
  {
    var assembly = Assembly.GetExecutingAssembly() ?? throw new Exception("Can't get assembly!");

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

    var configFilePath = (string?)null;
    FluentArgsBuilder
      .New()
      .Parameter("-c", "--config").WithDescription("Path to config file").IsOptional()
      .Call(_configPath =>
      {
        configFilePath = _configPath;
      })
      .Parse(_args ?? []);

    SettingsProviderImpl settingsProvider;
    try
    {
      settingsProvider = new SettingsProviderImpl(configFilePath, lifetime);
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

    _ = AppDependencyManager
      .Create()
      .AddSingleton<ILogger>(logger)
      .AddSingleton<ILoggerDisposable>(logger)
      .AddSingleton<ILifetime>(lifetime)
      .AddSingleton<IReadOnlyLifetime>(lifetime)
      .AddSingleton<ISettingsProvider>(settingsProvider)
      .AddModule<UdpProxyImpl>()
      .ActivateOnStart<UdpProxyImpl>()
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
