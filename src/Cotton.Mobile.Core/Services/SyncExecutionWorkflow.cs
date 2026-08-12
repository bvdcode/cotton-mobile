// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class SyncExecutionWorkflow(
        ICottonCloudToDeviceSyncCoordinator cloudToDeviceCoordinator,
        ICottonDeviceToCloudSyncCoordinator deviceToCloudCoordinator,
        ICottonBidirectionalSyncCoordinator bidirectionalCoordinator,
        IUserDialogService dialogService)
    {
        private readonly ICottonCloudToDeviceSyncCoordinator _cloudToDeviceCoordinator =
            cloudToDeviceCoordinator ?? throw new ArgumentNullException(nameof(cloudToDeviceCoordinator));
        private readonly ICottonDeviceToCloudSyncCoordinator _deviceToCloudCoordinator =
            deviceToCloudCoordinator ?? throw new ArgumentNullException(nameof(deviceToCloudCoordinator));
        private readonly ICottonBidirectionalSyncCoordinator _bidirectionalCoordinator =
            bidirectionalCoordinator ?? throw new ArgumentNullException(nameof(bidirectionalCoordinator));
        private readonly IUserDialogService _dialogService =
            dialogService ?? throw new ArgumentNullException(nameof(dialogService));

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

            switch (CottonSyncRootRunRouting.CreateRoute(root))
            {
                case CottonSyncRootRunRoute.CloudToDevice:
                    CottonCloudToDeviceSyncRunSummary cloudSummary =
                        await _cloudToDeviceCoordinator
                            .RunRootAsync(instanceUri, root, cancellationToken)
                            .ConfigureAwait(false);
                    return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(cloudSummary);

                case CottonSyncRootRunRoute.DeviceToCloud:
                    CottonDeviceToCloudSyncRunSummary deviceSummary =
                        await _deviceToCloudCoordinator
                            .RunRootAsync(instanceUri, root, cancellationToken)
                            .ConfigureAwait(false);
                    return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(deviceSummary);

                case CottonSyncRootRunRoute.Bidirectional:
                    CottonBidirectionalSyncRunSummary bidirectionalSummary =
                        await RunBidirectionalRootAsync(instanceUri, root, reportStatus, cancellationToken)
                            .ConfigureAwait(false);
                    return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(bidirectionalSummary);

                default:
                    throw new ArgumentOutOfRangeException(nameof(root), "Sync direction is not supported.");
            }
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

            List<CottonCloudToDeviceSyncRootRunResult> cloudResults = [];
            List<CottonDeviceToCloudSyncRootRunResult> deviceResults = [];
            List<CottonBidirectionalSyncRootRunResult> bidirectionalResults = [];
            foreach (CottonSyncRootSnapshot root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (CottonSyncRootRunRouting.CreateRoute(root))
                {
                    case CottonSyncRootRunRoute.CloudToDevice:
                        CottonCloudToDeviceSyncRunSummary cloudSummary =
                            await _cloudToDeviceCoordinator
                                .RunRootAsync(instanceUri, root, cancellationToken)
                                .ConfigureAwait(false);
                        cloudResults.AddRange(cloudSummary.RootResults);
                        break;

                    case CottonSyncRootRunRoute.DeviceToCloud:
                        CottonDeviceToCloudSyncRunSummary deviceSummary =
                            await _deviceToCloudCoordinator
                                .RunRootAsync(instanceUri, root, cancellationToken)
                                .ConfigureAwait(false);
                        deviceResults.AddRange(deviceSummary.RootResults);
                        break;

                    case CottonSyncRootRunRoute.Bidirectional:
                        CottonBidirectionalSyncRunSummary bidirectionalSummary =
                            await RunBidirectionalRootAsync(
                                    instanceUri,
                                    root,
                                    reportStatus,
                                    cancellationToken)
                                .ConfigureAwait(false);
                        bidirectionalResults.AddRange(bidirectionalSummary.RootResults);
                        break;

                    default:
                        throw new InvalidOperationException("Sync run route is not supported.");
                }
            }

            return CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                new CottonCloudToDeviceSyncRunSummary(cloudResults),
                new CottonDeviceToCloudSyncRunSummary(deviceResults),
                new CottonBidirectionalSyncRunSummary(bidirectionalResults));
        }

        private async Task<CottonBidirectionalSyncRunSummary> RunBidirectionalRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            Action<string> reportStatus,
            CancellationToken cancellationToken)
        {
            CottonBidirectionalSyncRunSummary summary =
                await _bidirectionalCoordinator
                    .RunRootAsync(instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
            if (!summary.NeedsDestructiveReview)
            {
                return summary;
            }

            cancellationToken.ThrowIfCancellationRequested();
            reportStatus(CottonBidirectionalSyncStatusText.DestructiveReviewRequiredStatus);
            bool confirmed = await _dialogService.ShowConfirmationAsync(
                    CottonBidirectionalSyncStatusText.ConfirmDestructiveTitle,
                    CottonBidirectionalSyncStatusText.CreateConfirmDestructiveMessage(
                        summary.DestructiveReviewLocalDeleteCount,
                        summary.DestructiveReviewRemoteDeleteCount),
                    CottonBidirectionalSyncStatusText.ConfirmDestructiveAction,
                    CottonSyncRootManagementText.CancelAction)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!confirmed)
            {
                return summary;
            }

            CottonBidirectionalSyncExecutionPlan reviewedPlan = GetReviewedPlan(summary);
            return await _bidirectionalCoordinator
                .ExecuteReviewedPlanAsync(instanceUri, root, reviewedPlan, cancellationToken)
                .ConfigureAwait(false);
        }

        private static CottonBidirectionalSyncExecutionPlan GetReviewedPlan(
            CottonBidirectionalSyncRunSummary summary)
        {
            if (summary.RootResults.Count != 1)
            {
                throw new InvalidOperationException("Single-root destructive review requires exactly one result.");
            }

            CottonBidirectionalSyncRootRunResult result = summary.RootResults[0];
            return result.ExecutionPlan
                ?? throw new InvalidOperationException("Destructive review result does not contain an execution plan.");
        }
    }
}
