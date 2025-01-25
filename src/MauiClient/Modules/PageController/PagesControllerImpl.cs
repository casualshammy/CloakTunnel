using Ax.Fw.DependencyInjection;
using CloakTunnel.MauiClient.Interfaces;

namespace CloakTunnel.MauiClient.Modules.PageController;

internal class PagesControllerImpl : IPagesController, IAppModule<IPagesController>
{
  public static IPagesController ExportInstance(IAppDependencyCtx _ctx)
  {
    return new PagesControllerImpl();
  }

  private volatile Page? p_currentPage;
  private volatile Page? p_mainPage;

  public PagesControllerImpl() { }

  public Page? CurrentPage => p_currentPage;
  public Page? MainPage => p_mainPage;

  public void OnPageActivated(Page _page) => Interlocked.Exchange(ref p_currentPage, _page);
  public void OnMainPage(Page _page) => Interlocked.Exchange(ref p_mainPage, _page);

}
