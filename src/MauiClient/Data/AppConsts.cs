namespace CloakTunnel.MauiClient.Data;

internal static class AppConsts
{
  public const string LOG_TAG = nameof(CloakTunnel);

  public const string PREF_DB_VERSION = "settings.db-version";
  public const string PREF_DB_FIRST_START_INFO_SHOWN = "settings.first-start-info-shown";

  public const string NOTIFICATION_CHANNEL_GENERAL = "GeneralChannel";
  public const string NOTIFICATION_CHANNEL_SERVICE = "ServiceChannel";
  public const string NOTIFICATION_CHANNEL_TUNNEL_BIND_ERROR = "TunnelBindError";

  public const int NOTIFICATION_ID_SERVICE = 1000;
  public const int NOTIFICATION_ID_TUNNEL_BIND_ERROR = 2000;
  public const int NOTIFICATION_ID_BATTERY_OPTIMIZATION = 3000;

  public const int REQUEST_POST_NOTIFICATIONS_CODE = 1000;

  public static  Color COLOR_UP_TUNNEL_OFF { get; } = new Color(0, 204, 102);
  public static  Color COLOR_UP_TUNNEL_ON { get; } = new Color(255, 128, 0);
  public static Color COLOR_DELETE_TUNNEL { get; } = new Color(204, 0, 0);
}
