﻿using Ax.Fw;
using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using CommandLine;
using JustLogger;
using JustLogger.Interfaces;
using SlowUdpPipe.Client.Data;
using SlowUdpPipe.Client.Interfaces;
using SlowUdpPipe.Client.Modules.SettingsProvider;
using SlowUdpPipe.Client.Modules.UdpProxy;
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
      logger.Info($"Client stopped");
      logger.Info($"-------------------------------------------");
    });

    var version = new SerializableVersion(assembly.GetName().Version ?? new Version(0, 0, 0, 0));
    logger.Info($"-------------------------------------------");
    logger.Info($"SlowUdpPipe Client Started");
    logger.Info($"Version: {version}");
    logger.Info($"OS: {Environment.OSVersion} {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}");
    logger.Info($"-------------------------------------------");

    _ = AppDependencyManager
      .Create()
      .AddSingleton<IReadOnlyLifetime>(lifetime)
      .AddSingleton<ILifetime>(lifetime)
      .AddSingleton<ILogger>(logger)
      .AddSingleton<ILoggerDisposable>(logger)
      .AddSingleton<ISettingsProvider>(settingsProvider)
      .AddModule<UdpProxyImpl>()
      .ActivateOnStart<UdpProxyImpl>()
      .Build();

    lifetime.InstallConsoleCtrlCHook();

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
