namespace Cotton.Mobile.Services
{
    public class CottonStorageSummary
    {
        public CottonStorageSummary(
            CottonStorageCategorySnapshot thumbnailCache,
            CottonStorageCategorySnapshot folderListings,
            CottonStorageCategorySnapshot downloadedFiles,
            CottonStorageCategorySnapshot transferStaging,
            CottonOnDeviceStorageSummary onDeviceStorage,
            CottonStorageBudgetSummary budget,
            CottonCloudStorageQuotaSnapshot cloudQuota)
        {
            ArgumentNullException.ThrowIfNull(thumbnailCache);
            ArgumentNullException.ThrowIfNull(folderListings);
            ArgumentNullException.ThrowIfNull(downloadedFiles);
            ArgumentNullException.ThrowIfNull(transferStaging);
            ArgumentNullException.ThrowIfNull(onDeviceStorage);
            ArgumentNullException.ThrowIfNull(budget);
            ArgumentNullException.ThrowIfNull(cloudQuota);

            ThumbnailCache = thumbnailCache;
            FolderListings = folderListings;
            DownloadedFiles = downloadedFiles;
            TransferStaging = transferStaging;
            OnDeviceStorage = onDeviceStorage;
            Budget = budget;
            CloudQuota = cloudQuota;
        }

        public CottonStorageCategorySnapshot ThumbnailCache { get; }

        public CottonStorageCategorySnapshot FolderListings { get; }

        public CottonStorageCategorySnapshot DownloadedFiles { get; }

        public CottonStorageCategorySnapshot TransferStaging { get; }

        public CottonOnDeviceStorageSummary OnDeviceStorage { get; }

        public CottonStorageBudgetSummary Budget { get; }

        public CottonCloudStorageQuotaSnapshot CloudQuota { get; }

        public long TotalSizeBytes =>
            ThumbnailCache.SizeBytes + FolderListings.SizeBytes + DownloadedFiles.SizeBytes + TransferStaging.SizeBytes;

        public int TotalFileCount =>
            ThumbnailCache.FileCount + FolderListings.FileCount + DownloadedFiles.FileCount + TransferStaging.FileCount;
    }
}
