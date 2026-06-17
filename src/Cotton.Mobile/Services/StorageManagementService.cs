using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class StorageManagementService : IStorageManagementService
    {
        private const string ThumbnailCacheName = "Thumbnails";
        private const string DownloadedFilesName = "Downloaded files";

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
                            cancellationToken),
                        ScanDirectory(
                            DownloadedFilesName,
                            CottonMobileStoragePaths.CreateDownloadsDirectory(),
                            SearchOption.AllDirectories,
                            cancellationToken));
                },
                cancellationToken);
        }

        public Task ClearThumbnailCacheAsync(CancellationToken cancellationToken = default)
        {
            return ClearDirectoryAsync(
                CottonMobileStoragePaths.CreateThumbnailCacheDirectory(_thumbnailOptions),
                cancellationToken);
        }

        public Task ClearDownloadedFilesAsync(CancellationToken cancellationToken = default)
        {
            return ClearDirectoryAsync(
                CottonMobileStoragePaths.CreateDownloadsDirectory(),
                cancellationToken);
        }

        public async Task ClearAllCachedFilesAsync(CancellationToken cancellationToken = default)
        {
            await ClearThumbnailCacheAsync(cancellationToken).ConfigureAwait(false);
            await ClearDownloadedFilesAsync(cancellationToken).ConfigureAwait(false);
        }

        private Task ClearDirectoryAsync(string directory, CancellationToken cancellationToken)
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
            CancellationToken cancellationToken)
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
