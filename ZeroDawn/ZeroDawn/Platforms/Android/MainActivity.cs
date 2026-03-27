using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace ZeroDawn;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Window is null)
        {
            return;
        }

        // Keep Android system bars outside the app surface so the UI starts below
        // the status bar and doesn't inherit app theme coloring.
        WindowCompat.SetDecorFitsSystemWindows(Window, true);

        Window.SetStatusBarColor(Android.Graphics.Color.White);
        Window.SetNavigationBarColor(Android.Graphics.Color.White);

        var insetsController = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (insetsController is not null)
        {
            insetsController.AppearanceLightStatusBars = true;
            insetsController.AppearanceLightNavigationBars = true;
        }
    }
}
