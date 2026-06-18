using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class StorageManagementService : IStorageManagementService
    {
        private const string ThumbnailCacheName = "Thumbnails";
        private const string DownloadedFilesName = "Downloaded files";
        private const string TemporaryThumbnailFileExtension = ".tmp";

        private readonly FileThumbnailCacheOptions _thumbnailOptions;
        private readonly ILogger<StorageManagementService> _logger;

        public StorageManagementService(
            FileThumbnailCacheOptions thumbnailOptions,
            ILogger<StorageManagementService> logger)
        {
            ArgumentNullException.ThrowIfNull(thumbnailOptions);
            ArgumentNullException.ThrowIfNull(logger);

            _thumbnailOptions = thumbnailOptions;
            _logger = logger;
        }

        public event EventHandler? DownloadedFilesCleared;

        public Task<CottonStorageSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return new CottonStorageSummary(
                        ScanDirectory(
                            ThumbnailCacheName,
                            CottonMobileStoragePaths.CreateThumbnailCacheDirectory(_thumbnailOptions),
                            SearchOption.TopDirectoryOnly,
                            cancellationToken,
                            includeTemporaryThumbnails: false),
                        ScanDirectory(
                            DownloadedFilesName,
                            CottonMobileStoragePaths.CreateDownloadsDirectory(),
                            SearchOption.AllDirectories,
                            cancellationToken,
                            includeTemporaryDownloads: false));
                },
                cancellationToken);
        }

        public Task ClearThumbnailCacheAsync(CancellationToken cancellationToken = default)
        {
            return ClearDirectoryAsync(
                CottonMobileStoragePaths.CreateThumbnailCacheDirectory(_thumbnailOptions),
                cancellationToken);
        }

        public async Task ClearDownloadedFilesAsync(CancellationToken cancellationToken = default)
        {
            await ClearDirectoryAsync(
                CottonMobileStoragePaths.CreateDownloadsDirectory(),
                cancellationToken,
                includeTemporaryDownloads: false).ConfigureAwait(false);
            DownloadedFilesCleared?.Invoke(this, EventArgs.Empty);
        }

        public async Task ClearAllCachedFilesAsync(CancellationToken cancellationToken = default)
        {
            List<Exception> failures = [];
            await TryClearCacheAreaAsync(
                ClearThumbnailCacheAsync,
                "thumbnail cache",
                failures,
                cancellationToken).ConfigureAwait(false);
            await TryClearCacheAreaAsync(
                ClearDownloadedFilesAsync,
                "downloaded files",
                failures,
                cancellationToken).ConfigureAwait(false);

            if (failures.Count == 1)
            {
                throw new InvalidOperationException("Failed to clear one Cotton mobile cache area.", failures[0]);
            }

            if (failures.Count > 1)
            {
                throw new AggregateException("Failed to clear Cotton mobile cached files.", failures);
            }
        }

        private async Task TryClearCacheAreaAsync(
            Func<CancellationToken, Task> clearAsync,
            string cacheAreaName,
            List<Exception> failures,
            CancellationToken cancellationToken)
        {
            try
            {
                await clearAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Failed to clear Cotton mobile {CacheAreaName}.", cacheAreaName);
                failures.Add(exception);
            }
        }

        private Task ClearDirectoryAsync(
            string directory,
            CancellationToken cancellationToken,
            bool includeTemporaryDownloads = true)
        {
            return Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Directory.Exists(directory))
                    {
                        return;
                    }

                    foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!includeTemporaryDownloads && CottonMobileStoragePaths.IsTemporaryDownloadPath(file))
                        {
                            continue;
                        }

                        DeleteFile(file);
                    }

                    DeleteEmptyDirectories(directory, cancellationToken);
                },
                cancellationToken);
        }

        private static CottonStorageCategorySnapshot ScanDirectory(
            string name,
            string directory,
            SearchOption searchOption,
            CancellationToken cancellationToken,
            bool includeTemporaryThumbnails = true,
            bool includeTemporaryDownloads = true)
        {
            if (!Directory.Exists(directory))
            {
                return new CottonStorageCategorySnapshot(name, 0, 0);
            }

            long sizeBytes = 0;
            int fileCount = 0;
            foreach (string file in Directory.EnumerateFiles(directory, "*", searchOption))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!includeTemporaryDownloads && CottonMobileStoragePaths.IsTemporaryDownloadPath(file))
                {
                    continue;
                }

                if (!includeTemporaryThumbnails && IsTemporaryThumbnailPath(file))
                {
                    continue;
                }

                var info = new FileInfo(file);
                if (!info.Exists)
                {
                    continue;
                }

                sizeBytes += info.Length;
                fileCount++;
            }

            return new CottonStorageCategorySnapshot(name, sizeBytes, fileCount);
        }

        private static bool IsTemporaryThumbnailPath(string path)
        {
            return path.EndsWith(TemporaryThumbnailFileExtension, StringComparison.OrdinalIgnoreCase);
        }

        private void DeleteFile(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(exception, "Failed to delete Cotton mobile storage file {Path}.", file);
            }
        }

        private void DeleteEmptyDirectories(string rootDirectory, CancellationToken cancellationToken)
        {
            foreach (string directory in Directory
                .EnumerateDirectories(rootDirectory, "*", SearchOption.AllDirectories)
                .OrderByDescending(path => path.Length))
            {
                cancellationToken.ThrowIfCancellationRequested();
                TryDeleteDirectory(directory);
            }
        }

        private void TryDeleteDirectory(string directory)
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Failed to delete empty Cotton mobile storage directory {Path}.", directory);
            }
        }
    }
}
