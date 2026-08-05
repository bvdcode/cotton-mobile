using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class CloudToDeviceCoordinatorFileOperator : ICottonCloudToDeviceSyncFileOperator
    {
        public List<Guid> DownloadedIds { get; } = [];

        public List<string> DownloadedRelativePaths { get; } = [];

        public List<Guid> RenamedIds { get; } = [];

        public List<Guid> RemovedIds { get; } = [];

        public Task DownloadOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            DownloadedIds.Add(item.TargetId);
            DownloadedRelativePaths.Add(item.RelativePath);
            return Task.CompletedTask;
        }

        public Task RenameAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            RenamedIds.Add(item.TargetId);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            RemovedIds.Add(item.TargetId);
            return Task.CompletedTask;
        }
    }
}
