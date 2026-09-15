using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class SessionRecoveryNotifications : ICottonSessionNotificationService
    {
        public int ClearCount { get; private set; }
        public Task ShowSignInRequiredAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void ClearSignInRequired() => ClearCount++;
    }
}
