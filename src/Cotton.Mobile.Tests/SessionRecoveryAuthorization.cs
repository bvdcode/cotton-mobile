using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class SessionRecoveryAuthorization : ICottonPendingAppCodeSessionStore, ICottonAppCodeAuthorizationService
    {
        public int ClearCount { get; private set; }
        public Task<CottonPendingAppCodeSession?> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<CottonPendingAppCodeSession?>(null);
        public Task SaveAsync(CottonPendingAppCodeSession session, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            ClearCount++;
            return Task.CompletedTask;
        }
        public Task<CottonSessionResult> SignInAsync(Uri instanceUri, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<CottonSessionResult> RestorePendingAsync(Uri instanceUri, CancellationToken cancellationToken) =>
            Task.FromResult(CottonSessionResult.Unauthenticated(instanceUri));
        public Task ClearPendingBestEffortAsync(string reason) => Task.CompletedTask;
    }
}
