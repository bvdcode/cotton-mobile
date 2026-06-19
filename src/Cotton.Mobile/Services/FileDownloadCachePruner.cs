using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileDownloadCachePruner : IFileDownloadCachePruner
    {
        private readonly FileDownloadCacheOptions _options;
        private readonly ICottonOfflineFilePinStore _offlineFilePinStore;
        private readonly ILogger<FileDownloadCachePruner> _logger;

        public FileDownloadCachePruner(
            FileDownloadCacheOptions options,
            ICottonOfflineFilePinStore offlineFilePinStore,
            ILogger<FileDownloadCachePruner> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(offlineFilePinStore);
            ArgumentNullException.ThrowIfNull(logger);

            _options = options;
            _offlineFilePinStore = offlineFilePinStore;
            _logger = logger;
        }

        public async Task PruneAsync(
            Uri instanceUri,
            string? protectedPath = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            IReadOnlyCollection<string> protectedDirectories =
                await LoadProtectedDownloadDirectoriesAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            await Task.Run(
                    () => PruneBestEffort(protectedPath, protectedDirectories, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyCollection<string>> LoadProtectedDownloadDirectoriesAsync(
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CottonOfflineFilePinSnapshot> pins =
                await _offlineFilePinStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            return pins
                .Select(pin => CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, pin.FileId))
                .ToList();
        }

        private void PruneBestEffort(
            string? protectedPath,
            IReadOnlyCollection<string> protectedDirectories,
            CancellationToken cancellationToken)
        {
            try
            {
                PruneCore(protectedPath, protectedDirectories, cancellationToken);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Failed to prune Cotton mobile download cache.");
            }
        }

        private void PruneCore(
            string? protectedPath,
            IReadOnlyCollection<string> protectedDirectories,
            CancellationToken cancellationToken)
        {
            string rootDirectory = CottonMobileStoragePaths.CreateDownloadsDirectory();
            if (!Directory.Exists(rootDirectory))
            {
                return;
            }

            string? normalizedProtectedPath = NormalizeProtectedPath(protectedPath);
            DeleteAbandonedTemporaryDownloads(rootDirectory, cancellationToken);
            List<CottonFileDownloadCacheEntry> entries = Directory
                .EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories)
                .Where(path => !CottonMobileStoragePaths.IsTemporaryDownloadPath(path))
                .Select(path => new FileInfo(path))
                .Where(file => file.Exists)
                .Select(file => new CottonFileDownloadCacheEntry(
                    file.FullName,
                    file.Length,
                    ResolvePruneTimestamp(file)))
                .ToList();
            IReadOnlyList<string> deletePaths = CottonFileDownloadCachePrunePlanner.SelectFilesToDelete(
                entries,
                _options.MaxCacheBytes,
                normalizedProtectedPath,
                protectedDirectories);
            foreach (string deletePath in deletePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TryDelete(new FileInfo(deletePath));
            }

            DeleteEmptyDirectories(rootDirectory, cancellationToken);
        }

        private void DeleteAbandonedTemporaryDownloads(string rootDirectory, CancellationToken cancellationToken)
        {
            DateTime utcNow = DateTime.UtcNow;
            foreach (string path in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!CottonMobileStoragePaths.IsTemporaryDownloadPath(path))
                {
                    continue;
                }

                var file = new FileInfo(path);
                if (!file.Exists || !CottonTemporaryFilePolicy.IsAbandoned(file, utcNow))
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

        private static DateTime ResolvePruneTimestamp(FileInfo file)
        {
            return CottonTemporaryFilePolicy.ResolveActivityTimestampUtc(file);
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
