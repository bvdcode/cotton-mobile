namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupMediaContentSource
    {
        Task<Stream?> OpenReadAsync(
            CottonCameraBackupCandidate candidate,
            CancellationToken cancellationToken = default);
    }
}
