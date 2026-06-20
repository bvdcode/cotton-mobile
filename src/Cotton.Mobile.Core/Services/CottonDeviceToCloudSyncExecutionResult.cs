namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncExecutionResult
    {
        public CottonDeviceToCloudSyncExecutionResult(
            int uploadedCount,
            int refreshedCount,
            int createdFolderCount,
            int deletedRemoteFileCount,
            int removedManifestCount,
            int skippedCount,
            int blockedCount)
        {
            if (uploadedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(uploadedCount), "Uploaded count cannot be negative.");
            }

            if (refreshedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(refreshedCount), "Refreshed count cannot be negative.");
            }

            if (createdFolderCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(createdFolderCount), "Created folder count cannot be negative.");
            }

            if (deletedRemoteFileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deletedRemoteFileCount), "Deleted remote file count cannot be negative.");
            }

            if (removedManifestCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(removedManifestCount), "Removed manifest count cannot be negative.");
            }

            if (skippedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skippedCount), "Skipped count cannot be negative.");
            }

            if (blockedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockedCount), "Blocked count cannot be negative.");
            }

            UploadedCount = uploadedCount;
            RefreshedCount = refreshedCount;
            CreatedFolderCount = createdFolderCount;
            DeletedRemoteFileCount = deletedRemoteFileCount;
            RemovedManifestCount = removedManifestCount;
            SkippedCount = skippedCount;
            BlockedCount = blockedCount;
        }

        public int UploadedCount { get; }

        public int RefreshedCount { get; }

        public int CreatedFolderCount { get; }

        public int DeletedRemoteFileCount { get; }

        public int RemovedManifestCount { get; }

        public int SkippedCount { get; }

        public int BlockedCount { get; }

        public bool HasAppliedChanges =>
            UploadedCount > 0
            || RefreshedCount > 0
            || CreatedFolderCount > 0
            || DeletedRemoteFileCount > 0
            || RemovedManifestCount > 0;

        public bool HasBlockedItems => BlockedCount > 0;
    }
}
