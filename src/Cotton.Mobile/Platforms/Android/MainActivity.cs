// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

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
        private const int DocumentTreeBridgeAttemptCount = 20;
        private static readonly TimeSpan DocumentTreeBridgeAttemptDelay = TimeSpan.FromMilliseconds(100);

        private ActivityResultLauncher? _documentTreeLauncher;
        private bool _documentTreeBridgeAttachmentScheduled;
        private ActivityResult? _pendingDocumentTreeResult;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            _documentTreeLauncher = RegisterForActivityResult(
                new ActivityResultContracts.StartActivityForResult(),
                new AndroidActivityResultCallback(HandleDocumentTreeResult));
            base.OnCreate(savedInstanceState);
            SupportFragmentManager.RegisterFragmentLifecycleCallbacks(new AndroidSystemBarAppearance(), recursive: true);

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
            ScheduleDocumentTreeBridgeAttachment();
            GetForegroundService()?.NotifyResumed();
        }

        private void HandleDocumentTreeResult(ActivityResult result)
        {
            IAndroidDocumentTreeActivityResultBridge? bridge = GetDocumentTreeResultBridge();
            if (bridge is null)
            {
                _pendingDocumentTreeResult = result;
                ScheduleDocumentTreeBridgeAttachment();
                return;
            }

            bridge.HandleActivityResult((Result)result.ResultCode, result.Data);
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
            foreach (AndroidX.Fragment.App.Fragment fragment in SupportFragmentManager.Fragments)
            {
                AndroidSystemBarAppearance.RefreshDialog(fragment);
            }
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

        private static IAndroidDocumentTreeActivityResultBridge? GetDocumentTreeResultBridge()
        {
            return IPlatformApplication.Current?.Services
                .GetService<IAndroidDocumentTreeActivityResultBridge>();
        }

        internal void LaunchDocumentTree(Intent intent)
        {
            ArgumentNullException.ThrowIfNull(intent);
            ActivityResultLauncher launcher = _documentTreeLauncher
                ?? throw new InvalidOperationException("Android document-tree launcher is unavailable.");
            launcher.Launch(intent);
        }

        private void ScheduleDocumentTreeBridgeAttachment()
        {
            if (_documentTreeBridgeAttachmentScheduled)
            {
                return;
            }

            _documentTreeBridgeAttachmentScheduled = true;
            _ = AttachDocumentTreeBridgeAsync();
        }

        private async Task AttachDocumentTreeBridgeAsync()
        {
            IAndroidDocumentTreeActivityResultBridge? bridge = null;
            for (int attempt = 0; attempt < DocumentTreeBridgeAttemptCount && bridge is null; attempt++)
            {
                await Task.Delay(DocumentTreeBridgeAttemptDelay);
                bridge = GetDocumentTreeResultBridge();
            }

            if (bridge is null)
            {
                _ = global::Android.Util.Log.Error(
                    nameof(MainActivity),
                    "Android document-tree result bridge is unavailable.");
                _documentTreeBridgeAttachmentScheduled = false;
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ActivityResult? pendingResult = _pendingDocumentTreeResult;
                if (pendingResult is not null)
                {
                    _pendingDocumentTreeResult = null;
                    bridge.HandleActivityResult((Result)pendingResult.ResultCode, pendingResult.Data);
                }

            });
        }

        private void ApplySystemBars()
        {
            if (Window is null || Resources?.Configuration is not Configuration configuration)
            {
                return;
            }

            global::Android.Graphics.Color systemBarColor = Resources.GetColor(
                Resource.Color.cotton_system_bar_background,
                Theme);
            WindowCompat.SetDecorFitsSystemWindows(Window, true);
            Window.ClearFlags(WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            if (!OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                Window.SetStatusBarColor(systemBarColor);
                Window.SetNavigationBarColor(systemBarColor);

                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    Window.NavigationBarDividerColor = systemBarColor;
                }
            }

            AndroidSystemBarAppearance.ApplyIconColors(Window, configuration);
        }
    }
}
