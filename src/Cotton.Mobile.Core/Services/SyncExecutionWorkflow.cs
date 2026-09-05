// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class SyncExecutionWorkflow(
        ICottonDeviceToCloudSyncCoordinator deviceToCloudCoordinator,
        ICottonSyncRootStore rootStore,
        ICottonAutomaticSyncStatusStore statusStore,
        TimeProvider timeProvider,
        ILogger<SyncExecutionWorkflow> logger)
    {
        private readonly ICottonDeviceToCloudSyncCoordinator _deviceToCloudCoordinator =
            deviceToCloudCoordinator ?? throw new ArgumentNullException(nameof(deviceToCloudCoordinator));
        private readonly ILogger<SyncExecutionWorkflow> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ICottonSyncRootStore _rootStore =
            rootStore ?? throw new ArgumentNullException(nameof(rootStore));
        private readonly ICottonAutomaticSyncStatusStore _statusStore =
            statusStore ?? throw new ArgumentNullException(nameof(statusStore));
        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        public async Task<CottonDeviceToCloudSyncRunSummary> RunWithStatusAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            cancellationToken.ThrowIfCancellationRequested();

            CottonDeviceToCloudSyncRunSummary summary;
            try
            {
                summary = await _deviceToCloudCoordinator
                    .RunRootAsync(instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                CottonAutomaticSyncLog.RootFailed(_logger, root.Id, exception);
                await SaveStatusAsync(
                    instanceUri,
                    CottonAutomaticSyncRootStatusSnapshot.Failed(
                        root.Id,
                        _timeProvider.GetUtcNow().UtcDateTime,
                        CottonAutomaticSyncFailureClassifier.Classify(exception)),
                    cancellationToken).ConfigureAwait(false);
                throw;
            }

            if (summary.CompletedRootCount > 0)
            {
                DateTime completedAt = _timeProvider.GetUtcNow().UtcDateTime;
                CottonAutomaticSyncRootStatusSnapshot status = summary.HasBlockedItems
                    ? CottonAutomaticSyncRootStatusSnapshot.Failed(
                        root.Id, completedAt, CottonAutomaticSyncFailureClassifier.ClassifyBlocked(summary))
                    : CottonAutomaticSyncRootStatusSnapshot.Succeeded(root.Id, completedAt);
                await SaveStatusAsync(instanceUri, status, cancellationToken).ConfigureAwait(false);
            }

            return summary;
        }

        private async Task SaveStatusAsync(
            Uri instanceUri,
            CottonAutomaticSyncRootStatusSnapshot status,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore
                .LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            HashSet<Guid> activeRootIds = [.. roots.Select(root => root.Id)];
            IReadOnlyCollection<CottonAutomaticSyncRootStatusSnapshot> updates =
                activeRootIds.Contains(status.RootId) ? [status] : [];
            await _statusStore.UpdateAsync(instanceUri, activeRootIds, updates, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<string> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            cancellationToken.ThrowIfCancellationRequested();

            CottonSyncDiagnosticLog.ManualRootStarted(_logger, root.Id);
            try
            {
                CottonDeviceToCloudSyncRunSummary summary = await RunWithStatusAsync(
                        instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
                CottonSyncDiagnosticLog.ManualRootCompleted(
                    _logger,
                    root.Id,
                    summary.RootCount,
                    summary.CompletedRootCount);
                return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(summary);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonSyncDiagnosticLog.ManualRootFailed(_logger, root.Id, exception);
                throw;
            }
        }

        public async Task<string> RunAllAsync(
            Uri instanceUri,
            IReadOnlyList<CottonSyncRootSnapshot> roots,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(roots);

            CottonSyncDiagnosticLog.ManualAllStarted(_logger, roots.Count);
            try
            {
                List<CottonDeviceToCloudSyncRootRunResult> deviceResults = [];
                int failedRootCount = 0;
                foreach (CottonSyncRootSnapshot root in roots)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        CottonDeviceToCloudSyncRunSummary summary = await RunWithStatusAsync(
                                instanceUri, root, cancellationToken)
                            .ConfigureAwait(false);
                        deviceResults.AddRange(summary.RootResults);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        CottonSyncDiagnosticLog.ManualRootFailed(_logger, root.Id, exception);
                        failedRootCount++;
                    }
                }

                CottonSyncDiagnosticLog.ManualAllCompleted(_logger, deviceResults.Count);
                return CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                    new CottonDeviceToCloudSyncRunSummary(deviceResults), failedRootCount);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonSyncDiagnosticLog.ManualAllFailed(_logger, exception);
                throw;
            }
        }
    }
}
