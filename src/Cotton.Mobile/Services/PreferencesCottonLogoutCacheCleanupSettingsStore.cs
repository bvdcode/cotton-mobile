using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesCottonLogoutCacheCleanupSettingsStore : ICottonLogoutCacheCleanupSettingsStore
    {
        private const string ClearCachedFilesOnLogoutKey =
            "Cotton.Mobile.Security.Logout.ClearCachedFilesOnLogout";

        private readonly IPreferences _preferences;
        private readonly ILogger<PreferencesCottonLogoutCacheCleanupSettingsStore> _logger;

        public PreferencesCottonLogoutCacheCleanupSettingsStore(
            IPreferences preferences,
            ILogger<PreferencesCottonLogoutCacheCleanupSettingsStore> logger)
        {
            ArgumentNullException.ThrowIfNull(preferences);
            ArgumentNullException.ThrowIfNull(logger);

            _preferences = preferences;
            _logger = logger;
        }

        public Task<CottonLogoutCacheCleanupSettings> GetAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return Task.FromResult(new CottonLogoutCacheCleanupSettings(
                    _preferences.Get(
                        ClearCachedFilesOnLogoutKey,
                        CottonLogoutCacheCleanupSettings.Default.ClearCachedFilesOnLogout)));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to read Cotton mobile logout cache cleanup settings.");
                return Task.FromResult(CottonLogoutCacheCleanupSettings.Default);
            }
        }

        public Task SaveAsync(
            CottonLogoutCacheCleanupSettings settings,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);
            cancellationToken.ThrowIfCancellationRequested();

            _preferences.Set(ClearCachedFilesOnLogoutKey, settings.ClearCachedFilesOnLogout);
            return Task.CompletedTask;
        }
    }
}
