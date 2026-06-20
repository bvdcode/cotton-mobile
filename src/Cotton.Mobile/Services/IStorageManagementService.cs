namespace Cotton.Mobile.Services
{
    public interface IStorageManagementService
    {
        event EventHandler? DownloadedFilesCleared;

        Task<CottonStorageSummary> GetSummaryAsync(CancellationToken cancellationToken = default);

        Task ClearThumbnailCacheAsync(CancellationToken cancellationToken = default);

        Task ClearFolderListingsCacheAsync(CancellationToken cancellationToken = default);

        Task ClearDownloadedFilesAsync(CancellationToken cancellationToken = default);

        Task<CottonTransferStagedFileCleanupResult> ClearTemporaryUploadsAsync(
            CancellationToken cancellationToken = default);

        Task<CottonDeviceSpaceCleanupResult> FreeDeviceSpaceAsync(CancellationToken cancellationToken = default);

        Task ClearAllCachedFilesAsync(CancellationToken cancellationToken = default);
    }
}
