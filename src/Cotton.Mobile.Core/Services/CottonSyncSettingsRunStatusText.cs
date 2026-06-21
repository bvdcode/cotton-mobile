namespace Cotton.Mobile.Services
{
    public static class CottonSyncSettingsRunStatusText
    {
        public const string StartingAllStatus = "Syncing folders...";

        public static string OfflineUnavailableStatus { get; } =
            CottonCloudToDeviceSyncStatusText.OfflineUnavailableStatus;

        public static string FailedStatus { get; } =
            CottonCloudToDeviceSyncStatusText.FailedStatus;

        public static string CreateCompletedStatus(
            CottonCloudToDeviceSyncRunSummary cloudToDeviceSummary,
            CottonDeviceToCloudSyncRunSummary deviceToCloudSummary,
            CottonBidirectionalSyncRunSummary? bidirectionalSummary = null)
        {
            ArgumentNullException.ThrowIfNull(cloudToDeviceSummary);
            ArgumentNullException.ThrowIfNull(deviceToCloudSummary);

            int rootCount = cloudToDeviceSummary.RootCount
                + deviceToCloudSummary.RootCount
                + (bidirectionalSummary?.RootCount ?? 0);
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
            AddCount(parts, bidirectionalSummary?.DownloadedCount ?? 0, "bidirectional downloaded");
            AddCount(parts, bidirectionalSummary?.RefreshedLocalCount ?? 0, "bidirectional refreshed locally");
            AddCount(parts, bidirectionalSummary?.RenamedLocalCount ?? 0, "bidirectional renamed locally");
            AddCount(parts, bidirectionalSummary?.RemovedLocalCount ?? 0, "bidirectional removed locally");
            AddCount(parts, bidirectionalSummary?.UploadedCount ?? 0, "bidirectional uploaded");
            AddCount(parts, bidirectionalSummary?.RefreshedRemoteCount ?? 0, "bidirectional updated in cloud");
            AddCount(
                parts,
                deviceToCloudSummary.CreatedFolderCount + (bidirectionalSummary?.CreatedFolderCount ?? 0),
                "folder created",
                "folders created");
            AddCount(
                parts,
                deviceToCloudSummary.DeletedRemoteFileCount + (bidirectionalSummary?.DeletedRemoteFileCount ?? 0),
                "remote file removed",
                "remote files removed");
            AddCount(
                parts,
                deviceToCloudSummary.RemovedManifestCount + (bidirectionalSummary?.RemovedManifestCount ?? 0),
                "record cleaned",
                "records cleaned");
            AddCount(
                parts,
                deviceToCloudSummary.DestructiveReviewRemoteDeleteCount,
                "cloud removal needs review",
                "cloud removals need review");
            AddCount(
                parts,
                bidirectionalSummary?.ConflictReviewCount ?? 0,
                "bidirectional conflict needs review",
                "bidirectional conflicts need review");
            AddCount(
                parts,
                bidirectionalSummary?.DestructiveReviewLocalDeleteCount ?? 0,
                "bidirectional local removal needs review",
                "bidirectional local removals need review");
            AddCount(
                parts,
                bidirectionalSummary?.DestructiveReviewRemoteDeleteCount ?? 0,
                "bidirectional cloud removal needs review",
                "bidirectional cloud removals need review");
            AddCount(
                parts,
                cloudToDeviceSummary.BlockedItemCount
                    + deviceToCloudSummary.BlockedItemCount
                    + (bidirectionalSummary?.BlockedItemCount ?? 0),
                "blocked");
            AddRootCount(
                parts,
                cloudToDeviceSummary.SkippedRootCount
                    + deviceToCloudSummary.SkippedRootCount
                    + (bidirectionalSummary?.SkippedRootCount ?? 0));

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
