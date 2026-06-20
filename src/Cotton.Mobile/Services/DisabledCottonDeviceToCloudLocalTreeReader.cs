namespace Cotton.Mobile.Services
{
    public class DisabledCottonDeviceToCloudLocalTreeReader : ICottonDeviceToCloudLocalTreeReader
    {
        public Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            throw new PlatformNotSupportedException("Device-to-cloud local tree reading is not available on this platform.");
        }
    }
}
