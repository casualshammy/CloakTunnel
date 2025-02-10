using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Toolkit;
using CloakTunnel.MauiClient.Toolkit;
using SkiaSharp;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CloakTunnel.MauiClient.Pages;

public partial class BenchmarkPage : CContentPage
{
  private readonly ILog p_log;
  private readonly IReadOnlyLifetime p_lifetime;
  private ImmutableDictionary<string, float?> p_results = ImmutableDictionary<string, float?>.Empty;

  public BenchmarkPage()
  {
    p_log = Container.Locate<ILog>()["benchmark-page"];
    p_log.Info($"Page is loading...");

    p_lifetime = Container.Locate<IReadOnlyLifetime>();

    InitializeComponent();
    BindingContext = this;

    p_mainGrid.RowDefinitions[0].Height = new GridLength(0);
    p_mainGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
    p_mainGrid.RowDefinitions[2].Height = new GridLength(100, GridUnitType.Absolute);

    p_chart.SetOptions(new Controls.VerticalBarChartOptions(SKColors.LightGray, 40, "Performance (more is better)", _ => _.Value));

    p_log.Info($"Page is loaded");
  }

  public bool DataReady => p_results.Count > 0;

  private async void Benchmark_Clicked(object _sender, EventArgs _e)
  {
    using var lifetime = p_lifetime.GetChildLifetime();
    if (lifetime == null)
      return;

    var btn = _sender as Button;
    if (btn != null)
    {
      btn.IsEnabled = false;
      btn.Background = Data.AppConsts.COLOR_UP_TUNNEL_ON;
    }

    var animation = new Animation
    {
      { 0, 0.5, new Animation(_animValue => p_benchmarkBtn.Scale = _animValue, 1d, 0.9d, Easing.SinInOut) },
      { 0.5, 1, new Animation(_animValue => p_benchmarkBtn.Scale = _animValue, 0.9d, 1d, Easing.SinInOut) }
    };

    try
    {
      void updateProgress(double _progress) 
        => MainThread.BeginInvokeOnMainThread(() => p_progressLabel.Text = $"Benchmarking ({_progress * 100:F0}%)");

      animation.Commit(
        p_progressLabel, 
        "benchmark-image-anim", 
        16, 
        2000, 
        null, 
        (_, __) => p_benchmarkBtn.ScaleTo(1d, 250), 
        () => true);

      await Task.Run(() =>
      {
        foreach (var result in EncryptionAlgorithmsTest.Benchmark(lifetime, updateProgress))
        {
          var algoSlug = EncryptionToolkit.ENCRYPTION_ALG_SLUG[result.Algorithm];
          if (result.ResultMs == null)
            p_results = p_results.SetItem(algoSlug, null);
          else
            p_results = p_results.SetItem(algoSlug, (result.WorkVolumeBytes / (result.ResultMs.Value / 1000f)));

          _ = MainThread.InvokeOnMainThreadAsync(() =>
          {
            p_benchmarkBtn.WidthRequest = 100;
            p_benchmarkBtn.HeightRequest = 100;
            p_mainGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            p_mainGrid.RowDefinitions[1].Height = new GridLength(110, GridUnitType.Absolute);
            p_mainGrid.RowDefinitions[2].Height = new GridLength(50, GridUnitType.Absolute);

            OnPropertyChanged(nameof(DataReady));
            p_chart.SetEntries(p_results.Select(_ =>
            {
              var hexBarColor = _.Key.ExpressAsRgba();
              var barColor = SKColor.Parse(hexBarColor);
              var barColorBrightness = hexBarColor.GetYiqBrightnessFromHexColor();
              return new Controls.VerticalBarChartEntry(
                _.Key,
                _.Value ?? 0,
                null,
                barColor,
                barColorBrightness > 128 ? SKColors.Black : SKColors.White);
            }));
          });
        }
      });
    }
    finally
    {
      animation.Dispose();
      p_progressLabel.Text = string.Empty;
      if (btn != null)
      {
        btn.IsEnabled = true;
        btn.Background = Data.AppConsts.COLOR_UP_TUNNEL_OFF;
      }
    }
  }

}