namespace Cotton.Mobile.Services
{
    public class CottonFileDownloadCacheEntry
    {
        public CottonFileDownloadCacheEntry(
            string path,
            long sizeBytes,
            DateTime activityUtc,
            bool requiresSensitiveEviction)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", nameof(path));
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size cannot be negative.");
            }

            Path = path;
            SizeBytes = sizeBytes;
            ActivityUtc = activityUtc;
            RequiresSensitiveEviction = requiresSensitiveEviction;
        }

        public string Path { get; }

        public long SizeBytes { get; }

        public DateTime ActivityUtc { get; }

        public bool RequiresSensitiveEviction { get; }
    }
}
