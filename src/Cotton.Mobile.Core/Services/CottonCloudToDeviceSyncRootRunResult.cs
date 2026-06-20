namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncRootRunResult
    {
        private CottonCloudToDeviceSyncRootRunResult(
            Guid rootId,
            Guid folderId,
            string folderName,
            CottonCloudToDeviceSyncRootRunStatus status,
            string statusText,
            CottonCloudToDeviceSyncPlanSnapshot? plan,
            CottonCloudToDeviceSyncExecutionResult? executionResult)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Cloud folder id is required.", nameof(folderId));
            }

            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Sync root run status is not supported.");
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new ArgumentException("Cloud folder name is required.", nameof(folderName));
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

        public CottonCloudToDeviceSyncRootRunStatus Status { get; }

        public string StatusText { get; }

        public CottonCloudToDeviceSyncPlanSnapshot? Plan { get; }

        public CottonCloudToDeviceSyncExecutionResult? ExecutionResult { get; }

        public bool IsCompleted => Status == CottonCloudToDeviceSyncRootRunStatus.Completed;

        public bool IsSkipped => !IsCompleted;

        public bool HasAppliedChanges => ExecutionResult?.HasAppliedChanges == true;

        public bool HasBlockedItems => ExecutionResult?.HasBlockedItems == true || Plan?.HasBlockingItems == true;

        public static CottonCloudToDeviceSyncRootRunResult Completed(
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanSnapshot plan,
            CottonCloudToDeviceSyncExecutionResult executionResult)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentNullException.ThrowIfNull(executionResult);

            return new CottonCloudToDeviceSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                CottonCloudToDeviceSyncRootRunStatus.Completed,
                "Sync root completed",
                plan,
                executionResult);
        }

        public static CottonCloudToDeviceSyncRootRunResult SkippedNotReady(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return new CottonCloudToDeviceSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                CottonCloudToDeviceSyncRootRunStatus.SkippedNotReady,
                root.StatusText,
                null,
                null);
        }

        public static CottonCloudToDeviceSyncRootRunResult SkippedPaused(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return new CottonCloudToDeviceSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                CottonCloudToDeviceSyncRootRunStatus.SkippedPaused,
                CottonSyncRootManagementText.PausedStatusText,
                null,
                null);
        }

        public static CottonCloudToDeviceSyncRootRunResult SkippedUnsupportedDirection(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            return new CottonCloudToDeviceSyncRootRunResult(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                CottonCloudToDeviceSyncRootRunStatus.SkippedUnsupportedDirection,
                "Sync direction is not cloud-to-device",
                null,
                null);
        }
    }
}
