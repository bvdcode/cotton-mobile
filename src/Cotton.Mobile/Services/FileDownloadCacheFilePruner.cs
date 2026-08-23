// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileDownloadCacheFilePruner(
        FileDownloadCacheOptions options,
        ILogger<FileDownloadCacheFilePruner> logger,
        TimeProvider timeProvider)
    {
        private readonly FileDownloadCacheOptions _options =
            options ?? throw new ArgumentNullException(nameof(options));
        private readonly ILogger<FileDownloadCacheFilePruner> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        public void Prune(
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
                CottonLog.Debug(_logger, "Failed to prune Cotton mobile download cache.", exception);
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

            DeleteAbandonedTemporaryDownloads(rootDirectory, cancellationToken);
            IReadOnlyList<string> deletePaths = CottonFileDownloadCachePrunePlanner.SelectFilesToDelete(
                LoadEntries(rootDirectory),
                _options.MaxCacheBytes,
                NormalizeProtectedPath(protectedPath),
                protectedDirectories);
            foreach (string deletePath in deletePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TryDeleteFile(new FileInfo(deletePath));
            }

            DeleteEmptyDirectories(rootDirectory, cancellationToken);
        }

        private static List<CottonFileDownloadCacheEntry> LoadEntries(string rootDirectory)
        {
            return [.. Directory
                .EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories)
                .Where(path => !CottonMobileStoragePaths.IsTemporaryDownloadPath(path))
                .Select(path => new FileInfo(path))
                .Where(file => file.Exists)
                .Select(file => new CottonFileDownloadCacheEntry(
                    file.FullName,
                    file.Length,
                    CottonTemporaryFilePolicy.ResolveActivityTimestampUtc(file),
                    CottonSensitiveFileCachePolicy.IsSensitiveFile(file.Name, contentType: null)))];
        }

        private void DeleteAbandonedTemporaryDownloads(
            string rootDirectory,
            CancellationToken cancellationToken)
        {
            DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;
            foreach (string path in Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!CottonMobileStoragePaths.IsTemporaryDownloadPath(path))
                {
                    continue;
                }

                FileInfo file = new(path);
                if (file.Exists && CottonTemporaryFilePolicy.IsAbandoned(file, utcNow))
                {
                    TryDeleteFile(file);
                }
            }
        }

        private static string? NormalizeProtectedPath(string? protectedPath)
        {
            return string.IsNullOrWhiteSpace(protectedPath)
                ? null
                : Path.GetFullPath(protectedPath);
        }

        private void TryDeleteFile(FileInfo file)
        {
            try
            {
                file.Delete();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                CottonLog.DebugWithContext(
                    _logger,
                    "Failed to prune a Cotton mobile downloaded file.",
                    file.FullName,
                    exception);
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
                CottonLog.DebugWithContext(
                    _logger,
                    "Failed to prune an empty Cotton mobile download directory.",
                    directory,
                    exception);
            }
        }
    }
}
