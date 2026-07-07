// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesCottonCameraBackupSettingsStore : ICottonCameraBackupSettingsStore
    {
        private const string SettingsKeyPrefix = "Cotton.Mobile.CameraBackup.Settings.";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly IPreferences _preferences;
        private readonly ILogger<PreferencesCottonCameraBackupSettingsStore> _logger;

        public PreferencesCottonCameraBackupSettingsStore(
            IPreferences preferences,
            ILogger<PreferencesCottonCameraBackupSettingsStore> logger)
        {
            ArgumentNullException.ThrowIfNull(preferences);
            ArgumentNullException.ThrowIfNull(logger);

            _preferences = preferences;
            _logger = logger;
        }

        public Task<CottonCameraBackupSettings> GetAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string key = CreateKey(instanceUri);
            string value;
            try
            {
                value = _preferences.Get(key, string.Empty);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to read Cotton camera backup settings.");
                return Task.FromResult(CottonCameraBackupSettings.Default);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return Task.FromResult(CottonCameraBackupSettings.Default);
            }

            try
            {
                CameraBackupSettingsDto? dto =
                    JsonSerializer.Deserialize<CameraBackupSettingsDto>(value, SerializerOptions);
                return Task.FromResult(dto?.ToSettings() ?? CottonCameraBackupSettings.Default);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to parse Cotton camera backup settings.");
                RemoveInvalidSettingsBestEffort(key);
                return Task.FromResult(CottonCameraBackupSettings.Default);
            }
        }

        public Task SaveAsync(
            Uri instanceUri,
            CottonCameraBackupSettings settings,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);
            cancellationToken.ThrowIfCancellationRequested();

            string key = CreateKey(instanceUri);
            string value = JsonSerializer.Serialize(CameraBackupSettingsDto.FromSettings(settings), SerializerOptions);
            try
            {
                _preferences.Set(key, value);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save Cotton camera backup settings.");
                throw;
            }

            return Task.CompletedTask;
        }

        private static string CreateKey(Uri instanceUri)
        {
            return $"{SettingsKeyPrefix}{CottonMobileStoragePaths.CreateInstanceStorageKey(instanceUri)}";
        }

        private void RemoveInvalidSettingsBestEffort(string key)
        {
            try
            {
                _preferences.Remove(key);
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Failed to remove invalid Cotton camera backup settings.");
            }
        }

        private class CameraBackupSettingsDto
        {
            public bool IsEnabled { get; set; }

            public CameraBackupDestinationDto? Destination { get; set; }

            public bool PhotosOnly { get; set; } = true;

            public bool WifiOnly { get; set; } = true;

            public bool AllowCellular { get; set; }

            public bool ChargingOnly { get; set; }

            public static CameraBackupSettingsDto FromSettings(CottonCameraBackupSettings settings)
            {
                return new CameraBackupSettingsDto
                {
                    IsEnabled = settings.IsEnabled,
                    Destination = settings.Destination is null
                        ? null
                        : new CameraBackupDestinationDto
                        {
                            FolderId = settings.Destination.FolderId,
                            FolderName = settings.Destination.FolderName,
                            Path = settings.Destination.Path,
                        },
                    PhotosOnly = settings.PhotosOnly,
                    WifiOnly = settings.WifiOnly,
                    AllowCellular = settings.AllowCellular,
                    ChargingOnly = settings.ChargingOnly,
                };
            }

            public CottonCameraBackupSettings ToSettings()
            {
                return new CottonCameraBackupSettings(
                    isEnabled: false,
                    Destination?.ToSnapshot(),
                    PhotosOnly,
                    WifiOnly,
                    AllowCellular,
                    ChargingOnly);
            }
        }

        private class CameraBackupDestinationDto
        {
            public Guid FolderId { get; set; }

            public string? FolderName { get; set; }

            public string? Path { get; set; }

            public CottonUploadDestinationSnapshot? ToSnapshot()
            {
                if (FolderId == Guid.Empty)
                {
                    return null;
                }

                return new CottonUploadDestinationSnapshot(
                    FolderId,
                    FolderName ?? string.Empty,
                    Path);
            }
        }
    }
}
