// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonSyncRootStore : ICottonSyncRootStore
    {
        private const int SchemaVersion = 1;
        private const string TemporaryFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public const string MetadataFileName = "sync-roots.json";

        private readonly ICottonSyncRootMetadataPathProvider _pathProvider;
        private readonly ILogger<FileSystemCottonSyncRootStore> _logger;

        public FileSystemCottonSyncRootStore(
            ICottonSyncRootMetadataPathProvider pathProvider,
            ILogger<FileSystemCottonSyncRootStore> logger)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);
            ArgumentNullException.ThrowIfNull(logger);

            _pathProvider = pathProvider;
            _logger = logger;
        }

        public async Task<IReadOnlyList<CottonSyncRootSnapshot>> LoadAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            string filePath = CreateMetadataFilePath(instanceUri);
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
                CottonStoredSyncRootCollection? stored =
                    await JsonSerializer.DeserializeAsync<CottonStoredSyncRootCollection>(
                        stream,
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                if (stored is null
                    || stored.SchemaVersion != SchemaVersion
                    || stored.Items is null)
                {
                    DeleteFile(filePath);
                    return [];
                }

                return CottonSyncRootStoreMapper.Deduplicate(stored.Items
                    .Select(item => CottonSyncRootStoreMapper.TryCreateSyncRoot(instanceUri, item))
                    .Where(item => item is not null)
                    .Select(item => item!)
                    .ToList());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                _logger.LogWarning(exception, "Failed to load sync roots from {FilePath}; resetting the store.", filePath);
                DeleteFile(filePath);
                return [];
            }
        }

        public async Task SaveAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonSyncRootSnapshot> roots,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(roots);
            CottonSyncRootStoreMapper.EnsureRootsMatchInstance(instanceUri, roots);

            string directory = _pathProvider.CreateSyncRootMetadataDirectory(instanceUri);
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
                        CottonSyncRootStoreMapper.CreateStoredCollection(roots, SchemaVersion),
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
                _logger.LogError(exception, "Failed to save sync roots to {FilePath}.", filePath);
                DeleteFile(temporaryFilePath);
                throw;
            }
        }

        public async Task AddOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(root);
            CottonSyncRootStoreMapper.EnsureRootsMatchInstance(instanceUri, [root]);

            IReadOnlyList<CottonSyncRootSnapshot> current =
                await LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonSyncRootSnapshot> updated = current
                .Where(existing => existing.Id != root.Id)
                .Where(existing => !string.Equals(existing.StableKey, root.StableKey, StringComparison.Ordinal))
                .ToList();
            updated.Add(root);

            await SaveAsync(instanceUri, updated, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> RemoveAsync(
            Uri instanceUri,
            Guid rootId,
            CancellationToken cancellationToken = default)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            IReadOnlyList<CottonSyncRootSnapshot> current =
                await LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonSyncRootSnapshot> updated = current
                .Where(root => root.Id != rootId)
                .ToList();
            if (updated.Count == current.Count)
            {
                return false;
            }

            await SaveAsync(instanceUri, updated, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();

            DeleteFile(CreateMetadataFilePath(instanceUri));
            return Task.CompletedTask;
        }

        private string CreateMetadataFilePath(Uri instanceUri)
        {
            return Path.Combine(_pathProvider.CreateSyncRootMetadataDirectory(instanceUri), MetadataFileName);
        }

        private static string CreateTemporaryFilePath(string filePath)
        {
            return $"{filePath}.{Guid.NewGuid():N}{TemporaryFileExtension}";
        }

        private void DeleteFile(string filePath)
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
                _logger.LogError(exception, "Failed to delete sync-root store file {FilePath}.", filePath);
                throw;
            }
        }
    }
}
