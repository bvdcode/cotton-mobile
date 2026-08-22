// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class SyncExecutionWorkflow(
        ICottonDeviceToCloudSyncCoordinator deviceToCloudCoordinator,
        ILogger<SyncExecutionWorkflow> logger)
    {
        private readonly ICottonDeviceToCloudSyncCoordinator _deviceToCloudCoordinator =
            deviceToCloudCoordinator ?? throw new ArgumentNullException(nameof(deviceToCloudCoordinator));
        private readonly ILogger<SyncExecutionWorkflow> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

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
                CottonDeviceToCloudSyncRunSummary summary = await _deviceToCloudCoordinator
                    .RunRootAsync(instanceUri, root, cancellationToken)
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
                foreach (CottonSyncRootSnapshot root in roots)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CottonDeviceToCloudSyncRunSummary summary = await _deviceToCloudCoordinator
                        .RunRootAsync(instanceUri, root, cancellationToken)
                        .ConfigureAwait(false);
                    deviceResults.AddRange(summary.RootResults);
                }

                CottonSyncDiagnosticLog.ManualAllCompleted(_logger, deviceResults.Count);
                return CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                    new CottonDeviceToCloudSyncRunSummary(deviceResults));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonSyncDiagnosticLog.ManualAllFailed(_logger, exception);
                throw;
            }
        }
    }
}
