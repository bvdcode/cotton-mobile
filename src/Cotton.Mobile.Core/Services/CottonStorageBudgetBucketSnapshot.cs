namespace Cotton.Mobile.Services
{
    public class CottonStorageBudgetBucketSnapshot
    {
        private const double NearBudgetRatio = 0.9;

        private CottonStorageBudgetBucketSnapshot(
            CottonStorageBudgetBucketKind kind,
            string title,
            string detailText,
            int itemCount,
            long sizeBytes,
            long budgetBytes,
            string singularUnit,
            string pluralUnit)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Storage budget bucket kind is unknown.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Storage budget title is required.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(detailText))
            {
                throw new ArgumentException("Storage budget details are required.", nameof(detailText));
            }

            if (itemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount), "Storage budget item count cannot be negative.");
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Storage budget size cannot be negative.");
            }

            if (budgetBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(budgetBytes), "Storage budget must be positive.");
            }

            Kind = kind;
            Title = title.Trim();
            DetailText = detailText.Trim();
            ItemCount = itemCount;
            SizeBytes = sizeBytes;
            BudgetBytes = budgetBytes;
            SizeText = CottonFileSizeFormatter.Format(sizeBytes);
            BudgetText = CottonFileSizeFormatter.Format(budgetBytes);
            UsageText = $"{SizeText} of {BudgetText}";
            CountText = FormatCount(itemCount, singularUnit, pluralUnit);
            Status = ResolveStatus(sizeBytes, budgetBytes);
            StatusText = CreateStatusText(Status);
        }

        public CottonStorageBudgetBucketKind Kind { get; }

        public string Title { get; }

        public string DetailText { get; }

        public int ItemCount { get; }

        public long SizeBytes { get; }

        public long BudgetBytes { get; }

        public string SizeText { get; }

        public string BudgetText { get; }

        public string UsageText { get; }

        public string CountText { get; }

        public CottonStorageBudgetStatus Status { get; }

        public string StatusText { get; }

        public bool IsAttentionVisible => Status is CottonStorageBudgetStatus.NearBudget or CottonStorageBudgetStatus.OverBudget;

        public static CottonStorageBudgetBucketSnapshot CreateEvictableDownloads(
            int fileCount,
            long sizeBytes,
            long budgetBytes)
        {
            return new CottonStorageBudgetBucketSnapshot(
                CottonStorageBudgetBucketKind.EvictableDownloads,
                "Evictable downloads",
                "Opened files not kept offline.",
                fileCount,
                sizeBytes,
                budgetBytes,
                "file",
                "files");
        }

        public static CottonStorageBudgetBucketSnapshot CreateThumbnails(
            int previewCount,
            long sizeBytes,
            long budgetBytes)
        {
            return new CottonStorageBudgetBucketSnapshot(
                CottonStorageBudgetBucketKind.Thumbnails,
                "Thumbnail cache",
                "Regenerated while browsing.",
                previewCount,
                sizeBytes,
                budgetBytes,
                "preview",
                "previews");
        }

        public static CottonStorageBudgetBucketSnapshot CreateFolderListings(
            int listCount,
            long sizeBytes,
            long budgetBytes)
        {
            return new CottonStorageBudgetBucketSnapshot(
                CottonStorageBudgetBucketKind.FolderListings,
                "Folder list cache",
                "Saved navigation for offline browsing.",
                listCount,
                sizeBytes,
                budgetBytes,
                "list",
                "lists");
        }

        private static CottonStorageBudgetStatus ResolveStatus(long sizeBytes, long budgetBytes)
        {
            if (sizeBytes == 0)
            {
                return CottonStorageBudgetStatus.Empty;
            }

            if (sizeBytes > budgetBytes)
            {
                return CottonStorageBudgetStatus.OverBudget;
            }

            return sizeBytes >= budgetBytes * NearBudgetRatio
                ? CottonStorageBudgetStatus.NearBudget
                : CottonStorageBudgetStatus.WithinBudget;
        }

        private static string CreateStatusText(CottonStorageBudgetStatus status)
        {
            return status switch
            {
                CottonStorageBudgetStatus.Empty => "Empty",
                CottonStorageBudgetStatus.WithinBudget => "Within budget",
                CottonStorageBudgetStatus.NearBudget => "Near budget",
                CottonStorageBudgetStatus.OverBudget => "Over budget",
                _ => string.Empty,
            };
        }

        private static string FormatCount(int count, string singularUnit, string pluralUnit)
        {
            return count == 1 ? $"1 {singularUnit}" : $"{count:N0} {pluralUnit}";
        }
    }
}
