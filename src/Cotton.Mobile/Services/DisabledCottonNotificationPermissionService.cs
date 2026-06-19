namespace Cotton.Mobile.Services
{
    public sealed class DisabledCottonNotificationPermissionService : ICottonNotificationPermissionService
    {
        public Task<CottonNotificationPermissionState> GetPermissionStateAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CottonNotificationPermissionState.Unavailable);
        }

        public Task<CottonNotificationPermissionState> RequestPermissionAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CottonNotificationPermissionState.Unavailable);
        }

        public Task OpenSettingsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
