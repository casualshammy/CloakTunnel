using Android.Content;
using Android.OS;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.Common.Toolkit;
using SlowUdpPipe.MauiClient.Interfaces;
using SlowUdpPipe.MauiClient.Toolkit;
using SlowUdpPipe.MauiClient.ViewModels;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static SlowUdpPipe.MauiClient.Data.Consts;

namespace SlowUdpPipe.MauiClient.Pages;

public partial class MainPage : CContentPage
{
  private readonly ILogger p_log;
  private readonly IPreferencesStorage p_prefStorage;
  private readonly IReadOnlyLifetime p_lifetime;
  private readonly IUdpTunnelCtrl p_udpTunnelCtrl;
  private readonly Subject<bool> p_appearingSubj = new();
  private readonly MainPageViewModel p_bindingCtx;

  public MainPage()
  {
    p_log = Container.Locate<ILogger>()["main-page"];
    p_log.Info($"Main page is opening...");

    InitializeComponent();
    p_bindingCtx = (MainPageViewModel)BindingContext;

    var pageController = Container.Locate<IPagesController>();
    pageController.OnMainPage(this);

    p_prefStorage = Container.Locate<IPreferencesStorage>();
    p_lifetime = Container.Locate<IReadOnlyLifetime>();
    p_udpTunnelCtrl = Container.Locate<IUdpTunnelCtrl>();

    p_upTunnelOnStart.SwitchIsToggled = p_prefStorage.GetValueOrDefault<bool>(PREF_DB_UP_TUNNEL_ON_APP_STARTUP);

    p_appearingSubj
      .Take(1)
      .Delay(TimeSpan.FromSeconds(3))
      .SelectAsync(async (_, _ct) =>
      {
        var infoShown = p_prefStorage.GetValueOrDefault<bool>(PREF_DB_FIRST_START_INFO_SHOWN);
        if (!infoShown)
        {
          p_prefStorage.SetValue(PREF_DB_FIRST_START_INFO_SHOWN, true);
          await MainThread.InvokeOnMainThreadAsync(async () =>
          {
            var goToWebsite = await DisplayAlert(
                $"Important notice",
                $"SlowUdpPipe is NOT a VPN or proxy app! You *must* setup your own server or get credentials from someone who made it for you!" +
                $"\nPress button below to go to repository website where you could find instructions how to setup the server",
                "Go to website",
                "Understood");

            if (goToWebsite)
              await Launcher.Default.OpenAsync("https://github.com/casualshammy/slow-udp-pipe");
          });
        }
      })
      .Subscribe(p_lifetime);

    p_udpTunnelCtrl.State
      .Subscribe(_tunnelState =>
      {
        p_bindingCtx.StartStopBtnText = _tunnelState ? "Stop tunnel" : "Start tunnel";
        p_bindingCtx.StartStopBtnColor = _tunnelState ? COLOR_UP_TUNNEL_ON : COLOR_UP_TUNNEL_OFF;
      }, p_lifetime);
  }

  protected override void OnAppearing()
  {
    base.OnAppearing();
    p_appearingSubj.OnNext(true);
  }

  private async void StartStop_Clicked(object _sender, EventArgs _e)
  {
    var active = await p_udpTunnelCtrl.GetStateAsync();
    if (!active)
    {
      var context = global::Android.App.Application.Context;
      var packageName = context.PackageName;
      var powerMgr = context.GetSystemService(Context.PowerService) as PowerManager;
      var ignoring = powerMgr?.IsIgnoringBatteryOptimizations(packageName);
      if (ignoring != true)
      {
        var result = await DisplayAlert(
          $"Battery optimization is enabled",
          $"SlowUdpPipe needs to keep an open port so that WireGuard can connect to it. " +
          $"This doesn't affect the battery life of the device, as SlowUdpPipe consumes very little energy when it is handling low background traffic." +
          $"\nUnfortunately, Android restricts the activity of background applications in order to save energy. " +
          $"To ensure the proper functioning of SlowUdpPipe, it is necessary to disable optimization. " +
          $"\nTo do so, go to SlowUdpPipe's settings -> search for battery related settings -> enable 'Unrestricted' mode " +
          $"(it may be also called 'Allow background activity')",
          "Disable optimization",
          "Cancel");

        if (result)
        {
          var intent = new Intent();
          intent.SetFlags(ActivityFlags.NewTask);
          intent.SetAction(Android.Provider.Settings.ActionApplicationDetailsSettings);
          var uri = global::Android.Net.Uri.Parse($"package:{packageName}");
          intent.SetData(uri);
          intent.AddCategory(Intent.CategoryDefault);
          context.StartActivity(intent);
        }
      }
    }

    p_udpTunnelCtrl.SetState(!active);
  }

  private async void Benchmark_Clicked(object _sender, EventArgs _e)
  {
    using var lifetime = p_lifetime.GetChildLifetime();
    if (lifetime == null)
      return;

    var btn = _sender as Button;
    var btnOriginalText = btn?.Text;
    if (btn != null)
      btn.IsEnabled = false;

    try
    {
      void updateProgress(double _progress)
      {
        if (btn == null)
          return;

        MainThread.BeginInvokeOnMainThread(() => btn.Text = $"Benchmarking ({_progress * 100:F0}%)");
      }

      var results = await Task.Run(() => EncryptionAlgorithmsTest.Benchmark(lifetime, updateProgress).ToArray());
      var message = "More is better\n\n";
      foreach (var result in results)
      {
        var algoSlug = Consts.ENCRYPTION_ALG_SLUG[result.Algorithm];
        if (result.ResultMs == null)
          message += $"Cipher '{algoSlug}' is not supported on this platform\n";
        else
          message += $"{algoSlug}: {Converters.BytesPerSecondToString(result.WorkVolumeBytes / (result.ResultMs.Value / 1000d))}\n";
      }

      await DisplayAlert("Benchmark results", message, "Close");
    }
    finally
    {
      if (btn != null && btnOriginalText != null)
        btn.Text = btnOriginalText;
      if (btn != null)
        btn.IsEnabled = true;
    }
  }

}