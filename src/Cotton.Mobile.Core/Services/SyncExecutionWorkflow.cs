// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class SyncExecutionWorkflow(
        ICottonDeviceToCloudSyncCoordinator deviceToCloudCoordinator)
    {
        private readonly ICottonDeviceToCloudSyncCoordinator _deviceToCloudCoordinator =
            deviceToCloudCoordinator ?? throw new ArgumentNullException(nameof(deviceToCloudCoordinator));

        public async Task<string> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            Action<string> reportStatus,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(reportStatus);
            cancellationToken.ThrowIfCancellationRequested();

            CottonDeviceToCloudSyncRunSummary summary = await _deviceToCloudCoordinator
                .RunRootAsync(instanceUri, root, cancellationToken)
                .ConfigureAwait(false);
            return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(summary);
        }

        public async Task<string> RunAllAsync(
            Uri instanceUri,
            IReadOnlyList<CottonSyncRootSnapshot> roots,
            Action<string> reportStatus,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(roots);
            ArgumentNullException.ThrowIfNull(reportStatus);

            List<CottonDeviceToCloudSyncRootRunResult> deviceResults = [];
            foreach (CottonSyncRootSnapshot root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CottonDeviceToCloudSyncRunSummary summary = await _deviceToCloudCoordinator
                    .RunRootAsync(instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
                deviceResults.AddRange(summary.RootResults);
            }

            return CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                new CottonDeviceToCloudSyncRunSummary(deviceResults));
        }
    }
}
