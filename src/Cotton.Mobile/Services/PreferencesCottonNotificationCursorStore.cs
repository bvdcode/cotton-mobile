// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesCottonNotificationCursorStore : ICottonNotificationCursorStore
    {
        private const string InitializedKey = "Cotton.Mobile.Notifications.Cursor.Initialized";
        private const string NotificationIdKey = "Cotton.Mobile.Notifications.Cursor.NotificationId";
        private const string TotalCountKey = "Cotton.Mobile.Notifications.Cursor.TotalCount";

        private readonly IPreferences _preferences;
        private readonly ILogger<PreferencesCottonNotificationCursorStore> _logger;

        public PreferencesCottonNotificationCursorStore(
            IPreferences preferences,
            ILogger<PreferencesCottonNotificationCursorStore> logger)
        {
            ArgumentNullException.ThrowIfNull(preferences);
            ArgumentNullException.ThrowIfNull(logger);

            _preferences = preferences;
            _logger = logger;
        }

        public Task<CottonNotificationCursor?> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (!_preferences.Get(InitializedKey, false))
                {
                    return Task.FromResult<CottonNotificationCursor?>(null);
                }

                int totalCount = _preferences.Get(TotalCountKey, -1);
                string notificationIdValue = _preferences.Get(NotificationIdKey, string.Empty);
                if (totalCount < 0 || !TryParseNotificationId(notificationIdValue, out Guid? notificationId))
                {
                    ClearInvalidCursorBestEffort();
                    return Task.FromResult<CottonNotificationCursor?>(null);
                }

                return Task.FromResult<CottonNotificationCursor?>(
                    new CottonNotificationCursor(notificationId, totalCount));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to read the Cotton notification cursor.");
                return Task.FromResult<CottonNotificationCursor?>(null);
            }
        }

        public Task SaveAsync(
            CottonNotificationCursor cursor,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(cursor);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _preferences.Set(InitializedKey, false);
                _preferences.Set(
                    NotificationIdKey,
                    cursor.LastNotificationId?.ToString("D") ?? string.Empty);
                _preferences.Set(TotalCountKey, cursor.TotalCount);
                _preferences.Set(InitializedKey, true);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save the Cotton notification cursor.");
                throw;
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                RemoveCursor();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to clear the Cotton notification cursor.");
                throw;
            }

            return Task.CompletedTask;
        }

        private static bool TryParseNotificationId(string value, out Guid? notificationId)
        {
            if (string.IsNullOrEmpty(value))
            {
                notificationId = null;
                return true;
            }

            if (Guid.TryParseExact(value, "D", out Guid parsedNotificationId))
            {
                notificationId = parsedNotificationId;
                return true;
            }

            notificationId = null;
            return false;
        }

        private void ClearInvalidCursorBestEffort()
        {
            try
            {
                RemoveCursor();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to clear an invalid Cotton notification cursor.");
            }
        }

        private void RemoveCursor()
        {
            _preferences.Remove(InitializedKey);
            _preferences.Remove(NotificationIdKey);
            _preferences.Remove(TotalCountKey);
        }
    }
}
