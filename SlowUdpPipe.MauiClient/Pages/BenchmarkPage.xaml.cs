using Ax.Fw.SharedTypes.Interfaces;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.Common.Toolkit;
using SlowUdpPipe.MauiClient.Toolkit;
using System.Reactive.Linq;

namespace SlowUdpPipe.MauiClient.Pages;

public partial class BenchmarkPage : CContentPage
{
  private readonly ILogger p_log;
  private readonly IReadOnlyLifetime p_lifetime;

  public BenchmarkPage()
  {
    p_log = Container.Locate<ILogger>()["benchmark-page"];
    p_log.Info($"Main page is opening...");

    p_lifetime = Container.Locate<IReadOnlyLifetime>();

    InitializeComponent();
  }

  private async void Benchmark_Clicked(object _sender, EventArgs _e)
  {
    using var lifetime = p_lifetime.GetChildLifetime();
    if (lifetime == null)
      return;

    var animation = new Animation
    {
      { 0, 0.5, new Animation(_rotation => p_roundImageButton.Scale = _rotation, 1d, 0.75d, Easing.SinInOut) },
      { 0.5, 1,  new Animation(_rotation => p_roundImageButton.Scale = _rotation, 0.75d, 1d, Easing.SinInOut) }
    };

    var btn = _sender as Button;
    if (btn != null)
    {
      btn.IsEnabled = false;
      btn.Background = Data.AppConsts.COLOR_UP_TUNNEL_ON;
    }

    try
    {
      void updateProgress(double _progress)
      {
        MainThread.BeginInvokeOnMainThread(() => p_progressLabel.Text = $"Benchmarking ({_progress * 100:F0}%)");
      }

      animation.Commit(p_progressLabel, "benchmark-image-anim", 16, 2000, null, (_, __) => p_roundImageButton.Scale = 1d, () => true);
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