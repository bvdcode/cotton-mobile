// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonSyncRootStore(
        ICottonSyncRootMetadataPathProvider pathProvider,
        ILogger<FileSystemCottonSyncRootStore> logger,
        TimeProvider timeProvider) : ICottonSyncRootStore
    {
        private const int SchemaVersion = 1;
        public const string MetadataFileName = "sync-roots.json";

        private readonly ICottonSyncRootMetadataPathProvider _pathProvider =
            pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        private readonly ILogger<FileSystemCottonSyncRootStore> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

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
                CottonStoredSyncRootCollection? stored =
                    await CottonAtomicJsonFile
                        .ReadAsync<CottonStoredSyncRootCollection>(filePath, cancellationToken)
                        .ConfigureAwait(false);
                if (stored is null
                    || stored.SchemaVersion != SchemaVersion
                    || stored.Items is null)
                {
                    CottonAtomicJsonFile.DeleteIfExists(filePath);
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
                CottonAtomicJsonFile.DeleteIfExists(filePath);
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

            string filePath = CreateMetadataFilePath(instanceUri);

            try
            {
                await CottonAtomicJsonFile
                    .WriteAsync(
                        filePath,
                        CottonSyncRootStoreMapper.CreateStoredCollection(
                            roots,
                            SchemaVersion,
                            _timeProvider.GetUtcNow().UtcDateTime),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.LogError(exception, "Failed to save sync roots to {FilePath}.", filePath);
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

            CottonAtomicJsonFile.DeleteIfExists(CreateMetadataFilePath(instanceUri));
            return Task.CompletedTask;
        }

        private string CreateMetadataFilePath(Uri instanceUri)
        {
            return Path.Combine(_pathProvider.CreateSyncRootMetadataDirectory(instanceUri), MetadataFileName);
        }

    }
}
