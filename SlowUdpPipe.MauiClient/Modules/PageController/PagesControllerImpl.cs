using Ax.Fw.Attributes;
using Ax.Fw.DependencyInjection;
using SlowUdpPipe.MauiClient.Interfaces;

namespace SlowUdpPipe.MauiClient.Modules.PageController;

internal class PagesControllerImpl : IPagesController, IAppModule<PagesControllerImpl>
{
  public static PagesControllerImpl ExportInstance(IAppDependencyCtx _ctx)
  {
    return new();
  }

  private volatile Page? p_currentPage;
  private volatile Page? p_mainPage;

  public PagesControllerImpl() { }

  public Page? CurrentPage => p_currentPage;
  public Page? MainPage => p_mainPage;

  public void OnPageActivated(Page _page) => Interlocked.Exchange(ref p_currentPage, _page);
  public void OnMainPage(Page _page) => Interlocked.Exchange(ref p_mainPage, _page);

}
