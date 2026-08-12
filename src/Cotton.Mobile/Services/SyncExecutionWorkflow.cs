// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class SyncExecutionWorkflow
    {
        private readonly CottonCloudToDeviceSyncCoordinator _cloudToDeviceCoordinator;
        private readonly CottonDeviceToCloudSyncCoordinator _deviceToCloudCoordinator;
        private readonly CottonBidirectionalSyncCoordinator _bidirectionalCoordinator;
        private readonly IUserDialogService _dialogService;

        public SyncExecutionWorkflow(
            CottonCloudToDeviceSyncCoordinator cloudToDeviceCoordinator,
            CottonDeviceToCloudSyncCoordinator deviceToCloudCoordinator,
            CottonBidirectionalSyncCoordinator bidirectionalCoordinator,
            IUserDialogService dialogService)
        {
            ArgumentNullException.ThrowIfNull(cloudToDeviceCoordinator);
            ArgumentNullException.ThrowIfNull(deviceToCloudCoordinator);
            ArgumentNullException.ThrowIfNull(bidirectionalCoordinator);
            ArgumentNullException.ThrowIfNull(dialogService);

            _cloudToDeviceCoordinator = cloudToDeviceCoordinator;
            _deviceToCloudCoordinator = deviceToCloudCoordinator;
            _bidirectionalCoordinator = bidirectionalCoordinator;
            _dialogService = dialogService;
        }

        public async Task<string> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            Action<string> reportStatus)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(reportStatus);

            switch (CottonSyncRootRunRouting.CreateRoute(root))
            {
                case CottonSyncRootRunRoute.CloudToDevice:
                    CottonCloudToDeviceSyncRunSummary cloudSummary =
                        await _cloudToDeviceCoordinator.RunRootAsync(instanceUri, root);
                    return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(cloudSummary);

                case CottonSyncRootRunRoute.DeviceToCloud:
                    CottonDeviceToCloudSyncRunSummary deviceSummary =
                        await _deviceToCloudCoordinator.RunRootAsync(instanceUri, root);
                    return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(deviceSummary);

                case CottonSyncRootRunRoute.Bidirectional:
                    CottonBidirectionalSyncRunSummary bidirectionalSummary =
                        await RunBidirectionalRootAsync(instanceUri, root, reportStatus);
                    return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(bidirectionalSummary);

                default:
                    throw new ArgumentOutOfRangeException(nameof(root), "Sync direction is not supported.");
            }
        }

        public async Task<string> RunAllAsync(
            Uri instanceUri,
            IReadOnlyList<CottonSyncRootSnapshot> roots,
            Action<string> reportStatus)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(roots);
            ArgumentNullException.ThrowIfNull(reportStatus);

            List<CottonCloudToDeviceSyncRootRunResult> cloudResults = [];
            List<CottonDeviceToCloudSyncRootRunResult> deviceResults = [];
            List<CottonBidirectionalSyncRootRunResult> bidirectionalResults = [];
            foreach (CottonSyncRootSnapshot root in roots)
            {
                switch (CottonSyncRootRunRouting.CreateRoute(root))
                {
                    case CottonSyncRootRunRoute.CloudToDevice:
                        CottonCloudToDeviceSyncRunSummary cloudSummary =
                            await _cloudToDeviceCoordinator.RunRootAsync(instanceUri, root);
                        cloudResults.AddRange(cloudSummary.RootResults);
                        break;

                    case CottonSyncRootRunRoute.DeviceToCloud:
                        CottonDeviceToCloudSyncRunSummary deviceSummary =
                            await _deviceToCloudCoordinator.RunRootAsync(instanceUri, root);
                        deviceResults.AddRange(deviceSummary.RootResults);
                        break;

                    case CottonSyncRootRunRoute.Bidirectional:
                        CottonBidirectionalSyncRunSummary bidirectionalSummary =
                            await RunBidirectionalRootAsync(instanceUri, root, reportStatus);
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
            Action<string> reportStatus)
        {
            CottonBidirectionalSyncRunSummary summary =
                await _bidirectionalCoordinator.RunRootAsync(instanceUri, root);
            if (!summary.NeedsDestructiveReview)
            {
                return summary;
            }

            reportStatus(CottonBidirectionalSyncStatusText.DestructiveReviewRequiredStatus);
            bool confirmed = await _dialogService.ShowConfirmationAsync(
                CottonBidirectionalSyncStatusText.ConfirmDestructiveTitle,
                CottonBidirectionalSyncStatusText.CreateConfirmDestructiveMessage(
                    summary.DestructiveReviewLocalDeleteCount,
                    summary.DestructiveReviewRemoteDeleteCount),
                CottonBidirectionalSyncStatusText.ConfirmDestructiveAction,
                CottonSyncRootManagementText.CancelAction);
            if (!confirmed)
            {
                return summary;
            }

            CottonBidirectionalSyncExecutionPlan reviewedPlan = GetReviewedPlan(summary);
            return await _bidirectionalCoordinator.ExecuteReviewedPlanAsync(instanceUri, root, reviewedPlan);
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
