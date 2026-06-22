// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonSyncedFileManifestStore : ICottonSyncedFileManifestStore
    {
        private const int SchemaVersion = 2;
        private const string TemporaryFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public const string MetadataFileName = "synced-files.json";

        private readonly ICottonSyncedFileManifestPathProvider _pathProvider;

        public FileSystemCottonSyncedFileManifestStore(ICottonSyncedFileManifestPathProvider pathProvider)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);

            _pathProvider = pathProvider;
        }

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
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 16384,
                    useAsync: true);
                CottonStoredSyncedFileManifest? stored =
                    await JsonSerializer.DeserializeAsync<CottonStoredSyncedFileManifest>(
                        stream,
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                if (stored is null
                    || stored.SchemaVersion != SchemaVersion
                    || !string.Equals(stored.SyncRootStableKey, root.StableKey, StringComparison.Ordinal)
                    || stored.Items is null)
                {
                    DeleteFile(filePath);
                    return [];
                }

                return DeduplicateItems(stored.Items
                    .Select(TryCreateSyncedFile)
                    .Where(item => item is not null)
                    .Select(item => item!));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                DeleteFile(filePath);
                return [];
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

            string directory = _pathProvider.CreateSyncedFileManifestDirectory(instanceUri, root);
            string filePath = Path.Combine(directory, MetadataFileName);
            string temporaryFilePath = CreateTemporaryFilePath(filePath);

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
                        CreateStoredManifest(root, items),
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryFilePath, filePath, overwrite: true);
            }
            catch (OperationCanceledException)
            {
                DeleteFile(temporaryFilePath);
                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                DeleteFile(temporaryFilePath);
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
            List<CottonSyncedFileSnapshot> updated = current
                .Where(existing =>
                    existing.FileId != item.FileId
                    && !string.Equals(existing.RelativePath, item.RelativePath, StringComparison.OrdinalIgnoreCase))
                .ToList();
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
            List<CottonSyncedFileSnapshot> updated = current
                .Where(existing => existing.FileId != fileId)
                .ToList();
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

            DeleteFile(CreateMetadataFilePath(instanceUri, root));
            return Task.CompletedTask;
        }

        private string CreateMetadataFilePath(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return Path.Combine(
                _pathProvider.CreateSyncedFileManifestDirectory(instanceUri, root),
                MetadataFileName);
        }

        private static CottonStoredSyncedFileManifest CreateStoredManifest(
            CottonSyncRootSnapshot root,
            IReadOnlyCollection<CottonSyncedFileSnapshot> items)
        {
            return new CottonStoredSyncedFileManifest
            {
                SchemaVersion = SchemaVersion,
                SyncRootStableKey = root.StableKey,
                SavedAtUtc = DateTime.UtcNow,
                Items = DeduplicateItems(items)
                    .Select(CreateStoredItem)
                    .ToList(),
            };
        }

        private static IReadOnlyList<CottonSyncedFileSnapshot> DeduplicateItems(
            IEnumerable<CottonSyncedFileSnapshot> items)
        {
            List<CottonSyncedFileSnapshot> source = items.ToList();
            var fileIds = new HashSet<Guid>();
            var relativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<CottonSyncedFileSnapshot>(source.Count);

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
            };
        }

        private static CottonSyncedFileSnapshot? TryCreateSyncedFile(CottonStoredSyncedFileItem item)
        {
            try
            {
                return new CottonSyncedFileSnapshot(
                    item.FileId,
                    item.FileName ?? string.Empty,
                    item.ETag ?? string.Empty,
                    item.RemoteUpdatedAtUtc,
                    item.SizeBytes,
                    item.ContentType,
                    item.SyncedAtUtc,
                    item.RelativePath ?? string.Empty);
            }
            catch (Exception exception)
                when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static string CreateTemporaryFilePath(string filePath)
        {
            return $"{filePath}.{Guid.NewGuid():N}{TemporaryFileExtension}";
        }

        private static void DeleteFile(string filePath)
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
            }
        }
    }
}
