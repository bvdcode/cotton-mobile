// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonSyncRootPauseStore(
        ICottonSyncRootMetadataPathProvider pathProvider,
        ILogger<FileSystemCottonSyncRootPauseStore> logger,
        TimeProvider timeProvider) : ICottonSyncRootPauseStore
    {
        private const int SchemaVersion = 1;
        public const string MetadataFileName = "paused-sync-roots.json";

        private readonly ICottonSyncRootMetadataPathProvider _pathProvider =
            pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        private readonly ILogger<FileSystemCottonSyncRootPauseStore> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        public async Task<IReadOnlySet<Guid>> LoadPausedRootIdsAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            string filePath = CreateMetadataFilePath(instanceUri);
            if (!File.Exists(filePath))
            {
                return new HashSet<Guid>();
            }

            try
            {
                CottonStoredPausedSyncRootCollection? stored =
                    await CottonAtomicJsonFile
                        .ReadAsync<CottonStoredPausedSyncRootCollection>(filePath, cancellationToken)
                        .ConfigureAwait(false);
                if (stored is null
                    || stored.SchemaVersion != SchemaVersion
                    || stored.RootIds is null)
                {
                    CottonAtomicJsonFile.DeleteIfExists(filePath);
                    return new HashSet<Guid>();
                }

                return stored.RootIds
                    .Where(rootId => rootId != Guid.Empty)
                    .ToHashSet();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to load paused sync roots from {FilePath}; resetting the store.",
                    filePath);
                CottonAtomicJsonFile.DeleteIfExists(filePath);
                return new HashSet<Guid>();
            }
        }

        public async Task<bool> SetPausedAsync(
            Uri instanceUri,
            Guid rootId,
            bool isPaused,
            CancellationToken cancellationToken = default)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            HashSet<Guid> current =
                (await LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false)).ToHashSet();
            bool changed = isPaused
                ? current.Add(rootId)
                : current.Remove(rootId);
            if (!changed)
            {
                return false;
            }

            await SaveAsync(instanceUri, current, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();

            CottonAtomicJsonFile.DeleteIfExists(CreateMetadataFilePath(instanceUri));
            return Task.CompletedTask;
        }

        private async Task SaveAsync(
            Uri instanceUri,
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(rootIds);

            Guid[] validRootIds = rootIds
                .Where(rootId => rootId != Guid.Empty)
                .Distinct()
                .OrderBy(rootId => rootId.ToString("N"), StringComparer.Ordinal)
                .ToArray();

            string filePath = CreateMetadataFilePath(instanceUri);
            if (validRootIds.Length == 0)
            {
                CottonAtomicJsonFile.DeleteIfExists(filePath);
                return;
            }

            try
            {
                await CottonAtomicJsonFile
                    .WriteAsync(filePath, CreateStoredCollection(validRootIds), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.LogError(exception, "Failed to save paused sync roots to {FilePath}.", filePath);
                throw;
            }
        }

        private string CreateMetadataFilePath(Uri instanceUri)
        {
            return Path.Combine(_pathProvider.CreateSyncRootMetadataDirectory(instanceUri), MetadataFileName);
        }

        private CottonStoredPausedSyncRootCollection CreateStoredCollection(
            IReadOnlyCollection<Guid> rootIds)
        {
            return new CottonStoredPausedSyncRootCollection
            {
                SchemaVersion = SchemaVersion,
                SavedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                RootIds = rootIds.ToList(),
            };
        }

    }
}
