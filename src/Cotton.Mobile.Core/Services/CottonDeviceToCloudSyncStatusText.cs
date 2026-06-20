namespace Cotton.Mobile.Services
{
    public static class CottonDeviceToCloudSyncStatusText
    {
        private const string DefaultFolderName = "Folder";

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
