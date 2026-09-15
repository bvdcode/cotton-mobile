// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.App;
using Android.Content;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Cotton.Sdk.Notifications;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidSessionNotificationService(
        ICottonNotificationPermissionService permissionService,
        IPreferences preferences) : ICottonSessionNotificationService
    {
        private const string ShownPreferenceKey = "Cotton.Mobile.Sync.SignInNotificationShown";
        private readonly Lock _gate = new();

        public async Task ShowSignInRequiredAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await permissionService.CanPostAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            lock (_gate)
            {
                if (preferences.Get(ShownPreferenceKey, false))
                {
                    return;
                }

                AndroidNotificationChannels.EnsureCreated();
                Context context = global::Android.App.Application.Context;
                NotificationManager manager = GetManager(context);
                manager.Notify(
                    AndroidNotificationConstants.SignInRequiredNotificationId,
                    AndroidNotificationBuilder.Build(
                        context,
                        AndroidNotificationConstants.SignInRequiredNotificationId,
                        AppResources.AppTitle,
                        AppResources.SyncSignInRequired,
                        CottonNotificationPriority.High));
                preferences.Set(ShownPreferenceKey, true);
            }
        }

        public void ClearSignInRequired()
        {
            lock (_gate)
            {
                GetManager(global::Android.App.Application.Context)
                    .Cancel(AndroidNotificationConstants.SignInRequiredNotificationId);
                preferences.Remove(ShownPreferenceKey);
            }
        }

        private static NotificationManager GetManager(Context context)
        {
            return context.GetSystemService(Context.NotificationService) as NotificationManager
                ?? throw new InvalidOperationException("Android notification manager is unavailable.");
        }
    }
}
