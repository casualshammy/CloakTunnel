using Ax.Fw;
using Ax.Fw.DependencyInjection;
using Ax.Fw.Log;
using Ax.Fw.SharedTypes.Interfaces;
using CommunityToolkit.Maui;
using CloakTunnel.MauiClient.Interfaces;
using CloakTunnel.MauiClient.Modules.PageController;
using CloakTunnel.MauiClient.Modules.PreferencesStorage;
using CloakTunnel.MauiClient.Modules.TunnelsConfCtrl;
using CloakTunnel.MauiClient.Modules.TunnelsController;
using CloakTunnel.MauiClient.Platforms.Android.Services;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace CloakTunnel.MauiClient;

public static partial class MauiProgram
{
  public static MauiApp CreateMauiApp()
  {
    var lifetime = new Lifetime();
    lifetime.DoOnEnded(() =>
    {
      Application.Current?.Quit();
    });

    var logsFolder = Path.Combine(FileSystem.Current.AppDataDirectory, "logs");
    if (!Directory.Exists(logsFolder))
      Directory.CreateDirectory(logsFolder);

    var logger = new GenericLog()
      .AttachFileLog(() => Path.Combine(logsFolder, $"{DateTimeOffset.UtcNow:yyyy-MM-dd}.log"), TimeSpan.FromSeconds(1));

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

  [GeneratedRegex(@"^.+\.log$")]
  private static partial Regex GetLogFileRegex();

}