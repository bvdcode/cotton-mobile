// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile
{
    [Activity(Theme = "@style/Cotton.SplashTheme", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTask, ResizeableActivity = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "*/*")]
    [IntentFilter(new[] { Intent.ActionSendMultiple }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "*/*")]
    public class MainActivity : MauiAppCompatActivity
    {
        private const long FirstSystemBarReapplyDelayMilliseconds = 250;
        private const string ShareIntentLogTag = "CottonShare";
        private const long SecondSystemBarReapplyDelayMilliseconds = 1000;
        private const long ThirdSystemBarReapplyDelayMilliseconds = 2500;
        private const long FourthSystemBarReapplyDelayMilliseconds = 5000;
        private const string NotificationIntentLogTag = "CottonNotification";
#if DEBUG
        private const string VisualQaIntentLogTag = "CottonVisualQa";
        private const string VisualQaPageExtra = "dev.cottoncloud.app.debug.extra.VISUAL_QA_PAGE";
        private const long VisualQaLaunchDelayMilliseconds = 1000;
#endif
        private Android.Views.View? _statusBarScrim;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            RequestApplySystemBars();
            StageShareIntent(Intent);
            StageNotificationIntent(Intent);
#if DEBUG
            StageVisualQaIntent(Intent);
#endif
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            StageShareIntent(intent);
            StageNotificationIntent(intent);
#if DEBUG
            StageVisualQaIntent(intent);
#endif
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

#if DEBUG
        private void StageVisualQaIntent(Intent? intent)
        {
            string? pageName = intent?.GetStringExtra(VisualQaPageExtra);
            if (string.IsNullOrWhiteSpace(pageName))
            {
                return;
            }

            intent?.RemoveExtra(VisualQaPageExtra);
            Android.Views.View? decorView = Window?.DecorView;
            if (decorView is null)
            {
                Log.Warn(VisualQaIntentLogTag, "Ignored visual QA launch because the Android window is unavailable.");
                return;
            }

            string normalizedPageName = pageName.Trim().ToLowerInvariant();
            decorView.PostDelayed(
                () => _ = OpenVisualQaPageAsync(normalizedPageName),
                VisualQaLaunchDelayMilliseconds);
        }

        private static async Task OpenVisualQaPageAsync(string pageName)
        {
            try
            {
                IServiceProvider? services = IPlatformApplication.Current?.Services;
                if (services is null)
                {
                    Log.Warn(VisualQaIntentLogTag, "Ignored visual QA launch because application services are unavailable.");
                    return;
                }

                Uri instanceUri = services.GetRequiredService<CottonMobileOptions>().DefaultInstanceUri;
                switch (pageName)
                {
                    case "storage":
                        await services.GetRequiredService<IStorageSettingsPageService>().OpenAsync();
                        break;
                    case "notifications":
                        await services.GetRequiredService<INotificationSettingsPageService>().OpenAsync();
                        break;
                    case "sync":
                        await services.GetRequiredService<ISyncSettingsPageService>().OpenAsync(instanceUri);
                        break;
                    case "transfers":
                        await services.GetRequiredService<ITransfersPageService>().OpenAsync(instanceUri);
                        break;
                    case "backup":
                        await services.GetRequiredService<IBackupSetupPageService>().OpenAsync(instanceUri);
                        break;
                    case "capture":
                        await services.GetRequiredService<ICaptureInboxPageService>().OpenAsync(instanceUri);
                        break;
                    case "recent":
                        await services.GetRequiredService<IRecentFilesPageService>().OpenAsync(instanceUri);
                        break;
                    case "trash":
                        await services.GetRequiredService<ITrashPageService>().OpenAsync(instanceUri);
                        break;
                    case "activity":
                        await services.GetRequiredService<IActivityFeedPageService>().OpenAsync(instanceUri);
                        break;
                    default:
                        Log.Warn(VisualQaIntentLogTag, $"Ignored unknown visual QA page '{pageName}'.");
                        return;
                }

                Log.Info(VisualQaIntentLogTag, $"Opened visual QA page '{pageName}'.");
            }
            catch (Exception exception)
            {
                Log.Warn(VisualQaIntentLogTag, $"Failed to open visual QA page '{pageName}'. {exception}");
            }
        }
#endif

        private void RequestApplySystemBars()
        {
            ApplySystemBars();
            Android.Views.View? decorView = Window?.DecorView;
            if (decorView is null)
            {
                return;
            }

            decorView.Post(ApplySystemBars);
            decorView.PostDelayed(ApplySystemBars, FirstSystemBarReapplyDelayMilliseconds);
            decorView.PostDelayed(ApplySystemBars, SecondSystemBarReapplyDelayMilliseconds);
            decorView.PostDelayed(ApplySystemBars, ThirdSystemBarReapplyDelayMilliseconds);
            decorView.PostDelayed(ApplySystemBars, FourthSystemBarReapplyDelayMilliseconds);
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
            Android.Graphics.Color systemBarColor = Resources.GetColor(Resource.Color.cotton_system_bar_background, Theme);
            Android.Views.View decorView = Window.DecorView;
            WindowCompat.SetDecorFitsSystemWindows(Window, true);
            Window.ClearFlags(WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.SetStatusBarColor(systemBarColor);
            Window.SetNavigationBarColor(systemBarColor);
            ApplyStatusBarScrim(systemBarColor);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.NavigationBarDividerColor = systemBarColor;
            }

            bool isNightMode = (configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
            bool useLightStatusBars = !isNightMode;
            bool useLightNavigationBars = !isNightMode;
            WindowInsetsControllerCompat? compatInsetsController = WindowCompat.GetInsetsController(Window, decorView);
            if (compatInsetsController is not null)
            {
                compatInsetsController.AppearanceLightStatusBars = useLightStatusBars;
                compatInsetsController.AppearanceLightNavigationBars = useLightNavigationBars;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                int mask = (int)(
                    WindowInsetsControllerAppearance.LightStatusBars
                    | WindowInsetsControllerAppearance.LightNavigationBars);
                int appearance = 0;
                if (useLightStatusBars)
                {
                    appearance |= (int)WindowInsetsControllerAppearance.LightStatusBars;
                }

                if (useLightNavigationBars)
                {
                    appearance |= (int)WindowInsetsControllerAppearance.LightNavigationBars;
                }

                Window.InsetsController?.SetSystemBarsAppearance(appearance, mask);
                decorView.WindowInsetsController?.SetSystemBarsAppearance(appearance, mask);
            }

            SystemUiFlags flags = useLightStatusBars ? SystemUiFlags.LightStatusBar : 0;
            if (useLightNavigationBars && Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                flags |= SystemUiFlags.LightNavigationBar;
            }

            decorView.SystemUiFlags = flags;
#pragma warning restore CA1416, CA1422
        }

        private void ApplyStatusBarScrim(Android.Graphics.Color statusBarColor)
        {
            if (Window?.DecorView is not ViewGroup decorView || Resources is null)
            {
                return;
            }

            int statusBarHeight = GetStatusBarHeight();
            if (statusBarHeight <= 0)
            {
                return;
            }

            if (_statusBarScrim is null)
            {
                _statusBarScrim = new Android.Views.View(this)
                {
                    Clickable = false,
                    Focusable = false,
                };
                decorView.AddView(_statusBarScrim);
            }

            _statusBarScrim.SetBackgroundColor(statusBarColor);
            _statusBarScrim.LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                statusBarHeight,
                GravityFlags.Top);
            _statusBarScrim.BringToFront();
        }

        private int GetStatusBarHeight()
        {
            if (Resources is null)
            {
                return 0;
            }

            int resourceId = Resources.GetIdentifier("status_bar_height", "dimen", "android");
            return resourceId <= 0 ? 0 : Resources.GetDimensionPixelSize(resourceId);
        }
    }
}
