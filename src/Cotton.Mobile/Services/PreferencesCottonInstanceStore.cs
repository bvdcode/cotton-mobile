using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesCottonInstanceStore : ICottonInstanceStore
    {
        private const string InstanceUrlKey = "Cotton.Mobile.InstanceUrl";

        private readonly IPreferences _preferences;
        private readonly ILogger<PreferencesCottonInstanceStore> _logger;

        public PreferencesCottonInstanceStore(
            IPreferences preferences,
            ILogger<PreferencesCottonInstanceStore> logger)
        {
            ArgumentNullException.ThrowIfNull(preferences);
            ArgumentNullException.ThrowIfNull(logger);

            _preferences = preferences;
            _logger = logger;
        }

        public Task<Uri?> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string value;
            try
            {
                value = _preferences.Get(InstanceUrlKey, string.Empty);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to read Cotton mobile saved instance URL.");
                return Task.FromResult<Uri?>(null);
            }

            if (string.IsNullOrWhiteSpace(value)
                || !Uri.TryCreate(value, UriKind.Absolute, out Uri? instanceUri)
                || !CottonInstanceUri.IsSupported(instanceUri))
            {
                RemoveInvalidInstanceBestEffort(value);
                return Task.FromResult<Uri?>(null);
            }

            return Task.FromResult<Uri?>(instanceUri);
        }

        public Task SaveAsync(Uri instanceUri, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));

            try
            {
                _preferences.Set(InstanceUrlKey, instanceUri.AbsoluteUri);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save Cotton mobile instance URL.");
                throw;
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _preferences.Remove(InstanceUrlKey);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to clear Cotton mobile instance URL.");
                throw;
            }

            return Task.CompletedTask;
        }

        private void RemoveInvalidInstanceBestEffort(string value)
        {
            try
            {
                _preferences.Remove(InstanceUrlKey);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to clear invalid Cotton mobile instance URL {InstanceUrl}.",
                    value);
            }
        }
    }
}
