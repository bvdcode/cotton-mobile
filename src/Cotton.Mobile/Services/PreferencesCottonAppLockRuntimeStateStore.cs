using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesCottonAppLockRuntimeStateStore : ICottonAppLockRuntimeStateStore
    {
        private const string LastBackgroundedAtUtcTicksKey =
            "Cotton.Mobile.Security.AppLock.LastBackgroundedAtUtcTicks";
        private const string LastUnlockedAtUtcTicksKey =
            "Cotton.Mobile.Security.AppLock.LastUnlockedAtUtcTicks";

        private readonly IPreferences _preferences;
        private readonly ILogger<PreferencesCottonAppLockRuntimeStateStore> _logger;

        public PreferencesCottonAppLockRuntimeStateStore(
            IPreferences preferences,
            ILogger<PreferencesCottonAppLockRuntimeStateStore> logger)
        {
            ArgumentNullException.ThrowIfNull(preferences);
            ArgumentNullException.ThrowIfNull(logger);

            _preferences = preferences;
            _logger = logger;
        }

        public Task<CottonAppLockRuntimeState> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return Task.FromResult(new CottonAppLockRuntimeState(
                    GetTimestamp(LastBackgroundedAtUtcTicksKey),
                    GetTimestamp(LastUnlockedAtUtcTicksKey)));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to read Cotton mobile app lock runtime state.");
                return Task.FromResult(CottonAppLockRuntimeState.Empty);
            }
        }

        public Task SaveAsync(
            CottonAppLockRuntimeState runtimeState,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(runtimeState);
            cancellationToken.ThrowIfCancellationRequested();

            SaveTimestamp(LastBackgroundedAtUtcTicksKey, runtimeState.LastBackgroundedAtUtc);
            SaveTimestamp(LastUnlockedAtUtcTicksKey, runtimeState.LastUnlockedAtUtc);
            return Task.CompletedTask;
        }

        private DateTimeOffset? GetTimestamp(string key)
        {
            long ticks = _preferences.Get(key, 0L);
            return ticks <= 0
                ? null
                : new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        private void SaveTimestamp(string key, DateTimeOffset? value)
        {
            if (value is null)
            {
                _preferences.Remove(key);
                return;
            }

            _preferences.Set(key, value.Value.ToUniversalTime().Ticks);
        }
    }
}
