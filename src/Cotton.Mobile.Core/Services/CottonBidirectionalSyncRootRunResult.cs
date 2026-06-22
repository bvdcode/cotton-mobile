namespace Cotton.Mobile.Services
{
    public class CottonBidirectionalSyncRootRunResult
    {
        private CottonBidirectionalSyncRootRunResult(
            Guid rootId,
            Guid folderId,
            string folderName,
            CottonBidirectionalSyncRootRunStatus status,
            string statusText,
            CottonBidirectionalSyncExecutionPlan? executionPlan,
            CottonCloudToDeviceSyncExecutionResult? cloudToDeviceExecutionResult,
            CottonDeviceToCloudSyncExecutionResult? deviceToCloudExecutionResult)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Cloud folder id is required.", nameof(folderId));
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new ArgumentException("Cloud folder name is required.", nameof(folderName));
            }

            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Bidirectional sync root run status is not supported.");
            }

            if (string.IsNullOrWhiteSpace(statusText))
            {
                throw new ArgumentException("Bidirectional sync root status text is required.", nameof(statusText));
            }

            RootId = rootId;
            FolderId = folderId;
            FolderName = folderName.Trim();
            Status = status;
            StatusText = statusText.Trim();
            ExecutionPlan = executionPlan;
            CloudToDeviceExecutionResult = cloudToDeviceExecutionResult;
            DeviceToCloudExecutionResult = deviceToCloudExecutionResult;
        }

        public Guid RootId { get; }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public CottonBidirectionalSyncRootRunStatus Status { get; }

        public string StatusText { get; }

        public CottonBidirectionalSyncExecutionPlan? ExecutionPlan { get; }

        public CottonBidirectionalSyncPlanSnapshot? PreflightPlan => ExecutionPlan?.PreflightPlan;

        public CottonCloudToDeviceSyncExecutionResult? CloudToDeviceExecutionResult { get; }

        public CottonDeviceToCloudSyncExecutionResult? DeviceToCloudExecutionResult { get; }

        public bool IsCompleted => Status == CottonBidirectionalSyncRootRunStatus.Completed;

        public bool IsSkipped => !IsCompleted;

        public bool HasAppliedChanges =>
            CloudToDeviceExecutionResult?.HasAppliedChanges == true
            || DeviceToCloudExecutionResult?.HasAppliedChanges == true;

        public bool HasBlockedItems =>
            Status is CottonBidirectionalSyncRootRunStatus.SkippedConflictReviewRequired
                or CottonBidirectionalSyncRootRunStatus.SkippedBlockedReviewRequired
                or CottonBidirectionalSyncRootRunStatus.SkippedDestructiveReviewRequired
            || PreflightPlan?.HasBlockingItems == true
            || CloudToDeviceExecutionResult?.HasBlockedItems == true
            || DeviceToCloudExecutionResult?.HasBlockedItems == true;

        public static CottonBidirectionalSyncRootRunResult Completed(
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncExecutionPlan executionPlan,
            CottonCloudToDeviceSyncExecutionResult cloudToDeviceExecutionResult,
            CottonDeviceToCloudSyncExecutionResult deviceToCloudExecutionResult)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(executionPlan);
            ArgumentNullException.ThrowIfNull(cloudToDeviceExecutionResult);
            ArgumentNullException.ThrowIfNull(deviceToCloudExecutionResult);

            return Create(
                root,
                CottonBidirectionalSyncRootRunStatus.Completed,
                "Bidirectional sync root completed",
                executionPlan,
                cloudToDeviceExecutionResult,
                deviceToCloudExecutionResult);
        }

        public static CottonBidirectionalSyncRootRunResult SkippedNotReady(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);
            return Create(root, CottonBidirectionalSyncRootRunStatus.SkippedNotReady, root.StatusText);
        }

        public static CottonBidirectionalSyncRootRunResult SkippedPaused(CottonSyncRootSnapshot root)
        {
            return Create(
                root,
                CottonBidirectionalSyncRootRunStatus.SkippedPaused,
                CottonSyncRootManagementText.PausedStatusText);
        }

        public static CottonBidirectionalSyncRootRunResult SkippedUnsupportedLocalRoot(CottonSyncRootSnapshot root)
        {
            return Create(
                root,
                CottonBidirectionalSyncRootRunStatus.SkippedUnsupportedLocalRoot,
                CottonDeviceToCloudSyncRootCapability.UnsupportedLocalRootStatusText);
        }

        public static CottonBidirectionalSyncRootRunResult SkippedUnsupportedDirection(CottonSyncRootSnapshot root)
        {
            return Create(
                root,
                CottonBidirectionalSyncRootRunStatus.SkippedUnsupportedDirection,
                "Sync direction is not bidirectional");
        }

        public static CottonBidirectionalSyncRootRunResult SkippedConflictReviewRequired(
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncExecutionPlan executionPlan)
        {
            return Create(
                root,
                CottonBidirectionalSyncRootRunStatus.SkippedConflictReviewRequired,
                CottonBidirectionalSyncStatusText.ConflictReviewRequiredStatus,
                executionPlan);
        }

        public static CottonBidirectionalSyncRootRunResult SkippedBlockedReviewRequired(
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncExecutionPlan executionPlan)
        {
            return Create(
                root,
                CottonBidirectionalSyncRootRunStatus.SkippedBlockedReviewRequired,
                CottonBidirectionalSyncStatusText.BlockedReviewRequiredStatus,
                executionPlan);
        }

        public static CottonBidirectionalSyncRootRunResult SkippedDestructiveReviewRequired(
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncExecutionPlan executionPlan)
        {
            return Create(
                root,
                CottonBidirectionalSyncRootRunStatus.SkippedDestructiveReviewRequired,
                CottonBidirectionalSyncStatusText.DestructiveReviewRequiredStatus,
                executionPlan);
        }

        private static CottonBidirectionalSyncRootRunResult Create(
            CottonSyncRootSnapshot root,
            CottonBidirectionalSyncRootRunStatus status,
            string statusText,
            CottonBidirectionalSyncExecutionPlan? executionPlan = null,
            CottonCloudToDeviceSyncExecutionResult? cloudToDeviceExecutionResult = null,
            CottonDeviceToCloudSyncExecutionResult? deviceToCloudExecutionResult = null)
        {
            ArgumentNullException.ThrowIfNull(root);

            return new CottonBidirectionalSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                status,
                statusText,
                executionPlan,
                cloudToDeviceExecutionResult,
                deviceToCloudExecutionResult);
        }
    }
}
