namespace SlowUdpPipe.MauiClient.Data;

internal static class AppConsts
{
  public const string PREF_DB_VERSION = "settings.db-version";
  public const string PREF_DB_FIRST_START_INFO_SHOWN = "settings.first-start-info-shown";

  public const string NOTIFICATION_CHANNEL_MAIN = "MainEventsChannel";

  public static  Color COLOR_UP_TUNNEL_OFF { get; } = new Color(0, 204, 102);
  public static  Color COLOR_UP_TUNNEL_ON { get; } = new Color(255, 128, 0);
  public static Color COLOR_DELETE_TUNNEL { get; } = new Color(204, 0, 0);
}
