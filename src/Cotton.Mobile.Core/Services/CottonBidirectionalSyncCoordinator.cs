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
        private readonly CottonRecursiveRemoteContentLoader _remoteContentLoader;
        private readonly CottonCloudToDeviceSyncPlanExecutor _cloudToDevicePlanExecutor;
        private readonly CottonDeviceToCloudSyncPlanExecutor _deviceToCloudPlanExecutor;

        public CottonBidirectionalSyncCoordinator(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonSyncedFileManifestStore manifestStore,
            ICottonDeviceToCloudLocalTreeReader localTreeReader,
            CottonRecursiveRemoteContentLoader remoteContentLoader,
            CottonCloudToDeviceSyncPlanExecutor cloudToDevicePlanExecutor,
            CottonDeviceToCloudSyncPlanExecutor deviceToCloudPlanExecutor)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(manifestStore);
            ArgumentNullException.ThrowIfNull(localTreeReader);
            ArgumentNullException.ThrowIfNull(remoteContentLoader);
            ArgumentNullException.ThrowIfNull(cloudToDevicePlanExecutor);
            ArgumentNullException.ThrowIfNull(deviceToCloudPlanExecutor);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _manifestStore = manifestStore;
            _localTreeReader = localTreeReader;
            _remoteContentLoader = remoteContentLoader;
            _cloudToDevicePlanExecutor = cloudToDevicePlanExecutor;
            _deviceToCloudPlanExecutor = deviceToCloudPlanExecutor;
        }

        public async Task<CottonBidirectionalSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            IReadOnlyList<CottonSyncRootSnapshot> roots =
                await _rootStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonBidirectionalSyncRootRunResult> results = new(roots.Count);

            foreach (CottonSyncRootSnapshot root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(await RunRootCoreAsync(instanceUri, root, pausedRootIds, cancellationToken)
                    .ConfigureAwait(false));
            }

            return new CottonBidirectionalSyncRunSummary(results);
        }

        public async Task<CottonBidirectionalSyncRunSummary> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ValidateRoot(instanceUri, root);

            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            CottonBidirectionalSyncRootRunResult result =
                await RunRootCoreAsync(instanceUri, root, pausedRootIds, cancellationToken)
                    .ConfigureAwait(false);
            return new CottonBidirectionalSyncRunSummary([result]);
        }

        public async Task<CottonBidirectionalSyncRunSummary> ExecuteReviewedPlanAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncExecutionPlan reviewedPlan,
            CancellationToken cancellationToken = default)
        {
            ValidateRoot(instanceUri, root);
            ArgumentNullException.ThrowIfNull(reviewedPlan);
            ValidateReviewedPlan(root, reviewedPlan);

            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            CottonBidirectionalSyncRootRunResult? skippedResult = CreateSkippedResult(root, pausedRootIds);
            if (skippedResult is not null)
            {
                return new CottonBidirectionalSyncRunSummary([skippedResult]);
            }

            CottonBidirectionalSyncRootRunResult result =
                await ExecutePlanAsync(instanceUri, root, reviewedPlan, cancellationToken).ConfigureAwait(false);
            return new CottonBidirectionalSyncRunSummary([result]);
        }

        private async Task<CottonBidirectionalSyncRootRunResult> RunRootCoreAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            IReadOnlySet<Guid> pausedRootIds,
            CancellationToken cancellationToken)
        {
            CottonBidirectionalSyncRootRunResult? skippedResult = CreateSkippedResult(root, pausedRootIds);
            if (skippedResult is not null)
            {
                return skippedResult;
            }

            CottonBidirectionalSyncExecutionPlan executionPlan =
                await CreateExecutionPlanAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            if (!executionPlan.CanExecute)
            {
                return executionPlan.PreflightPlan.ConflictCount > 0
                    ? CottonBidirectionalSyncRootRunResult.SkippedConflictReviewRequired(root, executionPlan)
                    : CottonBidirectionalSyncRootRunResult.SkippedBlockedReviewRequired(root, executionPlan);
            }

            if (executionPlan.HasDestructiveChanges)
            {
                return CottonBidirectionalSyncRootRunResult.SkippedDestructiveReviewRequired(root, executionPlan);
            }

            return await ExecutePlanAsync(instanceUri, root, executionPlan, cancellationToken).ConfigureAwait(false);
        }

        private async Task<CottonBidirectionalSyncRootRunResult> ExecutePlanAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncExecutionPlan executionPlan,
            CancellationToken cancellationToken)
        {
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

        private static CottonBidirectionalSyncRootRunResult? CreateSkippedResult(
            CottonSyncRootSnapshot root,
            IReadOnlySet<Guid> pausedRootIds)
        {
            if (root.Direction != CottonSyncDirection.Bidirectional)
            {
                return CottonBidirectionalSyncRootRunResult.SkippedUnsupportedDirection(root);
            }

            if (pausedRootIds.Contains(root.Id))
            {
                return CottonBidirectionalSyncRootRunResult.SkippedPaused(root);
            }

            if (CottonDeviceToCloudSyncRootCapability.CanRun(root))
            {
                return null;
            }

            return !root.CanRunSync
                ? CottonBidirectionalSyncRootRunResult.SkippedNotReady(root)
                : CottonBidirectionalSyncRootRunResult.SkippedUnsupportedLocalRoot(root);
        }

        private static void ValidateRoot(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            if (!Uri.Equals(root.InstanceUri, instanceUri))
            {
                throw new ArgumentException("Sync root belongs to a different instance.", nameof(root));
            }
        }

        private static void ValidateReviewedPlan(
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncExecutionPlan reviewedPlan)
        {
            if (reviewedPlan.PreflightPlan.SyncRootId != root.Id
                || reviewedPlan.PreflightPlan.FolderId != root.CloudFolder.FolderId)
            {
                throw new ArgumentException("Reviewed plan belongs to a different sync root.", nameof(reviewedPlan));
            }

            if (!reviewedPlan.CanExecute || !reviewedPlan.HasDestructiveChanges)
            {
                throw new ArgumentException("Reviewed plan must contain executable destructive changes.", nameof(reviewedPlan));
            }
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
                await _remoteContentLoader.LoadAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<CottonSyncedFileSnapshot> manifestFiles =
                await _manifestStore.LoadAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            CottonBidirectionalSyncPlanSnapshot preflightPlan =
                CottonBidirectionalSyncPlanner.Create(root, localContent, remoteContent, manifestFiles);

            return CottonBidirectionalSyncExecutionPlanner.Create(preflightPlan);
        }
    }
}
