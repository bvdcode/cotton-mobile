using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesCottonAppLockSettingsStore : ICottonAppLockSettingsStore
    {
        private const string IsEnabledKey = "Cotton.Mobile.Security.AppLock.IsEnabled";

        private readonly IPreferences _preferences;
        private readonly ILogger<PreferencesCottonAppLockSettingsStore> _logger;

        public PreferencesCottonAppLockSettingsStore(
            IPreferences preferences,
            ILogger<PreferencesCottonAppLockSettingsStore> logger)
        {
            ArgumentNullException.ThrowIfNull(preferences);
            ArgumentNullException.ThrowIfNull(logger);

            _preferences = preferences;
            _logger = logger;
        }

        public Task<CottonAppLockSettings> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return Task.FromResult(new CottonAppLockSettings(_preferences.Get(IsEnabledKey, false)));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to read Cotton app lock settings.");
                return Task.FromResult(CottonAppLockSettings.Disabled);
            }
        }

        public Task SaveAsync(
            CottonAppLockSettings settings,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _preferences.Set(IsEnabledKey, settings.IsEnabled);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save Cotton app lock settings.");
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
