namespace Cotton.Mobile.Services
{
    public class DisabledCottonDeviceToCloudLocalFileContentSource :
        ICottonDeviceToCloudLocalFileContentSource
    {
        public Task<Stream> OpenReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new PlatformNotSupportedException("Device-to-cloud local file reading is not available on this platform.");
        }
    }
}
