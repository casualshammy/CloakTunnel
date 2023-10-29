using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using AndroidX.Core.App;
using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Grace.DependencyInjection.Attributes;
using SlowUdpPipe.Common.Toolkit;
using SlowUdpPipe.MauiClient.Interfaces;
using SlowUdpPipe.MauiClient.Toolkit;
using System.Reactive.Linq;

namespace SlowUdpPipe.MauiClient.Platforms.Android.Services;

[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
[ExportClass(typeof(IUdpTunnelService), Singleton: true)]
public class UdpTunnelService : CAndroidService, IUdpTunnelService
{
  private const string NOTIFICATION_CHANNEL = "ServiceChannel";
  private const int NOTIFICATION_ID = 100;
  private const int REQUEST_POST_NOTIFICATIONS = 1000;
  private readonly NotificationManager p_notificationManager;
  private ILifetime? p_lifetime;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
  public UdpTunnelService()
  {
    var context = global::Android.App.Application.Context;
    p_notificationManager = (NotificationManager)context.GetSystemService(NotificationService)!;
  }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  [Import]
  public IReadOnlyLifetime Lifetime { get; init; }

  [Import]
  public IPreferencesStorage PrefStorage { get; init; }

  [Import]
  public IUdpTunnelCtrl UdpTunnelCtrl { get; init; }

  public override IBinder OnBind(Intent? _intent) => throw new NotImplementedException();

  [return: GeneratedEnum]
  public override StartCommandResult OnStartCommand(
    Intent? _intent,
    [GeneratedEnum] StartCommandFlags _flags,
    int _startId)
  {
    if (_intent?.Action == "START_SERVICE")
    {
      p_lifetime = Lifetime.GetChildLifetime();
      if (p_lifetime == null)
        return StartCommandResult.NotSticky;

      var notification = GetNotification("Udp tunnel is up", "No data transmitted yet", true, p_lifetime.Token);

      if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
#pragma warning disable CA1416 // Validate platform compatibility
        StartForeground(NOTIFICATION_ID, notification, global::Android.Content.PM.ForegroundService.TypeDataSync);
#pragma warning restore CA1416 // Validate platform compatibility
      else
        StartForeground(NOTIFICATION_ID, notification);

      UdpTunnelCtrl.TunnelStats
        .Buffer(TimeSpan.FromSeconds(3))
        .Subscribe(_list =>
        {
          if (!_list.Any())
            return;

          var avgRx = _list.Average(_ => (long)_.RxBytePerSecond);
          var avgTx = _list.Average(_ => (long)_.TxBytePerSecond);

          var text = $"Rx: {Converters.BytesPerSecondToString(avgRx)}; Tx: {Converters.BytesPerSecondToString(avgTx)}";
          GetNotification($"Udp tunnel is up", text, false, p_lifetime.Token);
        }, p_lifetime);

      p_lifetime.DoOnEnding(() =>
      {
        StopForeground(StopForegroundFlags.Remove);
        StopSelfResult(_startId);
      });

      return StartCommandResult.NotSticky;
    }
    else if (_intent?.Action == "STOP_SERVICE")
    {
      p_lifetime?.Dispose();
    }
    else if (_intent?.Action == "STOP_SERVICE_INFORM_TUNNEL_CTRL")
    {
      UdpTunnelCtrl.SetState(false);
    }

    return StartCommandResult.NotSticky;
  }

  public void Start()
  {
    var context = global::Android.App.Application.Context;
    var intent = new Intent(context, typeof(UdpTunnelService));
    intent.SetAction("START_SERVICE");
    context.StartForegroundService(intent);
  }

  public void Stop()
  {
    var context = global::Android.App.Application.Context;
    var intent = new Intent(context, Class);
    intent.SetAction("STOP_SERVICE");
    context.StartService(intent);
  }

  private Notification GetNotification(string _title, string _text, bool _firstShow, CancellationToken _ct)
  {
    if (_firstShow && Build.VERSION.SdkInt > BuildVersionCodes.SV2 && Platform.CurrentActivity != null)
      ActivityCompat.RequestPermissions(Platform.CurrentActivity, new[] { "android.permission.POST_NOTIFICATIONS" }, REQUEST_POST_NOTIFICATIONS);

    if (_firstShow)
    {
      var channel = new NotificationChannel(NOTIFICATION_CHANNEL, "Notify when tunnel is active", NotificationImportance.Min);
      channel.SetShowBadge(false);
      p_notificationManager.CreateNotificationChannel(channel);
    }

    var context = global::Android.App.Application.Context;
    var openAppIntent = PendingIntent.GetActivity(context, 0, Platform.CurrentActivity?.Intent, PendingIntentFlags.Immutable);

    var stopIntent = new Intent(context, Class);
    stopIntent.SetAction("STOP_SERVICE_INFORM_TUNNEL_CTRL");
    var stopServiceIntent = PendingIntent.GetService(context, 0, stopIntent, PendingIntentFlags.Immutable);
    var actions = new[] { new Notification.Action(Resource.Drawable.infinity, "Stop", stopServiceIntent) };

    var layoutSmall = new RemoteViews(context.PackageName, Resource.Layout.notification_small);
    layoutSmall.SetTextViewText(Resource.Id.notification_small_title, _title);

    var layoutLarge = new RemoteViews(context.PackageName, Resource.Layout.notification_large);
    layoutLarge.SetTextViewText(Resource.Id.notification_large_title, _title);
    layoutLarge.SetTextViewText(Resource.Id.notification_large_body, _text);

    var builder = new Notification.Builder(this, NOTIFICATION_CHANNEL)
     .SetContentIntent(openAppIntent)
     .SetSmallIcon(Resource.Drawable.infinity)
     .SetOnlyAlertOnce(true)
     .SetOngoing(true)
     .SetActions(actions)
     .SetStyle(new Notification.DecoratedCustomViewStyle())
     .SetCustomContentView(layoutSmall)
     .SetCustomBigContentView(layoutLarge)
     .SetContentTitle(_title);

#pragma warning disable CA1416 // Validate platform compatibility
    if (_firstShow && Build.VERSION.SdkInt >= BuildVersionCodes.S)
      builder = builder.SetForegroundServiceBehavior(1); // FOREGROUND_SERVICE_IMMEDIATE
#pragma warning restore CA1416 // Validate platform compatibility

    var notification = builder.Build();

    if (!_ct.IsCancellationRequested)
      p_notificationManager.Notify(NOTIFICATION_ID, notification);

    return notification;
  }

}
