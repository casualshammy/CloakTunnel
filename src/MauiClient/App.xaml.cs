using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.MauiClient.Toolkit;

namespace CloakTunnel.MauiClient;

public partial class App : CMauiApplication
{
  public App()
  {
    var log = Container.Locate<ILog>();

    log.Info($"App is starting...");
    InitializeComponent();
    log.Info($"App is started");

    MainPage = new NavigationAppShell();
  }
}