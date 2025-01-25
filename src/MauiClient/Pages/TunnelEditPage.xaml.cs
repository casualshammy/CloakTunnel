using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.MauiClient.Interfaces;
using CloakTunnel.MauiClient.Toolkit;
using CloakTunnel.MauiClient.ViewModels;

namespace CloakTunnel.MauiClient.Pages;

public partial class TunnelEditPage : CContentPage
{
  private readonly ITunnelsConfCtrl p_tunnelsConfCtrl;
  private readonly ILog p_log;
  private readonly TunnelEditViewModel p_tunnelModel;

  public TunnelEditPage(TunnelEditViewModel _tunnelModel)
  {
    p_tunnelModel = _tunnelModel;
    p_tunnelsConfCtrl = Container.Locate<ITunnelsConfCtrl>();
    p_log = Container.Locate<ILog>()["tunnel-edit-page"];
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