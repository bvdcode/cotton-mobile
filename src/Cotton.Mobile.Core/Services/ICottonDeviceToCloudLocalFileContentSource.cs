namespace Cotton.Mobile.Services
{
    public interface ICottonDeviceToCloudLocalFileContentSource
    {
        Task<Stream> OpenReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default);
    }
}
