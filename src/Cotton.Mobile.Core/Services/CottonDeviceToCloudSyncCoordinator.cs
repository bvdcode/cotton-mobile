// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncCoordinator : ICottonDeviceToCloudSyncCoordinator
    {
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ICottonSyncRootPauseStore _pauseStore;
        private readonly ICottonUploadReceiptStore _uploadReceiptStore;
        private readonly ICottonDeviceToCloudLocalTreeReader _localTreeReader;
        private readonly CottonRecursiveRemoteContentLoader _remoteContentLoader;
        private readonly CottonUploadOnlySyncPlanExecutor _planExecutor;
        private readonly CottonSyncRootExecutionLock _executionLock;
        private readonly CottonSyncProgressHub _progressHub;
        private readonly ILogger<CottonDeviceToCloudSyncCoordinator> _logger;

        public CottonDeviceToCloudSyncCoordinator(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonUploadReceiptStore uploadReceiptStore,
            ICottonDeviceToCloudLocalTreeReader localTreeReader,
            CottonRecursiveRemoteContentLoader remoteContentLoader,
            CottonUploadOnlySyncPlanExecutor planExecutor,
            CottonSyncRootExecutionLock executionLock,
            CottonSyncProgressHub progressHub,
            ILogger<CottonDeviceToCloudSyncCoordinator> logger)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(uploadReceiptStore);
            ArgumentNullException.ThrowIfNull(localTreeReader);
            ArgumentNullException.ThrowIfNull(remoteContentLoader);
            ArgumentNullException.ThrowIfNull(planExecutor);
            ArgumentNullException.ThrowIfNull(executionLock);
            ArgumentNullException.ThrowIfNull(progressHub);
            ArgumentNullException.ThrowIfNull(logger);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _uploadReceiptStore = uploadReceiptStore;
            _localTreeReader = localTreeReader;
            _remoteContentLoader = remoteContentLoader;
            _planExecutor = planExecutor;
            _executionLock = executionLock;
            _progressHub = progressHub;
            _logger = logger;
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
            List<CottonDeviceToCloudSyncRootRunResult> results = new(roots.Count);

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
            if (pausedRootIds.Contains(root.Id))
            {
                CottonSyncDiagnosticLog.RootSkipped(
                    _logger,
                    root.Id,
                    CottonDeviceToCloudSyncRootRunStatus.SkippedPaused);
                return CottonDeviceToCloudSyncRootRunResult.SkippedPaused(root);
            }

            if (CottonDeviceToCloudSyncRootCapability.HasUnsupportedLocalRoot(root))
            {
                CottonSyncDiagnosticLog.RootSkipped(
                    _logger,
                    root.Id,
                    CottonDeviceToCloudSyncRootRunStatus.SkippedUnsupportedLocalRoot);
                return CottonDeviceToCloudSyncRootRunResult.SkippedUnsupportedLocalRoot(root);
            }

            if (!root.CanRunSync)
            {
                CottonSyncDiagnosticLog.RootSkipped(
                    _logger,
                    root.Id,
                    CottonDeviceToCloudSyncRootRunStatus.SkippedNotReady);
                return CottonDeviceToCloudSyncRootRunResult.SkippedNotReady(root);
            }

            CottonSyncDiagnosticLog.RootStarted(_logger, root.Id, root.LocalRoot.StorageKind);

            return await _executionLock.ExecuteAsync(
                    root,
                    token => ExecuteRootAsync(instanceUri, root, token),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<CottonDeviceToCloudSyncRootRunResult> ExecuteRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            try
            {
                _progressHub.Report(CottonSyncProgressSnapshot.ScanningDevice(root.Id));
                CottonDeviceToCloudLocalContentSnapshot localContent = await _localTreeReader
                    .ReadAsync(instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
                CottonSyncDiagnosticLog.LocalScanCompleted(
                    _logger,
                    root.Id,
                    localContent.Items.Count,
                    localContent.Problems.Count);
                _progressHub.Report(CottonSyncProgressSnapshot.CheckingCloud(root.Id));
                CottonDeviceToCloudRemoteContentSnapshot remoteContent = await _remoteContentLoader
                    .LoadAsync(instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
                CottonSyncDiagnosticLog.CloudScanCompleted(_logger, root.Id, remoteContent.Items.Count);
                IReadOnlyList<CottonUploadReceiptSnapshot> uploadReceipts = await _uploadReceiptStore
                    .LoadAsync(instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
                CottonSyncDiagnosticLog.ReceiptsLoaded(_logger, root.Id, uploadReceipts.Count);
                CottonDeviceToCloudSyncPlanSnapshot plan =
                    CottonDeviceToCloudSyncPlanner.Create(root, localContent, remoteContent, uploadReceipts);
                CottonSyncDiagnosticLog.PlanCreated(
                    _logger,
                    root.Id,
                    plan.UploadCount,
                    plan.RemoteFolderCreateCount,
                    plan.ConfirmedUploadCount,
                    plan.LocalDeleteCount,
                    plan.BlockedCount,
                    plan.NoOpCount);

                CottonDeviceToCloudSyncExecutionResult executionResult = await _planExecutor
                    .ExecuteAsync(instanceUri, root, plan, cancellationToken)
                    .ConfigureAwait(false);
                CottonSyncDiagnosticLog.ExecutionCompleted(
                    _logger,
                    root.Id,
                    executionResult.UploadedCount,
                    executionResult.ConfirmedUploadCount,
                    executionResult.CreatedFolderCount,
                    executionResult.DeletedLocalFileCount,
                    executionResult.SkippedCount,
                    executionResult.BlockedCount);
                return CottonDeviceToCloudSyncRootRunResult.Completed(root, plan, executionResult);
            }
            finally
            {
                _progressHub.Complete(root.Id);
            }
        }
    }
}
