// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android;
using Android.Content.PM;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile
{
    public class AndroidNotificationPermissionService : ICottonNotificationPermissionService
    {
        private const string RequestedPreferenceKey = "Cotton.Mobile.Notifications.PermissionRequested";

        private readonly IPreferences _preferences;
        private readonly ILogger<AndroidNotificationPermissionService> _logger;

        public AndroidNotificationPermissionService(
            IPreferences preferences,
            ILogger<AndroidNotificationPermissionService> logger)
        {
            ArgumentNullException.ThrowIfNull(preferences);
            ArgumentNullException.ThrowIfNull(logger);

            _preferences = preferences;
            _logger = logger;
        }

        public Task<bool> CanPostAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return Task.FromResult(true);
            }

            bool isGranted = Android.App.Application.Context.CheckSelfPermission(
                Manifest.Permission.PostNotifications) == Permission.Granted;
            return Task.FromResult(isGranted);
        }

        public async Task RequestIfNeededAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!OperatingSystem.IsAndroidVersionAtLeast(33)
                || await CanPostAsync(cancellationToken).ConfigureAwait(false)
                || _preferences.Get(RequestedPreferenceKey, false))
            {
                return;
            }

            _preferences.Set(RequestedPreferenceKey, true);
            try
            {
                PermissionStatus status = await Permissions
                    .RequestAsync<CottonPostNotificationsPermissionRequest>()
                    .ConfigureAwait(false);
                _logger.LogInformation("Cotton notification permission result: {PermissionStatus}.", status);
            }
            catch (PermissionException exception)
            {
                _logger.LogWarning(exception, "Cotton notification permission is unavailable.");
            }
        }
    }
}
