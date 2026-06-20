namespace Cotton.Mobile.Services
{
    public static class CottonOfflineFolderStatusText
    {
        private const string DefaultFolderName = "Folder";

        public static string OfflineUnavailableStatus { get; } = "Offline. Folder offline needs internet.";

        public static string CancelledStatus { get; } = "Keep folder offline cancelled.";

        public static string FailedStatus { get; } = "Could not plan folder offline.";

        public static string CreateStartingStatus(string folderName)
        {
            return $"Checking {NormalizeFolderName(folderName)} for offline use...";
        }

        public static string CreatePlanStatus(
            CottonOfflineFolderPlanSnapshot plan,
            bool isCachedEstimate = false)
        {
            ArgumentNullException.ThrowIfNull(plan);

            string prefix = isCachedEstimate ? "Cached estimate: " : string.Empty;
            return plan.Status switch
            {
                CottonOfflineFolderPlanStatus.Empty =>
                    $"{prefix}{plan.FolderName} has no files to keep offline.",
                CottonOfflineFolderPlanStatus.ContainsFolders =>
                    $"{prefix}{plan.FolderName}: {FormatFileCount(plan.FileCount)}, {FormatFolderCount(plan.FolderCount)}, {FormatKnownSize(plan)}. Nested folders need scanning before offline download.",
                CottonOfflineFolderPlanStatus.HasUnknownSize =>
                    $"{prefix}{plan.FolderName}: {FormatFileCount(plan.FileCount)}, size unknown. Offline folder needs exact file sizes.",
                _ =>
                    $"{prefix}{plan.FolderName}: {FormatFileCount(plan.FileCount)}, {CottonFileSizeFormatter.Format(plan.KnownSizeBytes)}. Ready to keep offline.",
            };
        }

        private static string FormatKnownSize(CottonOfflineFolderPlanSnapshot plan)
        {
            if (!plan.HasExactSize)
            {
                return "partial size";
            }

            return CottonFileSizeFormatter.Format(plan.KnownSizeBytes);
        }

        private static string FormatFileCount(int count)
        {
            return count == 1 ? "1 file" : $"{count} files";
        }

        private static string FormatFolderCount(int count)
        {
            return count == 1 ? "1 folder" : $"{count} folders";
        }

        private static string NormalizeFolderName(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName) ? DefaultFolderName : folderName.Trim();
        }
    }
}
