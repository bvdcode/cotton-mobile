namespace Cotton.Mobile.Services
{
    public interface ICottonDeviceToCloudLocalTreeReader
    {
        Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default);
    }
}
