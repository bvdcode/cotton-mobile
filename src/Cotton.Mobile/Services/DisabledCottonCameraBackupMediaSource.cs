namespace Cotton.Mobile.Services
{
    public sealed class DisabledCottonCameraBackupMediaSource : ICottonCameraBackupMediaSource
    {
        public Task<IReadOnlyList<CottonCameraBackupCandidate>> ListCandidatesAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<CottonCameraBackupCandidate>>(
                Array.Empty<CottonCameraBackupCandidate>());
        }
    }
}
