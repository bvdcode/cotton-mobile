using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class CloudToDevicePlanExecutorFileOperator : ICottonCloudToDeviceSyncFileOperator
    {
        public List<Guid> DownloadedIds { get; } = [];

        public List<Uri> DownloadedInstanceUris { get; } = [];

        public List<Guid> RenamedIds { get; } = [];

        public List<Uri> RenamedInstanceUris { get; } = [];

        public List<Guid> RemovedIds { get; } = [];

        public List<Uri> RemovedInstanceUris { get; } = [];

        public Task DownloadOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            DownloadedInstanceUris.Add(instanceUri);
            DownloadedIds.Add(item.TargetId);
            return Task.CompletedTask;
        }

        public Task RenameAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            RenamedInstanceUris.Add(instanceUri);
            RenamedIds.Add(item.TargetId);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            RemovedInstanceUris.Add(instanceUri);
            RemovedIds.Add(item.TargetId);
            return Task.CompletedTask;
        }
    }
}
