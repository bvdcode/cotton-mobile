using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile;

[Activity(Theme = "@style/Cotton.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        ApplySystemBars();
    }

    protected override void OnResume()
    {
        base.OnResume();

        ApplySystemBars();

        IPlatformApplication.Current?.Services
            .GetService<IApplicationForegroundService>()
            ?.NotifyResumed();
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);

        ApplySystemBars();
    }

    private void ApplySystemBars()
    {
        if (Window is null || Resources is null)
        {
            return;
        }

        if (Build.VERSION.SdkInt < BuildVersionCodes.M)
        {
            return;
        }

        Configuration? configuration = Resources.Configuration;
        if (configuration is null)
        {
            return;
        }

#pragma warning disable CA1416, CA1422
        bool isNightMode = (configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            int mask = (int)(
                WindowInsetsControllerAppearance.LightStatusBars
                | WindowInsetsControllerAppearance.LightNavigationBars);
            int appearance = isNightMode ? (int)WindowInsetsControllerAppearance.None : mask;
            Window.InsetsController?.SetSystemBarsAppearance(appearance, mask);
            return;
        }

        SystemUiFlags flags = isNightMode ? 0 : SystemUiFlags.LightStatusBar;
        if (!isNightMode && Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            flags |= SystemUiFlags.LightNavigationBar;
        }

        Window.DecorView.SystemUiFlags = flags;
#pragma warning restore CA1416, CA1422
    }
}
