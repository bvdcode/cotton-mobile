using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesCottonInstanceStore : ICottonInstanceStore
    {
        private const string InstanceUrlKey = "Cotton.Mobile.InstanceUrl";

        private readonly IPreferences _preferences;

        public PreferencesCottonInstanceStore(IPreferences preferences)
        {
            ArgumentNullException.ThrowIfNull(preferences);
            _preferences = preferences;
        }

        public Task<Uri?> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string value = _preferences.Get(InstanceUrlKey, string.Empty);
            if (string.IsNullOrWhiteSpace(value)
                || !Uri.TryCreate(value, UriKind.Absolute, out Uri? instanceUri)
                || !CottonInstanceUri.IsSupported(instanceUri))
            {
                _preferences.Remove(InstanceUrlKey);
                return Task.FromResult<Uri?>(null);
            }

            return Task.FromResult<Uri?>(instanceUri);
        }

        public Task SaveAsync(Uri instanceUri, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));

            _preferences.Set(InstanceUrlKey, instanceUri.AbsoluteUri);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _preferences.Remove(InstanceUrlKey);
            return Task.CompletedTask;
        }
    }
}
