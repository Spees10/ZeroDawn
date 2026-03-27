namespace ZeroDawn;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        ApplyPlatformInsets();
    }

    private void ApplyPlatformInsets()
    {
#if ANDROID
        var resources = Android.App.Application.Context?.Resources;
        if (resources is null)
        {
            return;
        }

        var resourceId = resources.GetIdentifier("status_bar_height", "dimen", "android");
        if (resourceId <= 0)
        {
            return;
        }

        var statusBarHeightInPixels = resources.GetDimensionPixelSize(resourceId);
        var density = resources.DisplayMetrics?.Density ?? 1f;
        var statusBarHeight = statusBarHeightInPixels / density;

        Padding = new Thickness(0, statusBarHeight, 0, 0);
#endif
    }
}
