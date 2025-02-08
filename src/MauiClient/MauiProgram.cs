using Android.Util;
using Ax.Fw;
using Ax.Fw.DependencyInjection;
using Ax.Fw.Log;
using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.MauiClient.Data;
using CloakTunnel.MauiClient.Interfaces;
using CloakTunnel.MauiClient.Modules.PageController;
using CloakTunnel.MauiClient.Modules.PreferencesStorage;
using CloakTunnel.MauiClient.Modules.TunnelsConfCtrl;
using CloakTunnel.MauiClient.Modules.TunnelsController;
using CloakTunnel.MauiClient.Platforms.Android.Services;
using CommunityToolkit.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace CloakTunnel.MauiClient;

public static partial class MauiProgram
{
  public static MauiApp CreateMauiApp()
  {
    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

    var lifetime = new Lifetime();
    lifetime.DoOnEnded(() =>
    {
      Application.Current?.Quit();
    });

    var logsFolder = Path.Combine(FileSystem.Current.AppDataDirectory, "logs");
    if (!Directory.Exists(logsFolder))
      Directory.CreateDirectory(logsFolder);

    var logger = new GenericLog()
      .AttachFileLog(() => Path.Combine(logsFolder, $"{DateTimeOffset.UtcNow:yyyy-MM-dd}.log"), TimeSpan.FromSeconds(1))
      .AttachCustomLog(_logEntry =>
      {
        static string getLogEntryString(LogEntry _entry) 
          => $"| {_entry.GetTypePrefix()} | {_entry.Time:dd.MM.yyyy HH:mm:ss.fff} || {_entry.Scope} || {_entry.Text}";

        if (_logEntry.Type == LogEntryType.INFO)
          Log.Info(AppConsts.LOG_TAG, getLogEntryString(_logEntry));
        else if (_logEntry.Type == LogEntryType.WARN)
          Log.Warn(AppConsts.LOG_TAG, getLogEntryString(_logEntry));
        else if (_logEntry.Type == LogEntryType.ERROR)
          Log.Error(AppConsts.LOG_TAG, getLogEntryString(_logEntry));
      });

    lifetime.ToDisposeOnEnded(logger);

    var appStartedVersionStr = $"============= app is launched ({AppInfo.Current.VersionString}) =============";
    var line = new string(Enumerable.Repeat('=', appStartedVersionStr.Length).ToArray());
    logger.Info(line);
    logger.Info(appStartedVersionStr);
    logger.Info(line);
    lifetime.DoOnEnded(() =>
    {
      logger.Info("=========================================");
      logger.Info("============= app is closed =============");
      logger.Info("=========================================");
    });

    lifetime.ToDisposeOnEnded(FileLoggerCleaner.Create(new DirectoryInfo(logsFolder), false, GetLogFileRegex(), TimeSpan.FromDays(30), false, null, _file =>
    {
      logger.Info($"Old log file was removed: '{_file.Name}'");
    }));

    Container = AppDependencyManager
      .Create()
      .AddSingleton<ILifetime>(lifetime)
      .AddSingleton<IReadOnlyLifetime>(lifetime)
      .AddSingleton<ILog>(logger)
      .AddModule<UdpTunnelService, IUdpTunnelService>()
      .AddModule<TunnelsControllerImpl, IUdpTunnelCtrl>()
      .AddModule<TunnelsConfCtrlImpl, ITunnelsConfCtrl>()
      .AddModule<PreferencesStorageImpl, IPreferencesStorage>()
      .AddModule<PagesControllerImpl, IPagesController>()
      .ActivateOnStart<IUdpTunnelCtrl>()
      .Build();

    logger.Info($"Dependencies are installed");

    var builder = MauiApp.CreateBuilder();
    builder
      .UseMauiApp<App>()
      .UseMauiCommunityToolkit()
      .UseSkiaSharp()
      .ConfigureFonts(_fonts =>
      {
        _fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        _fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
      });

    logger.Info($"MauiApp is building...");

    return builder.Build();
  }

  [NotNull]
  public static IReadOnlyDependencyContainer? Container { get; private set; }

  public static void ExitApp()
  {
    var appLifetime = Container.LocateOrDefault<ILifetime>();
    appLifetime?.End();
  }

  private static void CurrentDomain_UnhandledException(
    object sender,
    UnhandledExceptionEventArgs e)
  {
    Log.Error(AppConsts.LOG_TAG, $"UnhandledException: {e.ExceptionObject as Exception}");
  }

  [GeneratedRegex(@"^.+\.log$")]
  private static partial Regex GetLogFileRegex();

}