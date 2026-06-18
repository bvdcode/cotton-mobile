namespace Cotton.Mobile.Services
{
    public class CottonStorageSummary
    {
        public CottonStorageSummary(
            CottonStorageCategorySnapshot thumbnailCache,
            CottonStorageCategorySnapshot folderListings,
            CottonStorageCategorySnapshot downloadedFiles)
        {
            ArgumentNullException.ThrowIfNull(thumbnailCache);
            ArgumentNullException.ThrowIfNull(folderListings);
            ArgumentNullException.ThrowIfNull(downloadedFiles);

            ThumbnailCache = thumbnailCache;
            FolderListings = folderListings;
            DownloadedFiles = downloadedFiles;
        }

        public CottonStorageCategorySnapshot ThumbnailCache { get; }

        public CottonStorageCategorySnapshot FolderListings { get; }

        public CottonStorageCategorySnapshot DownloadedFiles { get; }

        public long TotalSizeBytes => ThumbnailCache.SizeBytes + FolderListings.SizeBytes + DownloadedFiles.SizeBytes;

        public int TotalFileCount => ThumbnailCache.FileCount + FolderListings.FileCount + DownloadedFiles.FileCount;
    }
}
