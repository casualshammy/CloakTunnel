﻿using Android.Content;
using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using JustLogger;
using JustLogger.Interfaces;
using SlowUdpPipe.Common.Data;
using SlowUdpPipe.Common.Modules;
using SlowUdpPipe.MauiClient.Data;
using SlowUdpPipe.MauiClient.Interfaces;
using SlowUdpPipe.MauiClient.Modules.TunnelsConfCtrl;
using SlowUdpPipe.MauiClient.Platforms.Android.Services;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SlowUdpPipe.MauiClient.Modules.UdpTunnelCtrl;

[ExportClass(typeof(IUdpTunnelCtrl), Singleton: true, ActivateOnStart: true)]
internal class UdpTunnelCtrlImpl : IUdpTunnelCtrl
{
  private readonly ILogger p_log;
  private readonly Subject<TunnelStatWithName> p_statsSubj = new();

  public UdpTunnelCtrlImpl(
    IReadOnlyLifetime _lifetime,
    ILogger _logger,
    ITunnelsConfCtrl _tunnelsConfCtrl)
  {
    p_log = _logger["udp-tunnel-ctrl"];
    var instanceCounter = -1;

    _tunnelsConfCtrl.TunnelsConf
      .Throttle(TimeSpan.FromSeconds(3))
      .HotAlive(_lifetime, (_confs, _life) =>
      {
        if (_confs == null)
          return;

        p_log.Info($"Tunnels config was changed, building tunnels...");

        var activeTunnels = 0;
        foreach (var conf in _confs)
        {
          if (!conf.Enabled)
          {
            p_log.Info($"Tunnel {conf.Guid} is not enabled");
            continue;
          }

          var remote = conf.RemoteAddress;
          if (remote == null || !IPEndPoint.TryParse(remote, out var remoteEndpoint))
          {
            p_log.Info($"Tunnel {conf.Guid} has incorrect remote endpoint: '{remote}'");
            continue;
          }

          var local = conf.LocalAddress;
          if (local == null || !IPEndPoint.TryParse(local, out var localEndpoint))
          {
            p_log.Info($"Tunnel {conf.Guid} has incorrect local endpoint: '{local}'");
            continue;
          }

          var key = conf.Key;
          if (key.IsNullOrWhiteSpace())
          {
            p_log.Info($"Tunnel {conf.Guid} has empty key");
            continue;
          }

          var encryption = conf.EncryptionAlgo;

          var log = _logger[Interlocked.Increment(ref instanceCounter).ToString()];
          log.Info($"Launching udp tunnel; remote: {remoteEndpoint}, local: {localEndpoint}, cipher: {encryption}");

          var options = new UdpTunnelClientOptions(remoteEndpoint, localEndpoint, encryption, key);
          var tunnel = new UdpTunnelClient(options, _life, log);
          tunnel.Stats.Subscribe(_ => p_statsSubj.OnNext(new TunnelStatWithName(conf.Guid, conf.Name, _.TxBytePerSecond, _.RxBytePerSecond)), _life);
          ++activeTunnels;
        }

        if (activeTunnels > 0)
        {
          p_log.Info($"Total {activeTunnels} active tunnels, launching foreground service...");

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
        }
        else
        {
          p_log.Info($"There are not active tunnels");
        }
      });

    TunnelsStats = p_statsSubj;
  }

  public IObservable<TunnelStatWithName> TunnelsStats { get; }

}
