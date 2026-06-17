namespace Cotton.Mobile.Services
{
    public class FileThumbnailCacheOptions
    {
        private const long Megabyte = 1024 * 1024;

        public FileThumbnailCacheOptions(
            string directoryName,
            long maxCacheBytes,
            long maxEntryBytes,
            int maxParallelDownloads)
        {
            if (string.IsNullOrWhiteSpace(directoryName))
            {
                throw new ArgumentException("Thumbnail cache directory name is required.", nameof(directoryName));
            }

            if (maxCacheBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCacheBytes), "Thumbnail cache size must be positive.");
            }

            if (maxEntryBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEntryBytes), "Thumbnail entry size must be positive.");
            }

            if (maxEntryBytes > maxCacheBytes)
            {
                throw new ArgumentException("Thumbnail entry size cannot exceed total cache size.", nameof(maxEntryBytes));
            }

            if (maxParallelDownloads <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxParallelDownloads),
                    "Thumbnail parallel download limit must be positive.");
            }

            DirectoryName = directoryName.Trim();
            MaxCacheBytes = maxCacheBytes;
            MaxEntryBytes = maxEntryBytes;
            MaxParallelDownloads = maxParallelDownloads;
        }

        public string DirectoryName { get; }

        public long MaxCacheBytes { get; }

        public long MaxEntryBytes { get; }

        public int MaxParallelDownloads { get; }

        public static FileThumbnailCacheOptions Default { get; } = new(
            "ThumbnailCache",
            50 * Megabyte,
            2 * Megabyte,
            4);
    }
}
