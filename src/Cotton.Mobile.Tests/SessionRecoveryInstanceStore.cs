using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class SessionRecoveryInstanceStore(Uri instance) : ICottonInstanceStore
    {
        public Uri? Instance { get; private set; } = instance;
        public Task<Uri?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Instance);
        public Task SaveAsync(Uri instanceUri, CancellationToken cancellationToken = default)
        {
            Instance = instanceUri;
            return Task.CompletedTask;
        }
        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Instance = null;
            return Task.CompletedTask;
        }
    }
}
