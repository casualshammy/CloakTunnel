using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
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
  private ILifetime? p_lifetime;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

  [Import]
  public IReadOnlyLifetime Lifetime { get; init; }

  [Import]
  public IPreferencesStorage PrefStorage { get; init; }

  [Import]
  public IUdpTunnelCtrl UdpTunnelCtrl { get; init; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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

      var notification = CreateNotification("Udp tunnel is up", "No data transmitted yet");
      if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
#pragma warning disable CA1416 // Validate platform compatibility
        StartForeground(NOTIFICATION_ID, notification, global::Android.Content.PM.ForegroundService.TypeDataSync);
#pragma warning restore CA1416 // Validate platform compatibility
      else
        StartForeground(NOTIFICATION_ID, notification);

      UdpTunnelCtrl.TunnelStats
        .DistinctUntilChanged()
        .Sample(TimeSpan.FromSeconds(3))
        .Subscribe(_s =>
        {
          var text = $"Rx: {Converters.BytesToString(_s.RxBytePerSecond)}; Tx: {Converters.BytesToString(_s.TxBytePerSecond)}";
          UpdateNotificationText($"Udp tunnel is up", text, p_lifetime.Token);
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

  private Notification CreateNotification(string _title, string _text)
  {
    var context = global::Android.App.Application.Context;
    var manager = (NotificationManager)context.GetSystemService(NotificationService)!;
    var openAppIntent = PendingIntent.GetActivity(context, 0, Platform.CurrentActivity?.Intent, PendingIntentFlags.Immutable);

    var stopIntent = new Intent(context, Class);
    stopIntent.SetAction("STOP_SERVICE_INFORM_TUNNEL_CTRL");
    var stopServiceIntent = PendingIntent.GetService(context, 0, stopIntent, PendingIntentFlags.Immutable);
    var stopServiceAction = new[] { new Notification.Action(Resource.Drawable.infinity, "Stop", stopServiceIntent) };

    if (Build.VERSION.SdkInt > BuildVersionCodes.SV2 && Platform.CurrentActivity != null)
      if (ActivityCompat.ShouldShowRequestPermissionRationale(Platform.CurrentActivity, "android.permission.POST_NOTIFICATIONS"))
        ActivityCompat.RequestPermissions(Platform.CurrentActivity, new[] { "android.permission.POST_NOTIFICATIONS" }, REQUEST_POST_NOTIFICATIONS);

    var channel = new NotificationChannel(NOTIFICATION_CHANNEL, "Notify when tunnel is active", NotificationImportance.Low);
    manager.CreateNotificationChannel(channel);

    var builder = new Notification.Builder(this, NOTIFICATION_CHANNEL)
     .SetContentTitle(_title)
     .SetContentText(_text)
     .SetContentIntent(openAppIntent)
     .SetSmallIcon(Resource.Drawable.infinity)
     .SetOnlyAlertOnce(true)
     .SetOngoing(true)
     .SetActions(stopServiceAction);

    if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
#pragma warning disable CA1416 // Validate platform compatibility
      builder = builder.SetForegroundServiceBehavior(1); // FOREGROUND_SERVICE_IMMEDIATE
#pragma warning restore CA1416 // Validate platform compatibility

    var notification = builder.Build();
    manager.Notify(NOTIFICATION_ID, notification);

    return notification;
  }

  private void UpdateNotificationText(string _title, string _text, CancellationToken _ct)
  {
    var context = global::Android.App.Application.Context;
    if (context.GetSystemService(NotificationService) is not NotificationManager manager)
    {
      Log.Error($"Cannot get the instance of {nameof(NotificationManager)}");
      return;
    }

    var openAppIntent = PendingIntent.GetActivity(context, 0, Platform.CurrentActivity?.Intent, PendingIntentFlags.Immutable);

    var stopIntent = new Intent(context, Class);
    stopIntent.SetAction("STOP_SERVICE_INFORM_TUNNEL_CTRL");
    var stopServiceIntent = PendingIntent.GetService(context, 0, stopIntent, PendingIntentFlags.Immutable);
    var stopServiceAction = new[] { new Notification.Action(Resource.Drawable.infinity, "Stop", stopServiceIntent) };

    var builder = new Notification.Builder(this, NOTIFICATION_CHANNEL)
     .SetContentTitle(_title)
     .SetContentText(_text)
     .SetContentIntent(openAppIntent)
     .SetSmallIcon(Resource.Drawable.infinity)
     .SetOnlyAlertOnce(true)
     .SetOngoing(true)
     .SetActions(stopServiceAction);

    var notification = builder.Build();

    if (!_ct.IsCancellationRequested)
      manager.Notify(NOTIFICATION_ID, notification);
  }

}
