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
    public class AndroidCameraBackupMediaAccessPolicy : ICottonCameraBackupMediaAccessPolicy
    {
        private const string RequestedPreferenceKey = "cotton.cameraBackup.mediaAccess.requested";

        private readonly IPreferences _preferences;

        public AndroidCameraBackupMediaAccessPolicy(IPreferences preferences)
        {
            ArgumentNullException.ThrowIfNull(preferences);

            _preferences = preferences;
        }

        public Task<CottonCameraBackupMediaAccessState> GetAccessStateAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(GetCurrentAccessState());
        }

        public async Task<CottonCameraBackupMediaAccessState> RequestAccessAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _preferences.Set(RequestedPreferenceKey, true);

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return CottonCameraBackupMediaAccessState.Unavailable;
            }

            try
            {
                _ = await Permissions.RequestAsync<CameraBackupMediaPermission>()
                    .ConfigureAwait(false);
            }
            catch (PermissionException)
            {
                return CottonCameraBackupMediaAccessState.Unavailable;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return GetCurrentAccessState();
        }

        public Task OpenSettingsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppInfo.Current.ShowSettingsUI();
            return Task.CompletedTask;
        }

        public Task<bool> CanReadMediaAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CottonCameraBackupMediaAccessState state = GetCurrentAccessState();
            return Task.FromResult(CottonCameraBackupMediaAccessRules.CanReadAnyMedia(state));
        }

        private CottonCameraBackupMediaAccessState GetCurrentAccessState()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                return CottonCameraBackupMediaAccessState.Allowed;
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return CottonCameraBackupMediaAccessState.Unavailable;
            }

            if (HasPermission(Manifest.Permission.ReadExternalStorage))
            {
                return CottonCameraBackupMediaAccessState.Allowed;
            }

            return WasRequested()
                ? CottonCameraBackupMediaAccessState.Denied
                : CottonCameraBackupMediaAccessState.NotRequested;
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

        private class CameraBackupMediaPermission : Permissions.BasePlatformPermission
        {
            public override (string androidPermission, bool isRuntime)[] RequiredPermissions
            {
                get
                {
                    return
                    [
                        (Manifest.Permission.ReadExternalStorage, true),
                    ];
                }
            }
        }
    }
}
#endif
