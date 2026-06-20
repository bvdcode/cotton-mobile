namespace Cotton.Mobile.Services
{
    public static class CottonSyncSettingsRunStatusText
    {
        public const string StartingAllStatus = "Syncing folders...";

        public static string CreateCompletedStatus(
            CottonCloudToDeviceSyncRunSummary cloudToDeviceSummary,
            CottonDeviceToCloudSyncRunSummary deviceToCloudSummary)
        {
            ArgumentNullException.ThrowIfNull(cloudToDeviceSummary);
            ArgumentNullException.ThrowIfNull(deviceToCloudSummary);

            int rootCount = cloudToDeviceSummary.RootCount + deviceToCloudSummary.RootCount;
            if (rootCount == 0)
            {
                return "No folders are set to sync.";
            }

            List<string> parts = [];
            AddCount(parts, cloudToDeviceSummary.DownloadedCount, "downloaded");
            AddCount(parts, cloudToDeviceSummary.RefreshedCount, "refreshed");
            AddCount(parts, cloudToDeviceSummary.RenamedCount, "renamed");
            AddCount(parts, cloudToDeviceSummary.RemovedCount, "removed");
            AddCount(parts, deviceToCloudSummary.UploadedCount, "uploaded");
            AddCount(parts, deviceToCloudSummary.RefreshedCount, "updated");
            AddCount(
                parts,
                deviceToCloudSummary.CreatedFolderCount,
                "folder created",
                "folders created");
            AddCount(
                parts,
                deviceToCloudSummary.DeletedRemoteFileCount,
                "remote file removed",
                "remote files removed");
            AddCount(
                parts,
                deviceToCloudSummary.RemovedManifestCount,
                "record cleaned",
                "records cleaned");
            AddCount(parts, cloudToDeviceSummary.BlockedItemCount + deviceToCloudSummary.BlockedItemCount, "blocked");
            AddRootCount(parts, cloudToDeviceSummary.SkippedRootCount + deviceToCloudSummary.SkippedRootCount);

            if (parts.Count == 0)
            {
                return "Sync complete. Everything is up to date.";
            }

            return $"Sync complete. {string.Join(", ", parts)}.";
        }

        private static void AddCount(List<string> parts, int count, string label)
        {
            if (count <= 0)
            {
                return;
            }

            parts.Add($"{count} {label}");
        }

        private static void AddCount(List<string> parts, int count, string singularLabel, string pluralLabel)
        {
            if (count <= 0)
            {
                return;
            }

            parts.Add(count == 1 ? $"1 {singularLabel}" : $"{count} {pluralLabel}");
        }

        private static void AddRootCount(List<string> parts, int count)
        {
            if (count <= 0)
            {
                return;
            }

            parts.Add(count == 1 ? "1 root skipped" : $"{count} roots skipped");
        }
    }
}
