// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonFolderContentCache : ICottonFolderContentCache
    {
        private const int SchemaVersion = 2;
        private const string RootCacheFileName = "root.json";
        private const string FolderCacheFileExtension = ".json";
        private const string TemporaryCacheFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ILogger<FileSystemCottonFolderContentCache> _logger;

        public FileSystemCottonFolderContentCache(ILogger<FileSystemCottonFolderContentCache> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public Task SaveRootAsync(
            Uri instanceUri,
            CottonFolderContent content,
            CancellationToken cancellationToken = default)
        {
            return SaveAsync(instanceUri, RootCacheFileName, content, cancellationToken);
        }

        public Task<CottonFolderContent?> LoadRootAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            return LoadContentAsync(instanceUri, RootCacheFileName, cancellationToken);
        }

        public Task<CottonCachedFolderContentSnapshot?> LoadRootSnapshotAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            return LoadAsync(instanceUri, RootCacheFileName, cancellationToken);
        }

        public Task SaveFolderAsync(
            Uri instanceUri,
            CottonFolderContent content,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            return SaveAsync(instanceUri, CreateFolderCacheFileName(content.FolderId), content, cancellationToken);
        }

        public Task<CottonFolderContent?> LoadFolderAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(folder);

            return LoadContentAsync(instanceUri, CreateFolderCacheFileName(folder.Id), cancellationToken);
        }

        public Task<CottonCachedFolderContentSnapshot?> LoadFolderSnapshotAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(folder);

            return LoadAsync(instanceUri, CreateFolderCacheFileName(folder.Id), cancellationToken);
        }

        private async Task SaveAsync(
            Uri instanceUri,
            string fileName,
            CottonFolderContent content,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(content);

            string directory = CottonMobileStoragePaths.CreateFolderContentCacheDirectory(instanceUri);
            string filePath = Path.Combine(directory, fileName);
            string temporaryFilePath = CreateTemporaryCacheFilePath(filePath);

            try
            {
                Directory.CreateDirectory(directory);
                await using (var stream = new FileStream(
                    temporaryFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 16384,
                    useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(
                        stream,
                        CreateCachedContent(content),
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryFilePath, filePath, overwrite: true);
            }
            catch (OperationCanceledException)
            {
                DeleteTemporaryFile(temporaryFilePath);
                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                DeleteTemporaryFile(temporaryFilePath);
                _logger.LogDebug(
                    exception,
                    "Failed to save Cotton mobile folder listing cache {Path}.",
                    filePath);
            }
        }

        private async Task<CottonFolderContent?> LoadContentAsync(
            Uri instanceUri,
            string fileName,
            CancellationToken cancellationToken)
        {
            CottonCachedFolderContentSnapshot? snapshot =
                await LoadAsync(instanceUri, fileName, cancellationToken).ConfigureAwait(false);
            return snapshot?.Content;
        }

        private async Task<CottonCachedFolderContentSnapshot?> LoadAsync(
            Uri instanceUri,
            string fileName,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            string filePath = Path.Combine(
                CottonMobileStoragePaths.CreateFolderContentCacheDirectory(instanceUri),
                fileName);
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 16384,
                    useAsync: true);
                CottonCachedFolderContent? cachedContent = await JsonSerializer.DeserializeAsync<CottonCachedFolderContent>(
                    stream,
                    SerializerOptions,
                    cancellationToken).ConfigureAwait(false);
                CottonCachedFolderContentSnapshot? snapshot = CreateFolderContentSnapshot(cachedContent);
                if (snapshot is null)
                {
                    DeleteCacheFile(filePath);
                }

                return snapshot;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                _logger.LogDebug(
                    exception,
                    "Failed to load Cotton mobile folder listing cache {Path}.",
                    filePath);
                DeleteCacheFile(filePath);
                return null;
            }
        }

        private static CottonCachedFolderContent CreateCachedContent(CottonFolderContent content)
        {
            return new CottonCachedFolderContent
            {
                SchemaVersion = SchemaVersion,
                FolderId = content.FolderId,
                FolderName = content.FolderName,
                CachedAtUtc = DateTime.UtcNow,
                Entries = content.Entries.Select(CreateCachedEntry).ToList(),
            };
        }

        private static CottonCachedFileBrowserEntry CreateCachedEntry(CottonFileBrowserEntry entry)
        {
            return new CottonCachedFileBrowserEntry
            {
                Id = entry.Id,
                Type = entry.Type,
                Name = entry.Name,
                Kind = entry.Kind,
                Details = entry.Details,
                ActionLabel = entry.ActionLabel,
                BadgeText = entry.BadgeText,
                UpdatedAtUtc = entry.UpdatedAtUtc,
                SizeBytes = entry.SizeBytes,
                ContentType = entry.ContentType,
                PreviewHashEncryptedHex = entry.PreviewHashEncryptedHex,
                ETag = entry.ETag,
            };
        }

        private static CottonCachedFolderContentSnapshot? CreateFolderContentSnapshot(CottonCachedFolderContent? cachedContent)
        {
            if (cachedContent is null
                || cachedContent.SchemaVersion != SchemaVersion
                || cachedContent.FolderId == Guid.Empty
                || cachedContent.CachedAtUtc == default
                || cachedContent.Entries is null)
            {
                return null;
            }

            List<CottonFileBrowserEntry> entries = cachedContent
                .Entries
                .Where(IsUsableCachedEntry)
                .Select(entry => CottonFileBrowserEntry.CreateCached(
                    entry.Id,
                    entry.Type,
                    entry.Name,
                    entry.Kind,
                    entry.Details,
                    entry.ActionLabel,
                    entry.BadgeText,
                    entry.UpdatedAtUtc,
                    entry.SizeBytes,
                    entry.ContentType,
                    entry.PreviewHashEncryptedHex,
                    entry.ETag))
                .ToList();
            var content = new CottonFolderContent(cachedContent.FolderId, cachedContent.FolderName, entries);
            return new CottonCachedFolderContentSnapshot(content, cachedContent.CachedAtUtc);
        }

        private static bool IsUsableCachedEntry(CottonCachedFileBrowserEntry entry)
        {
            return entry.Id != Guid.Empty
                && Enum.IsDefined(typeof(CottonFileBrowserEntryType), entry.Type)
                && !string.IsNullOrWhiteSpace(entry.Name);
        }

        private static string CreateFolderCacheFileName(Guid folderId)
        {
            return $"{folderId:N}{FolderCacheFileExtension}";
        }

        private static string CreateTemporaryCacheFilePath(string filePath)
        {
            return $"{filePath}.{Guid.NewGuid():N}{TemporaryCacheFileExtension}";
        }

        private void DeleteTemporaryFile(string filePath)
        {
            DeleteFile(filePath, "temporary folder listing cache");
        }

        private void DeleteCacheFile(string filePath)
        {
            DeleteFile(filePath, "invalid folder listing cache");
        }

        private void DeleteFile(string filePath, string description)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(exception, "Failed to delete Cotton mobile {Description} {Path}.", description, filePath);
            }
        }
    }
}
