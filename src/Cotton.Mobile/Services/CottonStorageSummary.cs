namespace Cotton.Mobile.Services
{
    public class CottonStorageSummary
    {
        public CottonStorageSummary(
            CottonStorageCategorySnapshot thumbnailCache,
            CottonStorageCategorySnapshot folderListings,
            CottonStorageCategorySnapshot downloadedFiles,
            CottonOnDeviceStorageSummary onDeviceStorage,
            CottonStorageBudgetSummary budget)
        {
            ArgumentNullException.ThrowIfNull(thumbnailCache);
            ArgumentNullException.ThrowIfNull(folderListings);
            ArgumentNullException.ThrowIfNull(downloadedFiles);
            ArgumentNullException.ThrowIfNull(onDeviceStorage);
            ArgumentNullException.ThrowIfNull(budget);

            ThumbnailCache = thumbnailCache;
            FolderListings = folderListings;
            DownloadedFiles = downloadedFiles;
            OnDeviceStorage = onDeviceStorage;
            Budget = budget;
        }

        public CottonStorageCategorySnapshot ThumbnailCache { get; }

        public CottonStorageCategorySnapshot FolderListings { get; }

        public CottonStorageCategorySnapshot DownloadedFiles { get; }

        public CottonOnDeviceStorageSummary OnDeviceStorage { get; }

        public CottonStorageBudgetSummary Budget { get; }

        public long TotalSizeBytes => ThumbnailCache.SizeBytes + FolderListings.SizeBytes + DownloadedFiles.SizeBytes;

        public int TotalFileCount => ThumbnailCache.FileCount + FolderListings.FileCount + DownloadedFiles.FileCount;
    }
}
