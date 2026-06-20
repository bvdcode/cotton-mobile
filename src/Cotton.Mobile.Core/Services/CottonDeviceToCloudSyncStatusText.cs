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
