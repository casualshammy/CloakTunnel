using Ax.Fw.SharedTypes.Interfaces;
using SlowUdpPipe.MauiClient.Interfaces;

namespace SlowUdpPipe.MauiClient.Toolkit;

public abstract class CContentPage : ContentPage
{
  private readonly IPagesController p_pageController;

  protected CContentPage()
  {
    Container = MauiProgram.Container;
    p_pageController = Container.Locate<IPagesController>();
  }

  public IReadOnlyDependencyContainer Container { get; }

  protected override void OnAppearing()
  {
    base.OnAppearing();
    p_pageController.OnPageActivated(this);
  }

}