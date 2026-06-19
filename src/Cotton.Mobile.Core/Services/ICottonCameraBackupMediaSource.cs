namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupMediaSource
    {
        Task<IReadOnlyList<CottonCameraBackupCandidate>> ListCandidatesAsync(
            CancellationToken cancellationToken = default);
    }
}
