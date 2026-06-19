namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupTransferEnqueueCoordinator
    {
        Task<CottonCameraBackupTransferEnqueueResult> EnqueueAsync(
            Uri instanceUri,
            CottonCameraBackupSettings settings,
            CancellationToken cancellationToken = default);
    }
}
