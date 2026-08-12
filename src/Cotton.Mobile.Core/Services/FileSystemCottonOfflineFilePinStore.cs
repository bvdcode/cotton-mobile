// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonOfflineFilePinStore : ICottonOfflineFilePinStore
    {
        private const int SchemaVersion = 1;
        public const string MetadataFileName = "offline-files.json";

        private readonly ICottonOfflineFileMetadataPathProvider _pathProvider;
        private readonly ILogger<FileSystemCottonOfflineFilePinStore> _logger;

        public FileSystemCottonOfflineFilePinStore(
            ICottonOfflineFileMetadataPathProvider pathProvider,
            ILogger<FileSystemCottonOfflineFilePinStore> logger)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);
            ArgumentNullException.ThrowIfNull(logger);

            _pathProvider = pathProvider;
            _logger = logger;
        }

        public async Task<IReadOnlyList<CottonOfflineFilePinSnapshot>> LoadAsync(
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
                CottonStoredOfflineFilePinManifest? stored =
                    await CottonAtomicJsonFile
                        .ReadAsync<CottonStoredOfflineFilePinManifest>(filePath, cancellationToken)
                        .ConfigureAwait(false);
                if (stored is null
                    || stored.SchemaVersion != SchemaVersion
                    || stored.Items is null)
                {
                    CottonAtomicJsonFile.DeleteIfExists(filePath);
                    return [];
                }

                return stored.Items
                    .Select(TryCreatePin)
                    .Where(item => item is not null)
                    .Select(item => item!)
                    .GroupBy(item => item.FileId)
                    .Select(group => group.Last())
                    .ToList();
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
                    "Failed to load offline-file pins from {FilePath}; resetting the store.",
                    filePath);
                CottonAtomicJsonFile.DeleteIfExists(filePath);
                return [];
            }
        }

        public async Task SaveAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonOfflineFilePinSnapshot> items,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(items);

            string filePath = CreateMetadataFilePath(instanceUri);

            try
            {
                await CottonAtomicJsonFile
                    .WriteAsync(filePath, CreateStoredManifest(items), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.LogError(exception, "Failed to save offline-file pins to {FilePath}.", filePath);
                throw;
            }
        }

        public async Task AddOrReplaceAsync(
            Uri instanceUri,
            CottonOfflineFilePinSnapshot item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(item);

            IReadOnlyList<CottonOfflineFilePinSnapshot> current =
                await LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonOfflineFilePinSnapshot> updated = current
                .Where(existing => existing.FileId != item.FileId)
                .ToList();
            updated.Add(item);

            await SaveAsync(instanceUri, updated, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> RemoveAsync(
            Uri instanceUri,
            Guid fileId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("File id is required.", nameof(fileId));
            }

            IReadOnlyList<CottonOfflineFilePinSnapshot> current =
                await LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonOfflineFilePinSnapshot> updated = current
                .Where(existing => existing.FileId != fileId)
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
            return Path.Combine(
                _pathProvider.CreateOfflineFileMetadataDirectory(instanceUri),
                MetadataFileName);
        }

        private static CottonStoredOfflineFilePinManifest CreateStoredManifest(
            IReadOnlyCollection<CottonOfflineFilePinSnapshot> items)
        {
            return new CottonStoredOfflineFilePinManifest
            {
                SchemaVersion = SchemaVersion,
                SavedAtUtc = DateTime.UtcNow,
                Items = items
                    .GroupBy(item => item.FileId)
                    .Select(group => group.Last())
                    .Select(CreateStoredItem)
                    .ToList(),
            };
        }

        private static CottonStoredOfflineFilePinItem CreateStoredItem(CottonOfflineFilePinSnapshot item)
        {
            return new CottonStoredOfflineFilePinItem
            {
                FileId = item.FileId,
                FileName = item.FileName,
                PinnedAtUtc = item.PinnedAtUtc,
                RemoteUpdatedAtUtc = item.RemoteUpdatedAtUtc,
                SizeBytes = item.SizeBytes,
                ContentType = item.ContentType,
            };
        }

        private static CottonOfflineFilePinSnapshot? TryCreatePin(CottonStoredOfflineFilePinItem item)
        {
            try
            {
                return new CottonOfflineFilePinSnapshot(
                    item.FileId,
                    item.FileName ?? string.Empty,
                    item.PinnedAtUtc,
                    item.RemoteUpdatedAtUtc,
                    item.SizeBytes,
                    item.ContentType);
            }
            catch (Exception exception)
                when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                return null;
            }
        }

    }
}
