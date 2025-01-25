using Android.Content;
using Ax.Fw.DependencyInjection;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using CloakTunnel.Common.Clients;
using CloakTunnel.Common.Data;
using CloakTunnel.Common.Interfaces;
using CloakTunnel.MauiClient.Data;
using CloakTunnel.MauiClient.Interfaces;
using CloakTunnel.MauiClient.Platforms.Android.Services;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CloakTunnel.MauiClient.Modules.TunnelsController;

internal class TunnelsControllerImpl : IUdpTunnelCtrl, IAppModule<IUdpTunnelCtrl>
{
  public static IUdpTunnelCtrl ExportInstance(IAppDependencyCtx _ctx)
  {
    return _ctx.CreateInstance((
      IReadOnlyLifetime _lifetime,
      ILog _logger,
      ITunnelsConfCtrl _tunnelsConfCtrl) => new TunnelsControllerImpl(_lifetime, _logger["tunnel-ctrl"], _tunnelsConfCtrl));
  }

  private readonly ILog p_log;
  private readonly Subject<TunnelStatWithName> p_statsSubj = new();

  public TunnelsControllerImpl(
    IReadOnlyLifetime _lifetime,
    ILog _logger,
    ITunnelsConfCtrl _tunnelsConfCtrl)
  {
    p_log = _logger;
    var instanceCounter = -1;

    var scheduler = new EventLoopScheduler();

    _tunnelsConfCtrl.TunnelsConf
      .Throttle(TimeSpan.FromSeconds(3))
      .HotAlive(_lifetime, scheduler, (_confs, _life) =>
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
          if (remote == null || !Uri.TryCreate(remote, UriKind.Absolute, out var remoteUri))
          {
            p_log.Warn($"Tunnel {conf.Guid} has incorrect remote uri: '{remote}'");
            continue;
          }

          var local = conf.LocalAddress;
          if (local == null || !Uri.TryCreate(local, UriKind.Absolute, out var localUri))
          {
            p_log.Warn($"Tunnel {conf.Guid} has incorrect local endpoint: '{local}'");
            continue;
          }

          var key = conf.Key;
          if (key.IsNullOrWhiteSpace())
          {
            p_log.Warn($"Tunnel {conf.Guid} has empty key");
            continue;
          }

          var tunnelType = remoteUri.Scheme.StartsWith("ws") || remoteUri.Scheme.StartsWith("wss") 
            ? EndpointType.Websocket
            : EndpointType.Udp;

          if (tunnelType == EndpointType.Websocket && remoteUri.AbsolutePath.Length < 3)
          {
            p_log.Warn($"Websocket path (section after last '/') must be at least 2 characters long");
            continue;
          }

          var encryption = conf.EncryptionAlgo;

          var log = _logger[Interlocked.Increment(ref instanceCounter).ToString()];
          log.Info($"Launching {tunnelType} tunnel; remote: {remoteUri}, local: {localUri}, cipher: {encryption}");

          var options = new UdpTunnelClientOptions(tunnelType, localUri, remoteUri, encryption, key);

          ITunnelClient tunnel;
          if (tunnelType == EndpointType.Udp)
            tunnel = new UdpTunnelClient(options, _life, log);
          else
            tunnel = new WsTunnelClient(options, _life, log);

          tunnel.Stats.Subscribe(_ => p_statsSubj.OnNext(new TunnelStatWithName(conf.Guid, conf.Name, _.TxBytePerSecond, _.RxBytePerSecond)), _life);

          // handle cases then there is not received traffic
          tunnel.Stats
            .Buffer(TimeSpan.FromSeconds(30))
            .Subscribe(_list =>
            {
              if (_list.Count == 0)
                return;

              var totalRx = _list.Sum(_ => (float)_.RxBytePerSecond);
              var totalTx = _list.Sum(_ => (float)_.TxBytePerSecond);

              if (totalTx > 0 && totalRx == 0)
              {
                log.Warn($"Looks like incoming stream is stuck, resetting tunnel...");
                tunnel.DropAllClients();
                log.Warn($"Tunnel is reset");
              }
            }, _life);

          ++activeTunnels;
        }

        if (activeTunnels > 0)
        {
          p_log.Info($"Total {activeTunnels} active tunnels, launching foreground service...");

          _life.DoOnEnding(() =>
          {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(UdpTunnelService));
            intent.SetAction("STOP_SERVICE");
            context.StartService(intent);
            p_log.Info($"**Foreground service** is __stopped__");
          });

          var context = Android.App.Application.Context;
          var intent = new Intent(context, typeof(UdpTunnelService));
          intent.SetAction("START_SERVICE");
          context.StartForegroundService(intent);
          p_log.Info($"**Foreground service** is __started__");
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
