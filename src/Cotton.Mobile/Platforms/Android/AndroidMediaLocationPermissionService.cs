// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidMediaLocationPermissionService(
        IPreferences preferences,
        ILogger<AndroidMediaLocationPermissionService> logger) : ICottonMediaLocationPermissionService
    {
        private const string RequestedPreferenceKey = "Cotton.Mobile.MediaLocation.PermissionRequested";

        public bool IsSupported => OperatingSystem.IsAndroidVersionAtLeast(29);

        public bool IsGranted => AndroidMediaContentAccess.HasLocationPermission;

        public bool CanRequest => !preferences.Get(RequestedPreferenceKey, false)
            || Permissions.ShouldShowRationale<CottonMediaLocationPermissionRequest>();

        public async Task RequestAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsSupported || IsGranted)
            {
                return;
            }

            if (!CanRequest)
            {
                await MainThread.InvokeOnMainThreadAsync(AppInfo.Current.ShowSettingsUI).ConfigureAwait(false);
                return;
            }

            preferences.Set(RequestedPreferenceKey, true);
            PermissionStatus status = await MainThread.InvokeOnMainThreadAsync(
                    Permissions.RequestAsync<CottonMediaLocationPermissionRequest>)
                .ConfigureAwait(false);
            CottonLog.MediaLocationPermissionResult(logger, status, IsGranted);
        }
    }
}
