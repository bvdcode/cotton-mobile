// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Platforms.Android
{
    [Activity(
        Theme = "@style/Cotton.SplashTheme",
        MainLauncher = true,
        Exported = true,
        LaunchMode = LaunchMode.SingleTask,
        TaskAffinity = "",
        ResizeableActivity = true,
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

            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                SplashScreen.SetOnExitAnimationListener(new ImmediateSplashScreenExitListener(this));
            }

            ApplySystemBars();
        }

        protected override void OnResume()
        {
            base.OnResume();
            ApplySystemBars();
            GetForegroundService()?.NotifyResumed();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            IAndroidDocumentTreeActivityResultBridge? resultBridge = IPlatformApplication.Current?.Services
                .GetService<IAndroidDocumentTreeActivityResultBridge>();
            if (resultBridge?.TryHandleActivityResult(requestCode, resultCode, data) == true)
            {
                return;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }

        protected override void OnStop()
        {
            GetForegroundService()?.NotifyStopped();
            base.OnStop();
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            ApplySystemBars();
        }

        internal void RefreshSystemBars()
        {
            ApplySystemBars();
        }

        private static IApplicationForegroundService? GetForegroundService()
        {
            return IPlatformApplication.Current?.Services
                .GetService<IApplicationForegroundService>();
        }

        private void ApplySystemBars()
        {
            if (Window is null || Resources?.Configuration is not Configuration configuration)
            {
                return;
            }

#pragma warning disable CA1416, CA1422
            global::Android.Graphics.Color systemBarColor = Resources.GetColor(
                Resource.Color.cotton_system_bar_background,
                Theme);
            global::Android.Views.View decorView = Window.DecorView;
            WindowCompat.SetDecorFitsSystemWindows(Window, true);
            Window.ClearFlags(WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.SetStatusBarColor(systemBarColor);
            Window.SetNavigationBarColor(systemBarColor);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.NavigationBarDividerColor = systemBarColor;
            }

            bool useLightIcons = (configuration.UiMode & UiMode.NightMask) != UiMode.NightYes;
            WindowInsetsControllerCompat? insetsController = WindowCompat.GetInsetsController(Window, decorView);
            if (insetsController is not null)
            {
                insetsController.AppearanceLightStatusBars = useLightIcons;
                insetsController.AppearanceLightNavigationBars = useLightIcons;
            }
#pragma warning restore CA1416, CA1422
        }
    }
}
