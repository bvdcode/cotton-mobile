namespace Cotton.Mobile.Services
{
    public static class CottonDeviceToCloudSyncStatusText
    {
        private const string DefaultFolderName = "Folder";

        public const string ActionLabel = "Sync from folder";

        public static string AccountUnavailableStatus { get; } =
            CottonCloudToDeviceSyncStatusText.AccountUnavailableStatus;

        public static string OfflineUnavailableStatus { get; } =
            CottonCloudToDeviceSyncStatusText.OfflineUnavailableStatus;

        public static string CancelledStatus { get; } =
            CottonCloudToDeviceSyncStatusText.CancelledStatus;

        public static string FailedStatus { get; } =
            CottonCloudToDeviceSyncStatusText.FailedStatus;

        public static string DirectionConflictStatus { get; } =
            "This local folder already syncs from cloud. Stop that sync first.";

        public static string DestructiveReviewRequiredStatus { get; } =
            "Sync needs review before removing cloud files.";

        public static string ConfirmDestructiveTitle { get; } = "Sync from device folder?";

        public static string ConfirmDestructiveMessage { get; } =
            "Files removed from the selected device folder may be moved to trash in Cotton Cloud for this sync root.";

        public static string ConfirmDestructiveAction { get; } = "Sync";

        public static string ConfirmRemoteDeleteTitle { get; } = "Move cloud files to trash?";

        public static string ConfirmRemoteDeleteAction { get; } = "Move to trash";

        public static string CreateStartingStatus(string folderName)
        {
            return $"Syncing {NormalizeFolderName(folderName)}...";
        }

        public static string CreateConfirmRemoteDeleteMessage(int fileCount)
        {
            if (fileCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileCount), "Remote delete count must be positive.");
            }

            if (fileCount == 1)
            {
                return "This sync will move 1 cloud file to trash because it is missing from the selected device folder.";
            }

            return $"This sync will move {fileCount} cloud files to trash because they are missing from the selected device folder.";
        }

        public static string CreateCompletedStatus(CottonDeviceToCloudSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            return CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                new CottonCloudToDeviceSyncRunSummary([]),
                summary);
        }

        private static string NormalizeFolderName(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName) ? DefaultFolderName : folderName.Trim();
        }
    }
}
