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

        private static readonly Action<ILogger, string, Exception?> LogLoadFailed = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(LoadAsync)),
            "Failed to load sync roots from {FilePath}.");
        private static readonly Action<ILogger, string, Exception?> LogSaveFailed = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, nameof(SaveAsync)),
            "Failed to save sync roots to {FilePath}.");

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
                    throw new InvalidDataException("The sync-root metadata is invalid.");
                }

                return CottonSyncRootStoreMapper.Deduplicate([.. stored.Items
                    .Select(item => CottonSyncRootStoreMapper.CreateSyncRoot(instanceUri, item))]);
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
                LogSaveFailed(_logger, filePath, exception);
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
            List<CottonSyncRootSnapshot> updated = [.. current
                .Where(existing => existing.Id != root.Id)
                .Where(existing => !string.Equals(existing.StableKey, root.StableKey, StringComparison.Ordinal))];
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
            List<CottonSyncRootSnapshot> updated = [.. current.Where(root => root.Id != rootId)];
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
