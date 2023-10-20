using Ax.Fw.Attributes;
using SlowUdpPipe.MauiClient.Interfaces;

namespace SlowUdpPipe.MauiClient.Modules.PageController;

[ExportClass(typeof(IPagesController), Singleton: true)]
internal class PagesControllerImpl : IPagesController
{
  private volatile Page? p_currentPage;
  private volatile Page? p_mainPage;

  public PagesControllerImpl() { }

  public Page? CurrentPage => p_currentPage;
  public Page? MainPage => p_mainPage;

  public void OnPageActivated(Page _page) => Interlocked.Exchange(ref p_currentPage, _page);
  public void OnMainPage(Page _page) => Interlocked.Exchange(ref p_mainPage, _page);

}
