namespace SlowUdpPipe.MauiClient.Data;

internal static class Consts
{
  public const string PREF_DB_VERSION = "settings.db-version";
  public const string PREF_DB_LOCAL = "settings.local";
  public const string PREF_DB_REMOTE = "settings.remote";
  public const string PREF_DB_CIPHER = "settings.cipher";
  public const string PREF_DB_KEY = "settings.key";
  public const string PREF_DB_UP_TUNNEL_ON_APP_STARTUP = "settings.up-tunnel-on-app-startup";
  public const string PREF_DB_FIRST_START_INFO_SHOWN = "settings.first-start-info-shown";

  public const string NOTIFICATION_CHANNEL_MAIN = "MainEventsChannel";

  public static  Color COLOR_UP_TUNNEL_OFF { get; } = new Color(0, 204, 102);
  public static  Color COLOR_UP_TUNNEL_ON { get; } = new Color(255, 128, 0);

}
