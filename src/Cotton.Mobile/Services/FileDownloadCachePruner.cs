// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;
using System.Text.Json;

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
            List<string> protectedDirectories = pins
                .Select(pin => CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, pin.FileId))
                .ToList();
            protectedDirectories.AddRange(LoadSyncedManifestDownloadDirectories(instanceUri, cancellationToken));
            return protectedDirectories;
        }

        private IReadOnlyCollection<string> LoadSyncedManifestDownloadDirectories(
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            string instanceManifestDirectory = Path.Combine(
                CottonMobileStoragePaths.CreateSyncedFileManifestRootDirectory(),
                CottonMobileStoragePaths.CreateInstanceStorageKey(instanceUri));
            if (!Directory.Exists(instanceManifestDirectory))
            {
                return [];
            }

            var protectedDirectories = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                foreach (string manifestPath in Directory.EnumerateFiles(
                    instanceManifestDirectory,
                    FileSystemCottonSyncedFileManifestStore.MetadataFileName,
                    SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (Guid fileId in ReadSyncedManifestFileIds(manifestPath, cancellationToken))
                    {
                        protectedDirectories.Add(CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, fileId));
                    }
                }
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
                    "Failed to inspect Cotton mobile synced-file manifests under {Directory}.",
                    instanceManifestDirectory);
            }

            return protectedDirectories;
        }

        private IReadOnlyList<Guid> ReadSyncedManifestFileIds(
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

                var fileIds = new List<Guid>();
                foreach (JsonElement item in items.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (item.TryGetProperty("fileId", out JsonElement fileIdElement)
                        && Guid.TryParse(fileIdElement.GetString(), out Guid fileId)
                        && fileId != Guid.Empty)
                    {
                        fileIds.Add(fileId);
                    }
                }

                return fileIds;
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
                    "Failed to inspect Cotton mobile synced-file manifest {Path}.",
                    manifestPath);
                return [];
            }
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
                    ResolvePruneTimestamp(file),
                    CottonSensitiveFileCachePolicy.IsSensitiveFile(file.Name, contentType: null)))
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
