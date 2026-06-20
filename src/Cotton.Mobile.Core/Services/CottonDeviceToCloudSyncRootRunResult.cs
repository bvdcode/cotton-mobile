namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncRootRunResult
    {
        private CottonDeviceToCloudSyncRootRunResult(
            Guid rootId,
            Guid folderId,
            string folderName,
            CottonDeviceToCloudSyncRootRunStatus status,
            string statusText,
            CottonDeviceToCloudSyncPlanSnapshot? plan,
            CottonDeviceToCloudSyncExecutionResult? executionResult)
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
                throw new ArgumentOutOfRangeException(nameof(status), "Sync root run status is not supported.");
            }

            if (string.IsNullOrWhiteSpace(statusText))
            {
                throw new ArgumentException("Sync root status text is required.", nameof(statusText));
            }

            RootId = rootId;
            FolderId = folderId;
            FolderName = folderName.Trim();
            Status = status;
            StatusText = statusText.Trim();
            Plan = plan;
            ExecutionResult = executionResult;
        }

        public Guid RootId { get; }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public CottonDeviceToCloudSyncRootRunStatus Status { get; }

        public string StatusText { get; }

        public CottonDeviceToCloudSyncPlanSnapshot? Plan { get; }

        public CottonDeviceToCloudSyncExecutionResult? ExecutionResult { get; }

        public bool IsCompleted => Status == CottonDeviceToCloudSyncRootRunStatus.Completed;

        public bool IsSkipped => !IsCompleted;

        public bool HasAppliedChanges => ExecutionResult?.HasAppliedChanges == true;

        public bool HasBlockedItems =>
            ExecutionResult?.HasBlockedItems == true
            || Plan?.HasBlockingItems == true
            || Status == CottonDeviceToCloudSyncRootRunStatus.SkippedDestructiveReviewRequired;

        public static CottonDeviceToCloudSyncRootRunResult Completed(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanSnapshot plan,
            CottonDeviceToCloudSyncExecutionResult executionResult)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentNullException.ThrowIfNull(executionResult);

            return new CottonDeviceToCloudSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                CottonDeviceToCloudSyncRootRunStatus.Completed,
                "Sync root completed",
                plan,
                executionResult);
        }

        public static CottonDeviceToCloudSyncRootRunResult SkippedNotReady(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return new CottonDeviceToCloudSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                CottonDeviceToCloudSyncRootRunStatus.SkippedNotReady,
                root.StatusText,
                null,
                null);
        }

        public static CottonDeviceToCloudSyncRootRunResult SkippedPaused(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return new CottonDeviceToCloudSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                CottonDeviceToCloudSyncRootRunStatus.SkippedPaused,
                CottonSyncRootManagementText.PausedStatusText,
                null,
                null);
        }

        public static CottonDeviceToCloudSyncRootRunResult SkippedUnsupportedLocalRoot(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return new CottonDeviceToCloudSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                CottonDeviceToCloudSyncRootRunStatus.SkippedUnsupportedLocalRoot,
                CottonDeviceToCloudSyncRootCapability.UnsupportedLocalRootStatusText,
                null,
                null);
        }

        public static CottonDeviceToCloudSyncRootRunResult SkippedUnsupportedDirection(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return new CottonDeviceToCloudSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                CottonDeviceToCloudSyncRootRunStatus.SkippedUnsupportedDirection,
                "Sync direction is not device-to-cloud",
                null,
                null);
        }

        public static CottonDeviceToCloudSyncRootRunResult SkippedDestructiveReviewRequired(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanSnapshot plan)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(plan);

            return new CottonDeviceToCloudSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                CottonDeviceToCloudSyncRootRunStatus.SkippedDestructiveReviewRequired,
                CottonDeviceToCloudSyncStatusText.DestructiveReviewRequiredStatus,
                plan,
                null);
        }
    }
}
