// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using System.Runtime.Versioning;
using Android.App;
using Android.App.Usage;
using Android.Content;
using Cotton.Mobile.Services;
using AndroidSettings = Android.Provider.Settings;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidBackgroundSyncRestrictionService : IBackgroundSyncRestrictionService
    {
        public bool IsRestricted => OperatingSystem.IsAndroidVersionAtLeast(28)
            && IsRestrictedOnAndroid28OrLater();

        public Task OpenSettingsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Context context = global::Android.App.Application.Context;
            using AndroidUri packageUri = AndroidUri.Parse($"package:{context.PackageName}")
                ?? throw new InvalidOperationException("Android package URI is unavailable.");
            using Intent intent = new(AndroidSettings.ActionApplicationDetailsSettings, packageUri);
            intent.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);
            return Task.CompletedTask;
        }

        [SupportedOSPlatform("android28.0")]
        private static bool IsRestrictedOnAndroid28OrLater()
        {
            Context context = global::Android.App.Application.Context;
            ActivityManager activityManager = context.GetSystemService(Context.ActivityService) as ActivityManager
                ?? throw new InvalidOperationException("Android activity manager is unavailable.");
            return activityManager.IsBackgroundRestricted
                || (OperatingSystem.IsAndroidVersionAtLeast(30)
                    && IsStandbyBucketRestrictedOnAndroid30OrLater(context));
        }

        [SupportedOSPlatform("android30.0")]
        private static bool IsStandbyBucketRestrictedOnAndroid30OrLater(Context context)
        {
            UsageStatsManager usageStatsManager = context.GetSystemService(Context.UsageStatsService) as UsageStatsManager
                ?? throw new InvalidOperationException("Android usage stats manager is unavailable.");
            return (int)usageStatsManager.AppStandbyBucket >= (int)StandbyBucket.Restricted;
        }
    }
}
#endif
