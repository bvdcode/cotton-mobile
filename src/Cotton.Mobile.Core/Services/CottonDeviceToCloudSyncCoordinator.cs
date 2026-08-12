// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncCoordinator
    {
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ICottonSyncRootPauseStore _pauseStore;
        private readonly ICottonUploadReceiptStore _uploadReceiptStore;
        private readonly ICottonDeviceToCloudLocalTreeReader _localTreeReader;
        private readonly CottonRecursiveRemoteContentLoader _remoteContentLoader;
        private readonly CottonUploadOnlySyncPlanExecutor _planExecutor;

        public CottonDeviceToCloudSyncCoordinator(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonUploadReceiptStore uploadReceiptStore,
            ICottonDeviceToCloudLocalTreeReader localTreeReader,
            CottonRecursiveRemoteContentLoader remoteContentLoader,
            CottonUploadOnlySyncPlanExecutor planExecutor)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(uploadReceiptStore);
            ArgumentNullException.ThrowIfNull(localTreeReader);
            ArgumentNullException.ThrowIfNull(remoteContentLoader);
            ArgumentNullException.ThrowIfNull(planExecutor);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _uploadReceiptStore = uploadReceiptStore;
            _localTreeReader = localTreeReader;
            _remoteContentLoader = remoteContentLoader;
            _planExecutor = planExecutor;
        }

        public async Task<CottonDeviceToCloudSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            IReadOnlyList<CottonSyncRootSnapshot> roots =
                await _rootStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonDeviceToCloudSyncRootRunResult> results = new List<CottonDeviceToCloudSyncRootRunResult>(roots.Count);

            foreach (CottonSyncRootSnapshot root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(await RunRootCoreAsync(instanceUri, root, pausedRootIds, cancellationToken)
                    .ConfigureAwait(false));
            }

            return new CottonDeviceToCloudSyncRunSummary(results);
        }

        public async Task<CottonDeviceToCloudSyncRunSummary> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            if (!Uri.Equals(root.InstanceUri, instanceUri))
            {
                throw new ArgumentException("Sync root belongs to a different instance.", nameof(root));
            }

            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            CottonDeviceToCloudSyncRootRunResult result =
                await RunRootCoreAsync(instanceUri, root, pausedRootIds, cancellationToken)
                    .ConfigureAwait(false);
            return new CottonDeviceToCloudSyncRunSummary([result]);
        }

        private async Task<CottonDeviceToCloudSyncRootRunResult> RunRootCoreAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            IReadOnlySet<Guid> pausedRootIds,
            CancellationToken cancellationToken)
        {
            if (root.Direction != CottonSyncDirection.DeviceToCloud)
            {
                return CottonDeviceToCloudSyncRootRunResult.SkippedUnsupportedDirection(root);
            }

            if (pausedRootIds.Contains(root.Id))
            {
                return CottonDeviceToCloudSyncRootRunResult.SkippedPaused(root);
            }

            if (CottonDeviceToCloudSyncRootCapability.HasUnsupportedLocalRoot(root))
            {
                return CottonDeviceToCloudSyncRootRunResult.SkippedUnsupportedLocalRoot(root);
            }

            if (!root.CanRunSync)
            {
                return CottonDeviceToCloudSyncRootRunResult.SkippedNotReady(root);
            }

            return await ExecuteRootAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
        }

        private async Task<CottonDeviceToCloudSyncRootRunResult> ExecuteRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            CottonDeviceToCloudLocalContentSnapshot localContent = await _localTreeReader
                .ReadAsync(instanceUri, root, cancellationToken)
                .ConfigureAwait(false);
            CottonDeviceToCloudRemoteContentSnapshot remoteContent =
                await _remoteContentLoader.LoadAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<CottonUploadReceiptSnapshot> uploadReceipts =
                await _uploadReceiptStore.LoadAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            CottonDeviceToCloudSyncPlanSnapshot plan =
                CottonDeviceToCloudSyncPlanner.Create(root, localContent, remoteContent, uploadReceipts);

            CottonDeviceToCloudSyncExecutionResult executionResult =
                await _planExecutor.ExecuteAsync(instanceUri, root, plan, cancellationToken).ConfigureAwait(false);

            return CottonDeviceToCloudSyncRootRunResult.Completed(root, plan, executionResult);
        }
    }
}
