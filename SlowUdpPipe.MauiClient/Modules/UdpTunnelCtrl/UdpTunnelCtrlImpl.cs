using Android.Content;
using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Java.Lang;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.Common.Modules;
using SlowUdpPipe.MauiClient.Interfaces;
using SlowUdpPipe.MauiClient.Platforms.Android.Services;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static SlowUdpPipe.MauiClient.Data.Consts;

namespace SlowUdpPipe.MauiClient.Modules.UdpTunnelCtrl;

[ExportClass(typeof(IUdpTunnelCtrl), Singleton: true, ActivateOnStart: true)]
internal class UdpTunnelCtrlImpl : IUdpTunnelCtrl
{
  private readonly Subject<UdpTunnelStat> p_statsSubj = new();
  private readonly ReplaySubject<bool> p_stateSubj = new(1);

  public UdpTunnelCtrlImpl(
    IPreferencesStorage _prefStorage,
    IReadOnlyLifetime _lifetime,
    ILogger _logger)
  {
    var instanceCounter = -1;
    _prefStorage.PreferencesChanged
      .Sample(TimeSpan.FromSeconds(3))
      .CombineLatest(p_stateSubj)
      .HotAlive(_lifetime, (_tuple, _life) =>
      {
        var (_, enable) = _tuple;
        if (!enable)
          return;

        var remote = _prefStorage.GetValueOrDefault<string>(PREF_DB_REMOTE);
        if (remote == null || !IPEndPoint.TryParse(remote, out var remoteEndpoint))
          return;

        var local = _prefStorage.GetValueOrDefault<string>(PREF_DB_LOCAL);
        if (local == null || !IPEndPoint.TryParse(local, out var localEndpoint))
          return;

        var key = _prefStorage.GetValueOrDefault<string>(PREF_DB_KEY);
        if (key.IsNullOrWhiteSpace())
          return;

        var cipher = _prefStorage.GetValueOrDefault<EncryptionAlgorithm>(PREF_DB_CIPHER);

        var log = _logger[Interlocked.Increment(ref instanceCounter).ToString()];
        log.Info($"Launching udp tunnel; remote: {remoteEndpoint}, local: {localEndpoint}, cipher: {cipher}");

        var options = new UdpTunnelClientOptions(remoteEndpoint, localEndpoint, cipher, key);
        var tunnel = new UdpTunnelClient(options, _life, log);
        tunnel.Stats.Subscribe(_ => p_statsSubj.OnNext(_), _life);

        _life.DoOnEnding(() =>
        {
          var context = global::Android.App.Application.Context;
          var intent = new Intent(context, typeof(UdpTunnelService));
          intent.SetAction("STOP_SERVICE");
          context.StartService(intent);
        });

        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(UdpTunnelService));
        intent.SetAction("START_SERVICE");
        context.StartForegroundService(intent);
      });

    p_stateSubj.OnNext(_prefStorage.GetValueOrDefault<bool>(PREF_DB_UP_TUNNEL_ON_APP_STARTUP));

    TunnelStats = p_statsSubj;
    State = p_stateSubj;
  }

  public IObservable<UdpTunnelStat> TunnelStats { get; }
  public IObservable<bool> State { get; }

  public void SetState(bool _state) => p_stateSubj.OnNext(_state);

  public async Task<bool> GetStateAsync() => await p_stateSubj.FirstAsync();

}
