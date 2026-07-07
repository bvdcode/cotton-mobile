// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android;
using Android.App;
using Android.Content.PM;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class AndroidNotificationPermissionService : ICottonNotificationPermissionService
    {
        private const string RequestedPreferenceKey = "cotton.notifications.permission.requested";

        private readonly IPreferences _preferences;

        public AndroidNotificationPermissionService(IPreferences preferences)
        {
            ArgumentNullException.ThrowIfNull(preferences);

            _preferences = preferences;
        }

        public Task<CottonNotificationPermissionState> GetPermissionStateAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(GetCurrentPermissionState());
        }

        public async Task<CottonNotificationPermissionState> RequestPermissionAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return CottonNotificationPermissionState.NotRequired;
            }

            _preferences.Set(RequestedPreferenceKey, true);

            try
            {
                _ = await Permissions.RequestAsync<NotificationPermission>()
                    .ConfigureAwait(false);
            }
            catch (PermissionException)
            {
                return CottonNotificationPermissionState.Unavailable;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return GetCurrentPermissionState();
        }

        public Task OpenSettingsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppInfo.Current.ShowSettingsUI();
            return Task.CompletedTask;
        }

        private CottonNotificationPermissionState GetCurrentPermissionState()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return CottonNotificationPermissionState.NotRequired;
            }

            if (HasPermission(Manifest.Permission.PostNotifications))
            {
                return CottonNotificationPermissionState.Allowed;
            }

            return WasRequested()
                ? CottonNotificationPermissionState.Denied
                : CottonNotificationPermissionState.NotRequested;
        }

        private bool WasRequested()
        {
            return _preferences.Get(RequestedPreferenceKey, false);
        }

        private static bool HasPermission(string permission)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                return true;
            }

            return Android.App.Application.Context.CheckSelfPermission(permission) == Permission.Granted;
        }

        private class NotificationPermission : Permissions.BasePlatformPermission
        {
            public override (string androidPermission, bool isRuntime)[] RequiredPermissions
            {
                get
                {
                    if (!OperatingSystem.IsAndroidVersionAtLeast(33))
                    {
                        return [];
                    }

                    return
                    [
                        (Manifest.Permission.PostNotifications, true),
                    ];
                }
            }
        }
    }
}
#endif
