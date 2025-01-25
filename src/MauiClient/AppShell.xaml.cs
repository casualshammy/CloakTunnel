using Ax.Fw.SharedTypes.Interfaces;
using CommunityToolkit.Maui.Alerts;
using CloakTunnel.MauiClient.Interfaces;

namespace CloakTunnel.MauiClient;

public partial class NavigationAppShell : Shell
{
  private readonly ILifetime p_lifetime;
  private readonly IPagesController p_pageController;
  private DateTimeOffset p_lastTimeBackClicked = DateTimeOffset.MinValue;

  public NavigationAppShell()
  {
    InitializeComponent();
    if (Application.Current is not IMauiApp app)
      throw new ApplicationException($"App is not '{nameof(IMauiApp)}'");

    var log = app.Container.Locate<ILog>();
    log.Info($"App shell is started");

    p_lifetime = app.Container.Locate<ILifetime>();
    p_pageController = app.Container.Locate<IPagesController>();
  }

  protected override bool OnBackButtonPressed()
  {
    var mainPage = p_pageController.MainPage;
    var currentPage = p_pageController.CurrentPage;
    if (mainPage == currentPage)
    {
      var now = DateTimeOffset.UtcNow;
      if (now - p_lastTimeBackClicked < TimeSpan.FromSeconds(3))
      {
        p_lifetime.End();
        return false;
      }

      p_lastTimeBackClicked = now;
      Toast
        .Make("Press back again to exit", CommunityToolkit.Maui.Core.ToastDuration.Short)
        .Show();

      return true;
    }

    return base.OnBackButtonPressed();
  }

}