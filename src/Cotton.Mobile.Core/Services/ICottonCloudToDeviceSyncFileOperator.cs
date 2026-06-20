namespace Cotton.Mobile.Services
{
    public interface ICottonCloudToDeviceSyncFileOperator
    {
        Task DownloadOrReplaceAsync(
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default);

        Task RenameAsync(
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default);

        Task RemoveAsync(
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default);
    }
}
