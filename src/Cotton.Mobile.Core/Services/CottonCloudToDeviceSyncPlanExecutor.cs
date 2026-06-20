namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncPlanExecutor
    {
        private readonly ICottonCloudToDeviceSyncFileOperator _fileOperator;
        private readonly ICottonSyncedFileManifestStore _manifestStore;
        private readonly TimeProvider _timeProvider;

        public CottonCloudToDeviceSyncPlanExecutor(
            ICottonCloudToDeviceSyncFileOperator fileOperator,
            ICottonSyncedFileManifestStore manifestStore,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(fileOperator);
            ArgumentNullException.ThrowIfNull(manifestStore);

            _fileOperator = fileOperator;
            _manifestStore = manifestStore;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<CottonCloudToDeviceSyncExecutionResult> ExecuteAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanSnapshot plan,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(plan);

            if (plan.SyncRootId != root.Id || plan.FolderId != root.CloudFolder.FolderId)
            {
                throw new ArgumentException("Cloud-to-device sync plan does not match the sync root.", nameof(plan));
            }

            int downloadedCount = 0;
            int refreshedCount = 0;
            int renamedCount = 0;
            int removedCount = 0;
            int skippedCount = 0;
            int blockedCount = 0;

            foreach (CottonCloudToDeviceSyncPlanItem item in plan.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (item.Action)
                {
                    case CottonCloudToDeviceSyncActionKind.DownloadNewFile:
                        await DownloadOrReplaceAsync(instanceUri, root, item, cancellationToken).ConfigureAwait(false);
                        downloadedCount++;
                        break;

                    case CottonCloudToDeviceSyncActionKind.RefreshChangedFile:
                        await DownloadOrReplaceAsync(instanceUri, root, item, cancellationToken).ConfigureAwait(false);
                        refreshedCount++;
                        break;

                    case CottonCloudToDeviceSyncActionKind.RenameLocalFile:
                        await _fileOperator.RenameAsync(instanceUri, root, item, cancellationToken).ConfigureAwait(false);
                        await SaveManifestItemAsync(instanceUri, root, item, cancellationToken).ConfigureAwait(false);
                        renamedCount++;
                        break;

                    case CottonCloudToDeviceSyncActionKind.RemoveLocalOrphan:
                        await _fileOperator.RemoveAsync(instanceUri, root, item, cancellationToken).ConfigureAwait(false);
                        await _manifestStore.RemoveAsync(instanceUri, root, item.TargetId, cancellationToken).ConfigureAwait(false);
                        removedCount++;
                        break;

                    case CottonCloudToDeviceSyncActionKind.KeepExistingFile:
                        skippedCount++;
                        break;

                    case CottonCloudToDeviceSyncActionKind.BlockedFolder:
                    case CottonCloudToDeviceSyncActionKind.NeedsFreshServerRevision:
                        blockedCount++;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(plan), "Cloud-to-device sync action is not supported.");
                }
            }

            return new CottonCloudToDeviceSyncExecutionResult(
                downloadedCount,
                refreshedCount,
                renamedCount,
                removedCount,
                skippedCount,
                blockedCount);
        }

        private async Task DownloadOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            await _fileOperator.DownloadOrReplaceAsync(instanceUri, root, item, cancellationToken).ConfigureAwait(false);
            await SaveManifestItemAsync(instanceUri, root, item, cancellationToken).ConfigureAwait(false);
        }

        private async Task SaveManifestItemAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            DateTime syncedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            await _manifestStore
                .AddOrReplaceAsync(instanceUri, root, item.CreateManifestItem(syncedAtUtc), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
