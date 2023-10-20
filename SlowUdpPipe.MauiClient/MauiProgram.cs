using Ax.Fw;
using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using CommunityToolkit.Maui;
using Grace.DependencyInjection;
using JustLogger;
using JustLogger.Interfaces;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using SlowUdpPipe.MauiClient.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SlowUdpPipe.MauiClient;

public static class MauiProgram
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

    var fileLogger = new FileLogger(() => Path.Combine(logsFolder, $"{DateTimeOffset.UtcNow:yyyy-MM-dd}.log"), 1000);
    var androidLogger = new Platforms.Android.Toolkit.AndroidLogger("slowudppipe");
    var logger = lifetime.ToDisposeOnEnded(new CompositeLogger(androidLogger, fileLogger));

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

    lifetime.ToDisposeOnEnded(FileLoggerCleaner.Create(new DirectoryInfo(logsFolder), false, new Regex(@"^.+\.log$"), TimeSpan.FromDays(30), null, _file =>
    {
      logger.Info($"Old file was removed: '{_file.Name}'");
    }));

    var assembly = Assembly.GetExecutingAssembly();
    var containerBuilder = DependencyManagerBuilder
      .Create(lifetime, assembly)
      .AddSingleton<ILifetime>(lifetime)
      .AddSingleton<IReadOnlyLifetime>(lifetime)
      .AddSingleton<ILogger>(logger)
      .Build();

    logger.Info($"Dependencies are installed");

    Container = containerBuilder.ServiceProvider;

    var builder = MauiApp.CreateBuilder();
    builder
      .UseMauiApp<App>()
      .UseMauiCommunityToolkit()
      .UseLocalNotification(SetupLocalNotification)
      .ConfigureFonts(_fonts =>
      {
        _fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        _fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
      });

    logger.Info($"MauiApp is building...");

    return builder.Build();
  }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
  public static IInjectionScope Container { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  private static void SetupLocalNotification(ILocalNotificationBuilder _builder)
  {
    _builder.AddAndroid(_android =>
    {
      _android.AddChannel(new NotificationChannelRequest
      {
        Id = Consts.NOTIFICATION_CHANNEL_MAIN,
        Name = "Generic notifications",
        EnableSound = true,
        EnableVibration = true,
        Importance = AndroidImportance.Max
      });
    });
  }

}