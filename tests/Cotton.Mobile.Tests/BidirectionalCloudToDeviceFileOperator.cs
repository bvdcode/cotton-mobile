using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class BidirectionalCloudToDeviceFileOperator : ICottonCloudToDeviceSyncFileOperator
    {
        public List<string> DownloadedRelativePaths { get; } = [];

        public Task DownloadOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            DownloadedRelativePaths.Add(item.RelativePath);
            return Task.CompletedTask;
        }

        public Task RenameAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Rename is not used by these tests.");
        }

        public Task RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Local remove is not used by these tests.");
        }
    }
}
