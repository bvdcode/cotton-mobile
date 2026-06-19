namespace Cotton.Mobile.Services
{
    public sealed class DisabledCottonCameraBackupMediaAccessPolicy : ICottonCameraBackupMediaAccessPolicy
    {
        public Task<CottonCameraBackupMediaAccessState> GetAccessStateAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CottonCameraBackupMediaAccessState.NotRequested);
        }

        public Task<bool> CanReadMediaAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(false);
        }
    }
}
