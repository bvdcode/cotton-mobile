namespace Cotton.Mobile.Services
{
    public class CottonStorageBudgetSummary
    {
        private CottonStorageBudgetSummary(
            IReadOnlyList<CottonStorageBudgetBucketSnapshot> buckets,
            int protectedOfflineFileCount,
            long protectedOfflineBytes)
        {
            ArgumentNullException.ThrowIfNull(buckets);
            if (protectedOfflineFileCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(protectedOfflineFileCount),
                    "Protected offline file count cannot be negative.");
            }

            if (protectedOfflineBytes < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(protectedOfflineBytes),
                    "Protected offline size cannot be negative.");
            }

            Buckets = buckets;
            ProtectedOfflineFileCount = protectedOfflineFileCount;
            ProtectedOfflineBytes = protectedOfflineBytes;
            TotalEvictableBytes = buckets.Sum(bucket => bucket.SizeBytes);
            TotalBudgetBytes = buckets.Sum(bucket => bucket.BudgetBytes);
            SummaryText = TotalEvictableBytes == 0
                ? "Temporary cache is empty."
                : $"{CottonFileSizeFormatter.Format(TotalEvictableBytes)} of {CottonFileSizeFormatter.Format(TotalBudgetBytes)} temporary cache used";
            ProtectedOfflineText = protectedOfflineFileCount == 0
                ? "No kept-offline files are stored separately from cleanup."
                : $"{FormatFileCount(protectedOfflineFileCount)} kept offline, {CottonFileSizeFormatter.Format(protectedOfflineBytes)} stays on this device.";
        }

        public IReadOnlyList<CottonStorageBudgetBucketSnapshot> Buckets { get; }

        public int ProtectedOfflineFileCount { get; }

        public long ProtectedOfflineBytes { get; }

        public long TotalEvictableBytes { get; }

        public long TotalBudgetBytes { get; }

        public string SummaryText { get; }

        public string ProtectedOfflineText { get; }

        public bool IsAttentionVisible => Buckets.Any(bucket => bucket.IsAttentionVisible);

        public static CottonStorageBudgetSummary Empty { get; } = Create(
            evictableDownloadFileCount: 0,
            evictableDownloadBytes: 0,
            evictableDownloadBudgetBytes: 1,
            thumbnailCount: 0,
            thumbnailBytes: 0,
            thumbnailBudgetBytes: 1,
            folderListingCount: 0,
            folderListingBytes: 0,
            folderListingBudgetBytes: 1,
            protectedOfflineFileCount: 0,
            protectedOfflineBytes: 0);

        public static CottonStorageBudgetSummary Create(
            int evictableDownloadFileCount,
            long evictableDownloadBytes,
            long evictableDownloadBudgetBytes,
            int thumbnailCount,
            long thumbnailBytes,
            long thumbnailBudgetBytes,
            int folderListingCount,
            long folderListingBytes,
            long folderListingBudgetBytes,
            int protectedOfflineFileCount,
            long protectedOfflineBytes)
        {
            return new CottonStorageBudgetSummary(
            [
                CottonStorageBudgetBucketSnapshot.CreateEvictableDownloads(
                    evictableDownloadFileCount,
                    evictableDownloadBytes,
                    evictableDownloadBudgetBytes),
                CottonStorageBudgetBucketSnapshot.CreateThumbnails(
                    thumbnailCount,
                    thumbnailBytes,
                    thumbnailBudgetBytes),
                CottonStorageBudgetBucketSnapshot.CreateFolderListings(
                    folderListingCount,
                    folderListingBytes,
                    folderListingBudgetBytes),
            ],
            protectedOfflineFileCount,
            protectedOfflineBytes);
        }

        private static string FormatFileCount(int fileCount)
        {
            return fileCount == 1 ? "1 file" : $"{fileCount:N0} files";
        }
    }
}
