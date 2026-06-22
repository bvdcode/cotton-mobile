// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonBidirectionalSyncCoordinator
    {
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ICottonSyncRootPauseStore _pauseStore;
        private readonly ICottonSyncedFileManifestStore _manifestStore;
        private readonly ICottonDeviceToCloudLocalTreeReader _localTreeReader;
        private readonly ICottonDeviceToCloudRemoteFolderContentSource _remoteFolderContentSource;
        private readonly CottonCloudToDeviceSyncPlanExecutor _cloudToDevicePlanExecutor;
        private readonly CottonDeviceToCloudSyncPlanExecutor _deviceToCloudPlanExecutor;

        public CottonBidirectionalSyncCoordinator(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonSyncedFileManifestStore manifestStore,
            ICottonDeviceToCloudLocalTreeReader localTreeReader,
            ICottonDeviceToCloudRemoteFolderContentSource remoteFolderContentSource,
            CottonCloudToDeviceSyncPlanExecutor cloudToDevicePlanExecutor,
            CottonDeviceToCloudSyncPlanExecutor deviceToCloudPlanExecutor)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(manifestStore);
            ArgumentNullException.ThrowIfNull(localTreeReader);
            ArgumentNullException.ThrowIfNull(remoteFolderContentSource);
            ArgumentNullException.ThrowIfNull(cloudToDevicePlanExecutor);
            ArgumentNullException.ThrowIfNull(deviceToCloudPlanExecutor);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _manifestStore = manifestStore;
            _localTreeReader = localTreeReader;
            _remoteFolderContentSource = remoteFolderContentSource;
            _cloudToDevicePlanExecutor = cloudToDevicePlanExecutor;
            _deviceToCloudPlanExecutor = deviceToCloudPlanExecutor;
        }

        public async Task<CottonBidirectionalSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            return await RunAsync(instanceUri, CottonBidirectionalSyncRunOptions.Default, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CottonBidirectionalSyncRunSummary> RunAsync(
            Uri instanceUri,
            CottonBidirectionalSyncRunOptions options,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(options);

            IReadOnlyList<CottonSyncRootSnapshot> roots =
                await _rootStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            var results = new List<CottonBidirectionalSyncRootRunResult>(roots.Count);

            foreach (CottonSyncRootSnapshot root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(await RunRootCoreAsync(instanceUri, root, pausedRootIds, options, cancellationToken)
                    .ConfigureAwait(false));
            }

            return new CottonBidirectionalSyncRunSummary(results);
        }

        public async Task<CottonBidirectionalSyncRunSummary> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            return await RunRootAsync(instanceUri, root, CottonBidirectionalSyncRunOptions.Default, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CottonBidirectionalSyncRunSummary> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncRunOptions options,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(options);
            if (!Uri.Equals(root.InstanceUri, instanceUri))
            {
                throw new ArgumentException("Sync root belongs to a different instance.", nameof(root));
            }

            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            CottonBidirectionalSyncRootRunResult result =
                await RunRootCoreAsync(instanceUri, root, pausedRootIds, options, cancellationToken)
                    .ConfigureAwait(false);
            return new CottonBidirectionalSyncRunSummary([result]);
        }

        private async Task<CottonBidirectionalSyncRootRunResult> RunRootCoreAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            IReadOnlySet<Guid> pausedRootIds,
            CottonBidirectionalSyncRunOptions options,
            CancellationToken cancellationToken)
        {
            if (root.Direction != CottonSyncDirection.Bidirectional)
            {
                return CottonBidirectionalSyncRootRunResult.SkippedUnsupportedDirection(root);
            }

            if (pausedRootIds.Contains(root.Id))
            {
                return CottonBidirectionalSyncRootRunResult.SkippedPaused(root);
            }

            if (!CottonDeviceToCloudSyncRootCapability.CanRun(root))
            {
                return !root.CanRunSync
                    ? CottonBidirectionalSyncRootRunResult.SkippedNotReady(root)
                    : CottonBidirectionalSyncRootRunResult.SkippedUnsupportedLocalRoot(root);
            }

            CottonBidirectionalSyncExecutionPlan executionPlan =
                await CreateExecutionPlanAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            if (!executionPlan.CanExecute)
            {
                return executionPlan.PreflightPlan.ConflictCount > 0
                    ? CottonBidirectionalSyncRootRunResult.SkippedConflictReviewRequired(root, executionPlan)
                    : CottonBidirectionalSyncRootRunResult.SkippedBlockedReviewRequired(root, executionPlan);
            }

            if (executionPlan.HasDestructiveChanges && !options.AllowDestructiveChanges)
            {
                return CottonBidirectionalSyncRootRunResult.SkippedDestructiveReviewRequired(root, executionPlan);
            }

            CottonCloudToDeviceSyncExecutionResult cloudToDeviceResult =
                await _cloudToDevicePlanExecutor
                    .ExecuteAsync(instanceUri, root, executionPlan.CloudToDevicePlan, cancellationToken)
                    .ConfigureAwait(false);
            CottonDeviceToCloudSyncExecutionResult deviceToCloudResult =
                await _deviceToCloudPlanExecutor
                    .ExecuteAsync(instanceUri, root, executionPlan.DeviceToCloudPlan, cancellationToken)
                    .ConfigureAwait(false);

            return CottonBidirectionalSyncRootRunResult.Completed(
                root,
                executionPlan,
                cloudToDeviceResult,
                deviceToCloudResult);
        }

        private async Task<CottonBidirectionalSyncExecutionPlan> CreateExecutionPlanAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            CottonDeviceToCloudLocalContentSnapshot localContent = await _localTreeReader
                .ReadAsync(instanceUri, root, cancellationToken)
                .ConfigureAwait(false);
            CottonDeviceToCloudRemoteContentSnapshot remoteContent =
                await LoadRecursiveRemoteContentAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<CottonSyncedFileSnapshot> manifestFiles =
                await _manifestStore.LoadAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            CottonBidirectionalSyncPlanSnapshot preflightPlan =
                CottonBidirectionalSyncPlanner.Create(root, localContent, remoteContent, manifestFiles);

            return CottonBidirectionalSyncExecutionPlanner.Create(preflightPlan);
        }

        private async Task<CottonDeviceToCloudRemoteContentSnapshot> LoadRecursiveRemoteContentAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            var items = new List<CottonDeviceToCloudRemoteItemSnapshot>();
            var folders = new Queue<(CottonFolderHandle Folder, string RelativePath)>();
            var visitedFolderIds = new HashSet<Guid>();

            folders.Enqueue((root.CloudFolder.ToFolderHandle(), string.Empty));
            while (folders.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                (CottonFolderHandle folder, string folderRelativePath) = folders.Dequeue();
                if (!visitedFolderIds.Add(folder.Id))
                {
                    continue;
                }

                CottonFolderContent content = await _remoteFolderContentSource
                    .LoadAsync(instanceUri, folder, cancellationToken)
                    .ConfigureAwait(false);
                foreach (CottonFileBrowserEntry entry in content.Entries)
                {
                    string relativePath = entry.Type == CottonFileBrowserEntryType.Folder
                        ? CottonSyncRelativePath.CreateChildFolderPath(folderRelativePath, entry.Name)
                        : CottonSyncRelativePath.CreateFilePath(folderRelativePath, entry.Name);
                    items.Add(new CottonDeviceToCloudRemoteItemSnapshot(entry, relativePath));
                    if (entry.Type == CottonFileBrowserEntryType.Folder)
                    {
                        folders.Enqueue((new CottonFolderHandle(entry.Id, entry.Name), relativePath));
                    }
                }
            }

            return new CottonDeviceToCloudRemoteContentSnapshot(
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                items);
        }
    }
}
