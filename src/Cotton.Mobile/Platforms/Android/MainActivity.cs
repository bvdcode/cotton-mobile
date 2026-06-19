using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Util;
using Android.Views;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile
{
    [Activity(Theme = "@style/Cotton.SplashTheme", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "*/*")]
    [IntentFilter(new[] { Intent.ActionSendMultiple }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "*/*")]
    public class MainActivity : MauiAppCompatActivity
    {
        private const string ShareIntentLogTag = "CottonShare";

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ApplySystemBars();
            DeferShareIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            DeferShareIntent(intent);
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

        private static void DeferShareIntent(Intent? intent)
        {
            if (intent?.Action is not Intent.ActionSend and not Intent.ActionSendMultiple)
            {
                return;
            }

            string action = intent.Action ?? "unknown";
            string mimeType = intent.Type ?? "unknown";
            Log.Info(
                ShareIntentLogTag,
                $"Deferred Android share intent until Capture Inbox is available. Action={action}; MimeType={mimeType}.");
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
}
