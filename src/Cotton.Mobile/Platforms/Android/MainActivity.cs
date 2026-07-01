// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

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
    [Activity(Theme = "@style/Cotton.SplashTheme", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTask, ResizeableActivity = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "*/*")]
    [IntentFilter(new[] { Intent.ActionSendMultiple }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "*/*")]
    public class MainActivity : MauiAppCompatActivity
    {
        private const string ShareIntentLogTag = "CottonShare";
        private const string NotificationIntentLogTag = "CottonNotification";

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            RequestApplySystemBars();
            StageShareIntent(Intent);
            StageNotificationIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            StageShareIntent(intent);
            StageNotificationIntent(intent);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            IAndroidDeviceCredentialUnlockActivityResultBridge? unlockResultBridge =
                IPlatformApplication.Current?.Services
                    .GetService<IAndroidDeviceCredentialUnlockActivityResultBridge>();
            if (unlockResultBridge?.TryHandleActivityResult(requestCode, resultCode, data) == true)
            {
                return;
            }

            IAndroidDocumentScanActivityResultBridge? scanResultBridge = IPlatformApplication.Current?.Services
                .GetService<IAndroidDocumentScanActivityResultBridge>();
            if (scanResultBridge?.TryHandleActivityResult(requestCode, resultCode, data) == true)
            {
                return;
            }

            IAndroidDocumentTreeActivityResultBridge? documentTreeResultBridge =
                IPlatformApplication.Current?.Services
                    .GetService<IAndroidDocumentTreeActivityResultBridge>();
            if (documentTreeResultBridge?.TryHandleActivityResult(requestCode, resultCode, data) == true)
            {
                return;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }

        protected override void OnResume()
        {
            base.OnResume();

            RequestApplySystemBars();

            IPlatformApplication.Current?.Services
                .GetService<IApplicationForegroundService>()
                ?.NotifyResumed();

            ICottonWindowPrivacyService? windowPrivacyService = IPlatformApplication.Current?.Services
                .GetService<ICottonWindowPrivacyService>();
            if (windowPrivacyService is not null)
            {
                _ = windowPrivacyService.ApplyAsync();
            }
        }

        protected override void OnStop()
        {
            IPlatformApplication.Current?.Services
                .GetService<IApplicationForegroundService>()
                ?.NotifyStopped();

            base.OnStop();
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            RequestApplySystemBars();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            if (hasFocus)
            {
                RequestApplySystemBars();
            }
        }

        private void StageShareIntent(Intent? intent)
        {
            if (intent?.Action is not Intent.ActionSend and not Intent.ActionSendMultiple)
            {
                return;
            }

            IAndroidShareIntentStagingService? stagingService = IPlatformApplication.Current?.Services
                .GetService<IAndroidShareIntentStagingService>();
            if (stagingService is null)
            {
                Log.Info(
                    ShareIntentLogTag,
                    "Deferred Android share intent until Capture Inbox is available; staging service is unavailable.");
                return;
            }

            _ = StageShareIntentAsync(stagingService, intent);
        }

        private async Task StageShareIntentAsync(
            IAndroidShareIntentStagingService stagingService,
            Intent intent)
        {
            try
            {
                CottonShareIntakeSnapshot? snapshot =
                    await stagingService.StageAsync(intent, ContentResolver).ConfigureAwait(false);
                if (snapshot is null)
                {
                    return;
                }

                Log.Info(
                    ShareIntentLogTag,
                    $"Staged Android share intent for Capture Inbox. Id={snapshot.Id}; Status={snapshot.Status}; Items={snapshot.ItemCount}.");
            }
            catch (Exception exception)
            {
                Log.Warn(ShareIntentLogTag, $"Failed to stage Android share intent. {exception}");
            }
        }

        private void StageNotificationIntent(Intent? intent)
        {
            CottonNotificationLaunchRequest? request = TryCreateNotificationLaunchRequest(intent);
            if (request is null)
            {
                return;
            }

            ICottonNotificationLaunchState? launchState = IPlatformApplication.Current?.Services
                .GetService<ICottonNotificationLaunchState>();
            if (launchState is null)
            {
                Log.Info(
                    NotificationIntentLogTag,
                    "Deferred notification launch; launch state is unavailable.");
                return;
            }

            launchState.NotifyNotificationOpened(request);
            ClearNotificationLaunchExtras(intent);
            Log.Info(
                NotificationIntentLogTag,
                $"Staged notification launch. Id={request.NotificationId:D}; Category={request.Category}.");
        }

        private static CottonNotificationLaunchRequest? TryCreateNotificationLaunchRequest(Intent? intent)
        {
            if (intent is null || !intent.GetBooleanExtra(AndroidNotificationIntentExtras.IsNotificationLaunch, false))
            {
                return null;
            }

            string? notificationId = intent.GetStringExtra(AndroidNotificationIntentExtras.NotificationId);
            string? eventCategory = intent.GetStringExtra(AndroidNotificationIntentExtras.EventCategory);
            if (!Guid.TryParse(notificationId, out Guid parsedNotificationId)
                || !Enum.TryParse(eventCategory, ignoreCase: false, out CottonRemotePushEventCategory parsedCategory))
            {
                return null;
            }

            return CottonNotificationLaunchRequest.TryCreate(parsedNotificationId, parsedCategory);
        }

        private static void ClearNotificationLaunchExtras(Intent? intent)
        {
            intent?.RemoveExtra(AndroidNotificationIntentExtras.IsNotificationLaunch);
            intent?.RemoveExtra(AndroidNotificationIntentExtras.NotificationId);
            intent?.RemoveExtra(AndroidNotificationIntentExtras.EventCategory);
        }

        private void RequestApplySystemBars()
        {
            ApplySystemBars();
            Window?.DecorView.Post(ApplySystemBars);
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
            Android.Graphics.Color systemBarColor = Resources.GetColor(Resource.Color.cotton_surface, Theme);
            Window.SetStatusBarColor(systemBarColor);
            Window.SetNavigationBarColor(systemBarColor);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.NavigationBarDividerColor = systemBarColor;
            }

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
