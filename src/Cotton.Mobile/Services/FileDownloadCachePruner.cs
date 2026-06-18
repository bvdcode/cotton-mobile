using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileDownloadCachePruner : IFileDownloadCachePruner
    {
        private static readonly TimeSpan TemporaryDownloadGracePeriod = TimeSpan.FromHours(6);

        private readonly FileDownloadCacheOptions _options;
        private readonly ILogger<FileDownloadCachePruner> _logger;

        public FileDownloadCachePruner(
            FileDownloadCacheOptions options,
            ILogger<FileDownloadCachePruner> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _options = options;
            _logger = logger;
        }

        public Task PruneAsync(string? protectedPath = null, CancellationToken cancellationToken = default)
        {
            return Task.Run(
                () => PruneBestEffort(protectedPath, cancellationToken),
                cancellationToken);
        }

        private void PruneBestEffort(string? protectedPath, CancellationToken cancellationToken)
        {
            try
            {
                PruneCore(protectedPath, cancellationToken);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Failed to prune Cotton mobile download cache.");
            }
        }

        private void PruneCore(string? protectedPath, CancellationToken cancellationToken)
        {
            string rootDirectory = CottonMobileStoragePaths.CreateDownloadsDirectory();
            if (!Directory.Exists(rootDirectory))
            {
                return;
            }

            string? normalizedProtectedPath = NormalizeProtectedPath(protectedPath);
            DeleteAbandonedTemporaryDownloads(rootDirectory, cancellationToken);
            List<FileInfo> files = Directory
                .EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories)
                .Where(path => !CottonMobileStoragePaths.IsTemporaryDownloadPath(path))
                .Select(path => new FileInfo(path))
                .Where(file => file.Exists)
                .OrderBy(ResolvePruneTimestamp)
                .ThenBy(file => file.FullName, StringComparer.Ordinal)
                .ToList();

            long totalBytes = files.Sum(file => file.Length);
            foreach (FileInfo file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (totalBytes <= _options.MaxCacheBytes)
                {
                    break;
                }

                if (IsProtected(file, normalizedProtectedPath))
                {
                    continue;
                }

                totalBytes -= TryDelete(file);
            }

            DeleteEmptyDirectories(rootDirectory, cancellationToken);
        }

        private void DeleteAbandonedTemporaryDownloads(string rootDirectory, CancellationToken cancellationToken)
        {
            DateTime cutoffUtc = DateTime.UtcNow - TemporaryDownloadGracePeriod;
            foreach (string path in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!CottonMobileStoragePaths.IsTemporaryDownloadPath(path))
                {
                    continue;
                }

                var file = new FileInfo(path);
                if (!file.Exists || ResolvePruneTimestamp(file) > cutoffUtc)
                {
                    continue;
                }

                TryDelete(file);
            }
        }

        private static string? NormalizeProtectedPath(string? protectedPath)
        {
            return string.IsNullOrWhiteSpace(protectedPath)
                ? null
                : Path.GetFullPath(protectedPath);
        }

        private static bool IsProtected(FileInfo file, string? normalizedProtectedPath)
        {
            return normalizedProtectedPath is not null
                && string.Equals(file.FullName, normalizedProtectedPath, StringComparison.Ordinal);
        }

        private static DateTime ResolvePruneTimestamp(FileInfo file)
        {
            return file.LastAccessTimeUtc > file.LastWriteTimeUtc
                ? file.LastAccessTimeUtc
                : file.LastWriteTimeUtc;
        }

        private long TryDelete(FileInfo file)
        {
            try
            {
                long length = file.Length;
                file.Delete();
                return length;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Failed to prune Cotton mobile downloaded file {Path}.", file.FullName);
                return 0;
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
                _logger.LogDebug(exception, "Failed to prune empty Cotton mobile download directory {Path}.", directory);
            }
        }
    }
}
