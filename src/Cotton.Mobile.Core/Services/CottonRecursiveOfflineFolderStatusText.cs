namespace Cotton.Mobile.Services
{
    public static class CottonRecursiveOfflineFolderStatusText
    {
        public static string ScanningStatus { get; } = "Scanning nested folders for offline use...";

        public static string CreatePlanStatus(
            CottonRecursiveOfflineFolderPlanSnapshot plan,
            bool isCachedEstimate = false)
        {
            ArgumentNullException.ThrowIfNull(plan);

            string prefix = isCachedEstimate ? "Cached estimate: " : string.Empty;
            return plan.Status switch
            {
                CottonRecursiveOfflineFolderPlanStatus.Empty =>
                    $"{prefix}{plan.FolderName} has no files to keep offline.",
                CottonRecursiveOfflineFolderPlanStatus.NeedsFolderScan =>
                    $"{prefix}{plan.FolderName}: {FormatFileCount(plan.FileCount)}, {FormatFolderScanCount(plan.MissingFolderContentCount)}, {FormatKnownSize(plan)}.",
                CottonRecursiveOfflineFolderPlanStatus.HasUnknownSize =>
                    $"{prefix}{plan.FolderName}: {FormatFileCount(plan.FileCount)}, {FormatFolderCount(plan.FolderCount)}, size unknown. Offline folder needs exact file sizes.",
                _ =>
                    $"{prefix}{plan.FolderName}: {FormatFileCount(plan.FileCount)}, {FormatFolderCount(plan.FolderCount)}, {CottonFileSizeFormatter.Format(plan.KnownSizeBytes)}. Recursive offline folder plan is ready.",
            };
        }

        private static string FormatKnownSize(CottonRecursiveOfflineFolderPlanSnapshot plan)
        {
            return plan.HasExactSize
                ? CottonFileSizeFormatter.Format(plan.KnownSizeBytes)
                : "partial size";
        }

        private static string FormatFileCount(int count)
        {
            return count == 1 ? "1 file" : $"{count} files";
        }

        private static string FormatFolderCount(int count)
        {
            return count == 1 ? "1 folder" : $"{count} folders";
        }

        private static string FormatFolderScanCount(int count)
        {
            return count == 1 ? "1 folder needs scanning" : $"{count} folders need scanning";
        }
    }
}
