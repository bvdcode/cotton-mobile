namespace Cotton.Mobile.Services
{
    public static class CottonBidirectionalSyncStatusText
    {
        public const string ExecutionUnavailableStatus = "Bidirectional sync needs conflict review before it can run.";
        public const string ConflictReviewRequiredStatus = "Bidirectional sync needs conflict review.";
        public const string DestructiveReviewRequiredStatus = "Bidirectional sync needs review before removing files.";

        public static string CreateCompletedStatus(CottonBidirectionalSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            if (summary.RootCount == 0)
            {
                return "No bidirectional folders are set to sync.";
            }

            List<string> parts = [];
            AddCount(parts, summary.DownloadedCount, "downloaded");
            AddCount(parts, summary.RefreshedLocalCount, "refreshed locally");
            AddCount(parts, summary.RenamedLocalCount, "renamed locally");
            AddCount(parts, summary.RemovedLocalCount, "removed locally");
            AddCount(parts, summary.UploadedCount, "uploaded");
            AddCount(parts, summary.RefreshedRemoteCount, "updated in cloud");
            AddCount(parts, summary.CreatedFolderCount, "folder created", "folders created");
            AddCount(parts, summary.DeletedRemoteFileCount, "remote file removed", "remote files removed");
            AddCount(parts, summary.RemovedManifestCount, "record cleaned", "records cleaned");
            AddCount(parts, summary.ConflictReviewCount, "conflict needs review", "conflicts need review");
            AddCount(
                parts,
                summary.DestructiveReviewLocalDeleteCount,
                "local removal needs review",
                "local removals need review");
            AddCount(
                parts,
                summary.DestructiveReviewRemoteDeleteCount,
                "cloud removal needs review",
                "cloud removals need review");
            AddCount(parts, summary.BlockedItemCount, "blocked");
            AddRootCount(parts, summary.SkippedRootCount);

            if (parts.Count == 0)
            {
                return "Bidirectional sync complete. Everything is up to date.";
            }

            return $"Bidirectional sync complete. {string.Join(", ", parts)}.";
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
