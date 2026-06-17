namespace Cotton.Mobile.Services
{
    public class CottonStorageSummary
    {
        public CottonStorageSummary(
            CottonStorageCategorySnapshot thumbnailCache,
            CottonStorageCategorySnapshot downloadedFiles)
        {
            ArgumentNullException.ThrowIfNull(thumbnailCache);
            ArgumentNullException.ThrowIfNull(downloadedFiles);

            ThumbnailCache = thumbnailCache;
            DownloadedFiles = downloadedFiles;
        }

        public CottonStorageCategorySnapshot ThumbnailCache { get; }

        public CottonStorageCategorySnapshot DownloadedFiles { get; }

        public long TotalSizeBytes => ThumbnailCache.SizeBytes + DownloadedFiles.SizeBytes;

        public int TotalFileCount => ThumbnailCache.FileCount + DownloadedFiles.FileCount;
    }
}
