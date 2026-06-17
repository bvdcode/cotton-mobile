namespace Cotton.Mobile.Services
{
    public class FileDownloadCacheOptions
    {
        private const long Megabyte = 1024 * 1024;

        public FileDownloadCacheOptions(long maxCacheBytes)
        {
            if (maxCacheBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCacheBytes), "Download cache size must be positive.");
            }

            MaxCacheBytes = maxCacheBytes;
        }

        public long MaxCacheBytes { get; }

        public static FileDownloadCacheOptions Default { get; } = new(512 * Megabyte);
    }
}
