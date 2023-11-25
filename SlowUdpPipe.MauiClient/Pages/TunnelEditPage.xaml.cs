using JustLogger.Interfaces;
using SlowUdpPipe.MauiClient.Interfaces;
using SlowUdpPipe.MauiClient.Toolkit;
using SlowUdpPipe.MauiClient.ViewModels;

namespace SlowUdpPipe.MauiClient.Pages;

public partial class TunnelEditPage : CContentPage
{
  private readonly ITunnelsConfCtrl p_tunnelsConfCtrl;
  private readonly ILogger p_log;
  private readonly TunnelEditViewModel p_tunnelModel;

  public TunnelEditPage(TunnelEditViewModel _tunnelModel)
  {
    p_tunnelModel = _tunnelModel;
    p_tunnelsConfCtrl = Container.Locate<ITunnelsConfCtrl>();
    p_log = Container.Locate<ILogger>()["tunnel-edit-page"];
    p_log.Info($"Tunnel edit page is opening...");

    BindingContext = _tunnelModel;
    InitializeComponent();
  }

  private async void DeleteBtn_Clicked(object _sender, EventArgs _e)
  {
    p_tunnelsConfCtrl.DeleteTunnelConf(p_tunnelModel.Guid);
    _ = await Navigation.PopAsync();
  }
}