// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonSyncedFileManifestStore(
        ICottonSyncedFileManifestPathProvider pathProvider,
        ILogger<FileSystemCottonSyncedFileManifestStore> logger,
        TimeProvider timeProvider) : ICottonSyncedFileManifestStore
    {
        public const string MetadataFileName = "synced-files.json";

        private static readonly Action<ILogger, string, Exception?> LogLoadFailed = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(LoadAsync)),
            "Failed to load the synced-file manifest from {FilePath}.");
        private static readonly Action<ILogger, string, Exception?> LogSaveFailed = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, nameof(SaveAsync)),
            "Failed to save the synced-file manifest to {FilePath}.");

        private readonly ICottonSyncedFileManifestPathProvider _pathProvider =
            pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        private readonly ILogger<FileSystemCottonSyncedFileManifestStore> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        public async Task<IReadOnlyList<CottonSyncedFileSnapshot>> LoadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);

            string filePath = CreateMetadataFilePath(instanceUri, root);
            if (!File.Exists(filePath))
            {
                return [];
            }

            try
            {
                CottonStoredSyncedFileManifest? stored = await CottonAtomicJsonFile
                    .ReadAsync<CottonStoredSyncedFileManifest>(filePath, cancellationToken)
                    .ConfigureAwait(false);

                if (stored is null
                    || stored.SchemaVersion != CottonSyncedFileManifestSchema.CurrentVersion
                    || !string.Equals(stored.SyncRootStableKey, root.StableKey, StringComparison.Ordinal)
                    || stored.Items is null)
                {
                    throw new InvalidDataException("The synced-file manifest is invalid for this sync root.");
                }

                return DeduplicateItems(stored.Items.Select(CreateSyncedFile));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or NotSupportedException
                    or ArgumentException)
            {
                LogLoadFailed(_logger, filePath, exception);
                throw;
            }
        }

        public async Task SaveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            IReadOnlyCollection<CottonSyncedFileSnapshot> items,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(items);

            string filePath = CreateMetadataFilePath(instanceUri, root);

            try
            {
                await CottonAtomicJsonFile
                    .WriteAsync(filePath, CreateStoredManifest(root, items), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                LogSaveFailed(_logger, filePath, exception);
                throw;
            }
        }

        public async Task AddOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonSyncedFileSnapshot item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);

            IReadOnlyList<CottonSyncedFileSnapshot> current =
                await LoadAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            List<CottonSyncedFileSnapshot> updated = [.. current
                .Where(existing =>
                    existing.FileId != item.FileId
                    && !string.Equals(existing.RelativePath, item.RelativePath, StringComparison.OrdinalIgnoreCase))];
            updated.Add(item);

            await SaveAsync(instanceUri, root, updated, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            Guid fileId,
            CancellationToken cancellationToken = default)
        {
            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("File id is required.", nameof(fileId));
            }

            IReadOnlyList<CottonSyncedFileSnapshot> current =
                await LoadAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            List<CottonSyncedFileSnapshot> updated = [.. current.Where(existing => existing.FileId != fileId)];
            if (updated.Count == current.Count)
            {
                return false;
            }

            await SaveAsync(instanceUri, root, updated, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public Task ClearAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            cancellationToken.ThrowIfCancellationRequested();

            CottonAtomicJsonFile.DeleteIfExists(CreateMetadataFilePath(instanceUri, root));
            return Task.CompletedTask;
        }

        private string CreateMetadataFilePath(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return Path.Combine(
                _pathProvider.CreateSyncedFileManifestDirectory(instanceUri, root),
                MetadataFileName);
        }

        private CottonStoredSyncedFileManifest CreateStoredManifest(
            CottonSyncRootSnapshot root,
            IReadOnlyCollection<CottonSyncedFileSnapshot> items)
        {
            return new CottonStoredSyncedFileManifest
            {
                SchemaVersion = CottonSyncedFileManifestSchema.CurrentVersion,
                SyncRootStableKey = root.StableKey,
                SavedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                Items = [.. DeduplicateItems(items).Select<CottonSyncedFileSnapshot, CottonStoredSyncedFileItem?>(CreateStoredItem)],
            };
        }

        private static List<CottonSyncedFileSnapshot> DeduplicateItems(
            IEnumerable<CottonSyncedFileSnapshot> items)
        {
            List<CottonSyncedFileSnapshot> source = [.. items];
            HashSet<Guid> fileIds = [];
            HashSet<string> relativePaths = new(StringComparer.OrdinalIgnoreCase);
            List<CottonSyncedFileSnapshot> result = new(source.Count);

            for (int index = source.Count - 1; index >= 0; index--)
            {
                CottonSyncedFileSnapshot item = source[index];
                if (fileIds.Add(item.FileId) && relativePaths.Add(item.RelativePath))
                {
                    result.Add(item);
                }
            }

            result.Reverse();
            return result;
        }

        private static CottonStoredSyncedFileItem CreateStoredItem(CottonSyncedFileSnapshot item)
        {
            return new CottonStoredSyncedFileItem
            {
                FileId = item.FileId,
                FileName = item.FileName,
                RelativePath = item.RelativePath,
                ETag = item.ETag,
                RemoteUpdatedAtUtc = item.RemoteUpdatedAtUtc,
                SizeBytes = item.SizeBytes,
                ContentType = item.ContentType,
                SyncedAtUtc = item.SyncedAtUtc,
                ContentHash = item.ContentHash,
            };
        }

        private static CottonSyncedFileSnapshot CreateSyncedFile(CottonStoredSyncedFileItem? item)
        {
            if (item is null)
            {
                throw new InvalidDataException("The synced-file manifest contains an empty item.");
            }

            return new CottonSyncedFileSnapshot(
                item.FileId,
                item.FileName ?? string.Empty,
                item.ETag ?? string.Empty,
                item.RemoteUpdatedAtUtc,
                item.SizeBytes,
                item.ContentType,
                item.SyncedAtUtc,
                item.RelativePath ?? string.Empty,
                item.ContentHash);
        }
    }
}
