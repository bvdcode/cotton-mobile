namespace Cotton.Mobile.Services
{
    public static class CottonCloudToDeviceSyncStatusText
    {
        private const string DefaultFolderName = "Folder";

        public const string ActionLabel = "Sync to this device";

        public const string StartingAllStatus = "Syncing folders...";

        public static string AccountUnavailableStatus { get; } = "Sync needs a fresh account session.";

        public static string OfflineUnavailableStatus { get; } = "Offline. Sync needs internet.";

        public static string CancelledStatus { get; } = "Sync cancelled.";

        public static string FailedStatus { get; } = "Sync failed.";

        public static string CreateStartingStatus(string folderName)
        {
            return $"Syncing {NormalizeFolderName(folderName)}...";
        }

        public static string CreateCompletedStatus(CottonCloudToDeviceSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            if (summary.RootCount == 0)
            {
                return "No folders are set to sync.";
            }

            List<string> parts = [];
            AddCount(parts, summary.DownloadedCount, "downloaded");
            AddCount(parts, summary.RefreshedCount, "refreshed");
            AddCount(parts, summary.RenamedCount, "renamed");
            AddCount(parts, summary.RemovedCount, "removed");
            AddCount(parts, summary.BlockedItemCount, "blocked");
            AddRootCount(parts, summary.SkippedRootCount);

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

        private static void AddRootCount(List<string> parts, int count)
        {
            if (count <= 0)
            {
                return;
            }

            parts.Add(count == 1 ? "1 root skipped" : $"{count} roots skipped");
        }

        private static string NormalizeFolderName(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName) ? DefaultFolderName : folderName.Trim();
        }
    }
}
