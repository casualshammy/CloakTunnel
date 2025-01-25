using Android.App;
using Android.Content;
using Android.Content.PM;

namespace CloakTunnel.MauiClient
{
  [Activity(
    Theme = "@style/Maui.SplashTheme", 
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleInstance,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
  public class MainActivity : MauiAppCompatActivity
  {
    protected override void OnNewIntent(Intent? _intent)
    {
      base.OnNewIntent(_intent);
    }
  }
}