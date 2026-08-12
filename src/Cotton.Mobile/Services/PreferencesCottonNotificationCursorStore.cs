// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesCottonNotificationCursorStore : ICottonNotificationCursorStore
    {
        private const string CursorKey = "Cotton.Mobile.Notifications.Cursor.V2";

        private static readonly JsonSerializerOptions SerializerOptions =
            new(JsonSerializerDefaults.Web);

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
                string value = _preferences.Get(CursorKey, string.Empty);
                if (string.IsNullOrEmpty(value))
                {
                    return Task.FromResult<CottonNotificationCursor?>(null);
                }

                CottonNotificationCursor cursor = JsonSerializer
                    .Deserialize<CottonNotificationCursor>(value, SerializerOptions)
                    ?? throw new InvalidDataException("The Cotton notification cursor is empty.");
                return Task.FromResult<CottonNotificationCursor?>(cursor);
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to read the Cotton notification cursor.", exception);
                throw;
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
                string value = JsonSerializer.Serialize(cursor, SerializerOptions);
                _preferences.Set(CursorKey, value);
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to save the Cotton notification cursor.", exception);
                throw;
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _preferences.Remove(CursorKey);
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to clear the Cotton notification cursor.", exception);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
