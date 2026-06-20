namespace Cotton.Mobile.Services
{
    public class CottonOnDeviceStorageBucketSnapshot
    {
        private CottonOnDeviceStorageBucketSnapshot(
            CottonOnDeviceStorageBucketKind kind,
            string title,
            string detailText,
            long sizeBytes,
            int itemCount,
            string singularUnit,
            string pluralUnit,
            bool isAttentionVisible)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "On-device storage bucket kind is unknown.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Bucket title is required.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(detailText))
            {
                throw new ArgumentException("Bucket details are required.", nameof(detailText));
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Bucket size cannot be negative.");
            }

            if (itemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount), "Bucket item count cannot be negative.");
            }

            Kind = kind;
            Title = title.Trim();
            DetailText = detailText.Trim();
            SizeBytes = sizeBytes;
            ItemCount = itemCount;
            SizeText = CottonFileSizeFormatter.Format(sizeBytes);
            CountText = FormatCount(itemCount, singularUnit, pluralUnit);
            IsAttentionVisible = isAttentionVisible;
        }

        public CottonOnDeviceStorageBucketKind Kind { get; }

        public string Title { get; }

        public string DetailText { get; }

        public long SizeBytes { get; }

        public int ItemCount { get; }

        public string SizeText { get; }

        public string CountText { get; }

        public bool IsAttentionVisible { get; }

        public bool IsVisible => SizeBytes > 0 || ItemCount > 0 || IsAttentionVisible;

        public static CottonOnDeviceStorageBucketSnapshot CreateAvailableOfflineFiles(
            int fileCount,
            long sizeBytes)
        {
            return new CottonOnDeviceStorageBucketSnapshot(
                CottonOnDeviceStorageBucketKind.OfflineAvailable,
                "Offline files",
                "Ready on this device.",
                sizeBytes,
                fileCount,
                "file",
                "files",
                isAttentionVisible: false);
        }

        public static CottonOnDeviceStorageBucketSnapshot CreateStaleOfflineFiles(
            int fileCount,
            long sizeBytes)
        {
            return new CottonOnDeviceStorageBucketSnapshot(
                CottonOnDeviceStorageBucketKind.OfflineStale,
                "Refresh needed",
                "Kept offline but older than cloud.",
                sizeBytes,
                fileCount,
                "file",
                "files",
                isAttentionVisible: fileCount > 0);
        }

        public static CottonOnDeviceStorageBucketSnapshot CreateMissingOfflineFiles(int fileCount)
        {
            return new CottonOnDeviceStorageBucketSnapshot(
                CottonOnDeviceStorageBucketKind.OfflineMissing,
                "Offline files missing",
                "Marked offline but not saved here.",
                sizeBytes: 0,
                fileCount,
                "file",
                "files",
                isAttentionVisible: fileCount > 0);
        }

        public static CottonOnDeviceStorageBucketSnapshot CreateCachedFolderListings(
            int listCount,
            long sizeBytes)
        {
            return new CottonOnDeviceStorageBucketSnapshot(
                CottonOnDeviceStorageBucketKind.CachedFolderListings,
                "Saved folder lists",
                "Saved navigation for offline use.",
                sizeBytes,
                listCount,
                "list",
                "lists",
                isAttentionVisible: false);
        }

        public static CottonOnDeviceStorageBucketSnapshot CreateThumbnails(
            int previewCount,
            long sizeBytes)
        {
            return new CottonOnDeviceStorageBucketSnapshot(
                CottonOnDeviceStorageBucketKind.Thumbnails,
                "Thumbnails",
                "Saved file previews.",
                sizeBytes,
                previewCount,
                "preview",
                "previews",
                isAttentionVisible: false);
        }

        private static string FormatCount(int count, string singularUnit, string pluralUnit)
        {
            return count == 1 ? $"1 {singularUnit}" : $"{count:N0} {pluralUnit}";
        }
    }
}
