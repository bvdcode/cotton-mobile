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

        public static string ConfirmDestructiveTitle { get; } = "Sync from device folder?";

        public static string ConfirmDestructiveMessage { get; } =
            "Files removed from the selected device folder may be moved to trash in Cotton Cloud for this sync root.";

        public static string ConfirmDestructiveAction { get; } = "Sync";

        public static string CreateStartingStatus(string folderName)
        {
            return $"Syncing {NormalizeFolderName(folderName)}...";
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
