using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.MauiClient.Interfaces;
using CloakTunnel.MauiClient.Toolkit;
using CloakTunnel.MauiClient.ViewModels;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static CloakTunnel.MauiClient.Data.AppConsts;

namespace CloakTunnel.MauiClient.Pages;

public partial class TunnelsListPage : CContentPage
{
  private readonly IReadOnlyLifetime p_lifetime;
  private readonly ITunnelsConfCtrl p_tunnelsConfCtrl;
  private readonly Subject<bool> p_appearingSubj = new();
  private readonly TunnelsListViewModel p_bindingCtx;

  public TunnelsListPage()
  {
    InitializeComponent();
    p_bindingCtx = (TunnelsListViewModel)BindingContext;

    p_lifetime = Container.Locate<IReadOnlyLifetime>();
    var prefStorage = Container.Locate<IPreferencesStorage>();
    p_tunnelsConfCtrl = Container.Locate<ITunnelsConfCtrl>();

    var pagesCtrl = Container.Locate<IPagesController>();
    pagesCtrl.OnMainPage(this);

    p_appearingSubj
      .Take(1)
      .Delay(TimeSpan.FromSeconds(3))
      .SelectAsync(async (_, _ct) =>
      {
        var infoShown = prefStorage.GetValueOrDefault<bool>(PREF_DB_FIRST_START_INFO_SHOWN);
        if (!infoShown)
        {
          prefStorage.SetValue(PREF_DB_FIRST_START_INFO_SHOWN, true);
          await MainThread.InvokeOnMainThreadAsync(async () =>
          {
            var goToWebsite = await DisplayAlert(
                $"Important notice",
                $"CloakTunnel is NOT a VPN or proxy app! You *must* setup your own server or get credentials from someone who made it for you!" +
                $"\nPress button below to go to repository website where you could find instructions how to setup the server",
                "Go to website",
                "Understood");

            if (goToWebsite)
              await Launcher.Default.OpenAsync("https://github.com/casualshammy/slow-udp-pipe");
          });
        }
      })
      .Subscribe(p_lifetime);
  }

  protected override void OnAppearing()
  {
    base.OnAppearing();
    p_appearingSubj.OnNext(true);
  }

  private async void AddTunnel_Clicked(object _sender, EventArgs _e)
  {
    var conf = p_tunnelsConfCtrl.CreateTunnelConf();
    var model = await p_bindingCtx.TryGetExistingTunnelModelAsync(conf.Guid, p_lifetime.Token);
    if (model != null)
      await Navigation.PushAsync(new TunnelEditPage(model));
  }

  private async void OnItemSelected(object _sender, SelectionChangedEventArgs _e)
  {
    if (_e.CurrentSelection.FirstOrDefault() is not TunnelEditViewModel item)
      return;

    try
    {
      await Navigation.PushAsync(new TunnelEditPage(item));
    }
    finally
    {
      p_listView.SelectedItem = null;
    }
  }
}