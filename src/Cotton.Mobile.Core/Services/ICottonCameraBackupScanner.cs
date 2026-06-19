namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupScanner
    {
        Task<CottonCameraBackupScanResult> ScanAsync(
            CottonCameraBackupSettings settings,
            IReadOnlyCollection<CottonCameraBackupUploadedMediaSnapshot> uploadedMedia,
            CancellationToken cancellationToken = default);
    }
}
