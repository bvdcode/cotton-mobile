using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class StorageManagementService : IStorageManagementService
    {
        private const long Megabyte = 1024 * 1024;
        private const string ThumbnailCacheName = "Thumbnails";
        private const string FolderListingsName = "Folder listings";
        private const string DownloadedFilesName = "Downloaded files";
        private const string EvictableDownloadsName = "Evictable downloads";
        private const string TemporaryThumbnailFileExtension = ".tmp";
        private const long FolderListingBudgetBytes = 25 * Megabyte;

        private readonly FileThumbnailCacheOptions _thumbnailOptions;
        private readonly FileDownloadCacheOptions _downloadOptions;
        private readonly ILogger<StorageManagementService> _logger;

        public StorageManagementService(
            FileThumbnailCacheOptions thumbnailOptions,
            FileDownloadCacheOptions downloadOptions,
            ILogger<StorageManagementService> logger)
        {
            ArgumentNullException.ThrowIfNull(thumbnailOptions);
            ArgumentNullException.ThrowIfNull(downloadOptions);
            ArgumentNullException.ThrowIfNull(logger);

            _thumbnailOptions = thumbnailOptions;
            _downloadOptions = downloadOptions;
            _logger = logger;
        }

        public event EventHandler? DownloadedFilesCleared;

        public Task<CottonStorageSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CottonStorageCategorySnapshot thumbnails = ScanDirectory(
                        ThumbnailCacheName,
                        CottonMobileStoragePaths.CreateThumbnailCacheDirectory(_thumbnailOptions),
                        SearchOption.TopDirectoryOnly,
                        cancellationToken,
                        includeTemporaryThumbnails: false);
                    CottonStorageCategorySnapshot folderListings = ScanDirectory(
                        FolderListingsName,
                        CottonMobileStoragePaths.CreateFolderContentCacheRootDirectory(),
                        SearchOption.AllDirectories,
                        cancellationToken);
                    CottonStorageCategorySnapshot downloadedFiles = ScanDirectory(
                        DownloadedFilesName,
                        CottonMobileStoragePaths.CreateDownloadsDirectory(),
                        SearchOption.AllDirectories,
                        cancellationToken,
                        includeTemporaryDownloads: false);
                    CottonOfflineStorageScan offlineFiles = ScanOfflineFiles(cancellationToken);
                    CottonStorageCategorySnapshot evictableDownloads = ScanDirectory(
                        EvictableDownloadsName,
                        CottonMobileStoragePaths.CreateDownloadsDirectory(),
                        SearchOption.AllDirectories,
                        cancellationToken,
                        includeTemporaryDownloads: false,
                        excludedDirectories: offlineFiles.ProtectedDownloadDirectories);
                    int protectedOfflineFileCount =
                        offlineFiles.AvailableFileCount
                        + offlineFiles.StaleFileCount
                        + offlineFiles.MissingFileCount;
                    long protectedOfflineBytes = offlineFiles.AvailableBytes + offlineFiles.StaleBytes;

                    return new CottonStorageSummary(
                        thumbnails,
                        folderListings,
                        downloadedFiles,
                        CottonOnDeviceStorageSummary.Create(
                            offlineFiles.AvailableFileCount,
                            offlineFiles.AvailableBytes,
                            offlineFiles.StaleFileCount,
                            offlineFiles.StaleBytes,
                            offlineFiles.MissingFileCount,
                            folderListings.FileCount,
                            folderListings.SizeBytes,
                            thumbnails.FileCount,
                            thumbnails.SizeBytes),
                        CottonStorageBudgetSummary.Create(
                            evictableDownloads.FileCount,
                            evictableDownloads.SizeBytes,
                            _downloadOptions.MaxCacheBytes,
                            thumbnails.FileCount,
                            thumbnails.SizeBytes,
                            _thumbnailOptions.MaxCacheBytes,
                            folderListings.FileCount,
                            folderListings.SizeBytes,
                            FolderListingBudgetBytes,
                            protectedOfflineFileCount,
                            protectedOfflineBytes));
                },
                cancellationToken);
        }

        public Task ClearThumbnailCacheAsync(CancellationToken cancellationToken = default)
        {
            return ClearDirectoryAsync(
                CottonMobileStoragePaths.CreateThumbnailCacheDirectory(_thumbnailOptions),
                cancellationToken,
                includeTemporaryThumbnails: false);
        }

        public Task ClearFolderListingsCacheAsync(CancellationToken cancellationToken = default)
        {
            return ClearDirectoryAsync(
                CottonMobileStoragePaths.CreateFolderContentCacheRootDirectory(),
                cancellationToken);
        }

        public async Task ClearDownloadedFilesAsync(CancellationToken cancellationToken = default)
        {
            bool deletedFile = false;
            try
            {
                await ClearDirectoryAsync(
                    CottonMobileStoragePaths.CreateDownloadsDirectory(),
                    cancellationToken,
                    includeTemporaryDownloads: false,
                    onFileDeleted: () => deletedFile = true).ConfigureAwait(false);
                await ClearDirectoryAsync(
                    CottonMobileStoragePaths.CreateOfflineFileMetadataRootDirectory(),
                    cancellationToken).ConfigureAwait(false);
                NotifyDownloadedFilesCleared();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                if (deletedFile)
                {
                    _logger.LogDebug(
                        exception,
                        "Cotton mobile downloaded-file cleanup deleted files before failing.");
                    NotifyDownloadedFilesCleared();
                }

                throw;
            }
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
                ClearFolderListingsCacheAsync,
                "folder listings",
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
            bool includeTemporaryThumbnails = true,
            bool includeTemporaryDownloads = true,
            Action? onFileDeleted = null)
        {
            return Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Directory.Exists(directory))
                    {
                        return;
                    }

                    List<Exception> failures = [];
                    DateTime utcNow = DateTime.UtcNow;
                    foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!includeTemporaryDownloads
                            && CottonMobileStoragePaths.IsTemporaryDownloadPath(file)
                            && !IsAbandonedTemporaryStorageFile(file, utcNow))
                        {
                            continue;
                        }

                        if (!includeTemporaryThumbnails
                            && IsTemporaryThumbnailPath(file)
                            && !IsAbandonedTemporaryStorageFile(file, utcNow))
                        {
                            continue;
                        }

                        if (DeleteFile(file, failures))
                        {
                            onFileDeleted?.Invoke();
                        }
                    }

                    DeleteEmptyDirectories(directory, cancellationToken);
                    if (failures.Count == 1)
                    {
                        throw new InvalidOperationException("Failed to delete one Cotton mobile storage file.", failures[0]);
                    }

                    if (failures.Count > 1)
                    {
                        throw new AggregateException("Failed to delete Cotton mobile storage files.", failures);
                    }
                },
                cancellationToken);
        }

        private CottonStorageCategorySnapshot ScanDirectory(
            string name,
            string directory,
            SearchOption searchOption,
            CancellationToken cancellationToken,
            bool includeTemporaryThumbnails = true,
            bool includeTemporaryDownloads = true,
            IReadOnlyCollection<string>? excludedDirectories = null)
        {
            if (!Directory.Exists(directory))
            {
                return new CottonStorageCategorySnapshot(name, 0, 0);
            }

            IReadOnlyList<string> normalizedExcludedDirectories = NormalizeExcludedDirectories(excludedDirectories);
            long sizeBytes = 0;
            int fileCount = 0;
            try
            {
                foreach (string file in Directory.EnumerateFiles(directory, "*", searchOption))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (IsInExcludedDirectory(file, normalizedExcludedDirectories))
                    {
                        continue;
                    }

                    if (!includeTemporaryDownloads && CottonMobileStoragePaths.IsTemporaryDownloadPath(file))
                    {
                        continue;
                    }

                    if (!includeTemporaryThumbnails && IsTemporaryThumbnailPath(file))
                    {
                        continue;
                    }

                    if (TryReadStorageFile(name, file, out long fileSizeBytes))
                    {
                        sizeBytes += fileSizeBytes;
                        fileCount++;
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(
                    exception,
                    "Failed to finish scanning Cotton mobile {StorageCategoryName} storage directory {Directory}.",
                    name,
                    directory);
            }

            return new CottonStorageCategorySnapshot(name, sizeBytes, fileCount);
        }

        private CottonOfflineStorageScan ScanOfflineFiles(CancellationToken cancellationToken)
        {
            string metadataRootDirectory = CottonMobileStoragePaths.CreateOfflineFileMetadataRootDirectory();
            if (!Directory.Exists(metadataRootDirectory))
            {
                return CottonOfflineStorageScan.Empty;
            }

            var scan = new CottonOfflineStorageScan();
            var seenPins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<string> manifestPaths;
            try
            {
                manifestPaths = Directory
                    .EnumerateFiles(
                        metadataRootDirectory,
                        FileSystemCottonOfflineFilePinStore.MetadataFileName,
                        SearchOption.AllDirectories)
                    .ToList();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(
                    exception,
                    "Failed to enumerate Cotton mobile offline file metadata directory {Directory}.",
                    metadataRootDirectory);
                return scan;
            }

            foreach (string manifestPath in manifestPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? instanceStorageKey = Path.GetFileName(Path.GetDirectoryName(manifestPath));
                if (string.IsNullOrWhiteSpace(instanceStorageKey))
                {
                    continue;
                }

                foreach (CottonOfflineStoragePin pin in ReadOfflinePins(manifestPath, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string pinKey = $"{instanceStorageKey}:{pin.FileId:D}";
                    if (!seenPins.Add(pinKey))
                    {
                        continue;
                    }

                    scan.ProtectedDownloadDirectories.Add(
                        Path.Combine(
                            CottonMobileStoragePaths.CreateDownloadsDirectory(),
                            instanceStorageKey,
                            pin.FileId.ToString("D")));
                    AddOfflinePin(scan, instanceStorageKey, pin);
                }
            }

            return scan;
        }

        private IReadOnlyList<CottonOfflineStoragePin> ReadOfflinePins(
            string manifestPath,
            CancellationToken cancellationToken)
        {
            try
            {
                using FileStream stream = File.OpenRead(manifestPath);
                using JsonDocument document = JsonDocument.Parse(stream);
                cancellationToken.ThrowIfCancellationRequested();

                if (!document.RootElement.TryGetProperty("schemaVersion", out JsonElement schemaVersion)
                    || !schemaVersion.TryGetInt32(out int parsedSchemaVersion)
                    || parsedSchemaVersion != 1
                    || !document.RootElement.TryGetProperty("items", out JsonElement items)
                    || items.ValueKind != JsonValueKind.Array)
                {
                    return [];
                }

                var pins = new List<CottonOfflineStoragePin>();
                foreach (JsonElement item in items.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CottonOfflineStoragePin? pin = TryCreateOfflinePin(item);
                    if (pin is not null)
                    {
                        pins.Add(pin);
                    }
                }

                return pins;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or InvalidOperationException)
            {
                _logger.LogDebug(
                    exception,
                    "Failed to inspect Cotton mobile offline file metadata {Path}.",
                    manifestPath);
                return [];
            }
        }

        private CottonOfflineStoragePin? TryCreateOfflinePin(JsonElement item)
        {
            try
            {
                if (!item.TryGetProperty("fileId", out JsonElement fileIdElement)
                    || !Guid.TryParse(fileIdElement.GetString(), out Guid fileId)
                    || fileId == Guid.Empty
                    || !item.TryGetProperty("remoteUpdatedAtUtc", out JsonElement remoteUpdatedAtElement)
                    || !remoteUpdatedAtElement.TryGetDateTime(out DateTime remoteUpdatedAtUtc))
                {
                    return null;
                }

                long? sizeBytes = null;
                if (item.TryGetProperty("sizeBytes", out JsonElement sizeElement)
                    && sizeElement.ValueKind == JsonValueKind.Number
                    && sizeElement.TryGetInt64(out long parsedSize)
                    && parsedSize >= 0)
                {
                    sizeBytes = parsedSize;
                }

                return new CottonOfflineStoragePin(fileId, remoteUpdatedAtUtc, sizeBytes);
            }
            catch (Exception exception) when (exception is InvalidOperationException or FormatException)
            {
                return null;
            }
        }

        private void AddOfflinePin(
            CottonOfflineStorageScan scan,
            string instanceStorageKey,
            CottonOfflineStoragePin pin)
        {
            FileInfo? localFile = TryGetOfflineLocalFile(instanceStorageKey, pin.FileId);
            if (localFile is null)
            {
                scan.MissingFileCount++;
                return;
            }

            try
            {
                bool sizeMatches = !pin.SizeBytes.HasValue || pin.SizeBytes.Value == localFile.Length;
                bool isFresh = CottonLocalFileFreshness.IsFresh(localFile.LastWriteTimeUtc, pin.RemoteUpdatedAtUtc);
                if (sizeMatches && isFresh)
                {
                    scan.AvailableFileCount++;
                    scan.AvailableBytes += localFile.Length;
                    return;
                }

                scan.StaleFileCount++;
                scan.StaleBytes += localFile.Length;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(
                    exception,
                    "Failed to classify Cotton mobile offline local file {FileId}.",
                    pin.FileId);
                scan.MissingFileCount++;
            }
        }

        private FileInfo? TryGetOfflineLocalFile(string instanceStorageKey, Guid fileId)
        {
            try
            {
                string directory = Path.Combine(
                    CottonMobileStoragePaths.CreateDownloadsDirectory(),
                    instanceStorageKey,
                    fileId.ToString("D"));
                if (!Directory.Exists(directory))
                {
                    return null;
                }

                return Directory
                    .EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                    .Where(file => !CottonMobileStoragePaths.IsTemporaryDownloadPath(file))
                    .Select(file => new FileInfo(file))
                    .Where(file => file.Exists)
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(
                    exception,
                    "Failed to inspect Cotton mobile offline local file {FileId}.",
                    fileId);
                return null;
            }
        }

        private bool TryReadStorageFile(string storageCategoryName, string file, out long sizeBytes)
        {
            sizeBytes = 0;

            try
            {
                var info = new FileInfo(file);
                if (!info.Exists)
                {
                    return false;
                }

                sizeBytes = info.Length;
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(
                    exception,
                    "Failed to inspect Cotton mobile {StorageCategoryName} storage file {Path}.",
                    storageCategoryName,
                    file);

                return false;
            }
        }

        private static bool IsTemporaryThumbnailPath(string path)
        {
            return path.EndsWith(TemporaryThumbnailFileExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<string> NormalizeExcludedDirectories(IReadOnlyCollection<string>? directories)
        {
            if (directories is null || directories.Count == 0)
            {
                return [];
            }

            return directories
                .Where(directory => !string.IsNullOrWhiteSpace(directory))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static bool IsInExcludedDirectory(string path, IReadOnlyList<string> excludedDirectories)
        {
            if (excludedDirectories.Count == 0)
            {
                return false;
            }

            string fullPath = Path.GetFullPath(path);
            return excludedDirectories.Any(directory => IsPathInsideDirectory(fullPath, directory));
        }

        private static bool IsPathInsideDirectory(string path, string directory)
        {
            string relativePath = Path.GetRelativePath(directory, path);
            return !Path.IsPathRooted(relativePath)
                && !string.Equals(relativePath, ".", StringComparison.Ordinal)
                && !string.Equals(relativePath, "..", StringComparison.Ordinal)
                && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
        }

        private bool IsAbandonedTemporaryStorageFile(string file, DateTime utcNow)
        {
            try
            {
                var info = new FileInfo(file);
                return info.Exists && CottonTemporaryFilePolicy.IsAbandoned(info, utcNow);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Failed to inspect Cotton mobile temporary storage file {Path}.", file);
                return false;
            }
        }

        private bool DeleteFile(string file, List<Exception> failures)
        {
            try
            {
                File.Delete(file);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(exception, "Failed to delete Cotton mobile storage file {Path}.", file);
                failures.Add(exception);
                return false;
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

        private void NotifyDownloadedFilesCleared()
        {
            EventHandler? handlers = DownloadedFilesCleared;
            if (handlers is null)
            {
                return;
            }

            foreach (EventHandler handler in handlers.GetInvocationList().Cast<EventHandler>())
            {
                try
                {
                    handler.Invoke(this, EventArgs.Empty);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Cotton mobile downloaded-files-cleared subscriber failed.");
                }
            }
        }

        private sealed class CottonOfflineStorageScan
        {
            public static CottonOfflineStorageScan Empty => new();

            public int AvailableFileCount { get; set; }

            public long AvailableBytes { get; set; }

            public int StaleFileCount { get; set; }

            public long StaleBytes { get; set; }

            public int MissingFileCount { get; set; }

            public HashSet<string> ProtectedDownloadDirectories { get; } = new(StringComparer.Ordinal);
        }

        private sealed class CottonOfflineStoragePin
        {
            public CottonOfflineStoragePin(
                Guid fileId,
                DateTime remoteUpdatedAtUtc,
                long? sizeBytes)
            {
                FileId = fileId;
                RemoteUpdatedAtUtc = CottonLocalFileFreshness.NormalizeUtc(remoteUpdatedAtUtc);
                SizeBytes = sizeBytes;
            }

            public Guid FileId { get; }

            public DateTime RemoteUpdatedAtUtc { get; }

            public long? SizeBytes { get; }
        }
    }
}
