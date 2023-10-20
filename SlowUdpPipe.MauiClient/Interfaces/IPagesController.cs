namespace SlowUdpPipe.MauiClient.Interfaces;

public interface IPagesController
{
  Page? CurrentPage { get; }
  Page? MainPage { get; }

  void OnMainPage(Page _page);
  void OnPageActivated(Page _page);
}
