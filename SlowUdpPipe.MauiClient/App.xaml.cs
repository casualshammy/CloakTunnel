using JustLogger.Interfaces;
using SlowUdpPipe.MauiClient.Toolkit;

namespace SlowUdpPipe.MauiClient;

public partial class App : CMauiApplication
{
  public App()
  {
    var log = Container.Locate<ILogger>();

    log.Info($"App is starting...");
    InitializeComponent();
    log.Info($"App is started");

    MainPage = new NavigationAppShell();
  }
}