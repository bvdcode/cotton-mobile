// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonCameraBackupUploadedMediaStore : ICottonCameraBackupUploadedMediaStore
    {
        private const int SchemaVersion = 1;
        private const string MetadataFileName = "uploaded-media.json";
        private const string TemporaryFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ICottonCameraBackupMetadataPathProvider _pathProvider;

        public FileSystemCottonCameraBackupUploadedMediaStore(
            ICottonCameraBackupMetadataPathProvider pathProvider)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);

            _pathProvider = pathProvider;
        }

        public async Task<IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot>> LoadAsync(
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
                CottonStoredCameraBackupUploadedMedia? stored =
                    await JsonSerializer.DeserializeAsync<CottonStoredCameraBackupUploadedMedia>(
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

                return stored.Items
                    .Select(TryCreateUploadedMedia)
                    .Where(item => item is not null)
                    .Select(item => item!)
                    .GroupBy(item => item.Identity)
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
                DeleteFile(filePath);
                return [];
            }
        }

        public async Task SaveAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonCameraBackupUploadedMediaSnapshot> items,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(items);

            string directory = _pathProvider.CreateCameraBackupMetadataDirectory(instanceUri);
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
                        CreateStoredMetadata(items),
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
            CottonCameraBackupUploadedMediaSnapshot item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(item);

            IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot> current =
                await LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonCameraBackupUploadedMediaSnapshot> updated = current
                .Where(existing => !existing.Identity.Equals(item.Identity))
                .ToList();
            updated.Add(item);

            await SaveAsync(instanceUri, updated, cancellationToken).ConfigureAwait(false);
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
            return Path.Combine(
                _pathProvider.CreateCameraBackupMetadataDirectory(instanceUri),
                MetadataFileName);
        }

        private static CottonStoredCameraBackupUploadedMedia CreateStoredMetadata(
            IReadOnlyCollection<CottonCameraBackupUploadedMediaSnapshot> items)
        {
            return new CottonStoredCameraBackupUploadedMedia
            {
                SchemaVersion = SchemaVersion,
                SavedAtUtc = DateTime.UtcNow,
                Items = items
                    .GroupBy(item => item.Identity)
                    .Select(group => group.Last())
                    .Select(CreateStoredItem)
                    .ToList(),
            };
        }

        private static CottonStoredCameraBackupUploadedMediaItem CreateStoredItem(
            CottonCameraBackupUploadedMediaSnapshot item)
        {
            return new CottonStoredCameraBackupUploadedMediaItem
            {
                SourceId = item.Identity.SourceId,
                LastModifiedUtc = item.Identity.LastModifiedUtc,
                SizeBytes = item.Identity.SizeBytes,
                UploadedAtUtc = item.UploadedAtUtc,
                RemoteFileId = item.RemoteFileId,
                RemoteFileName = item.RemoteFileName,
            };
        }

        private static CottonCameraBackupUploadedMediaSnapshot? TryCreateUploadedMedia(
            CottonStoredCameraBackupUploadedMediaItem item)
        {
            try
            {
                return new CottonCameraBackupUploadedMediaSnapshot(
                    new CottonCameraBackupMediaIdentity(
                        item.SourceId ?? string.Empty,
                        item.LastModifiedUtc,
                        item.SizeBytes),
                    item.UploadedAtUtc,
                    item.RemoteFileId,
                    item.RemoteFileName);
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

        private class CottonStoredCameraBackupUploadedMedia
        {
            public int SchemaVersion { get; set; }

            public DateTime SavedAtUtc { get; set; }

            public List<CottonStoredCameraBackupUploadedMediaItem>? Items { get; set; }
        }

        private class CottonStoredCameraBackupUploadedMediaItem
        {
            public string? SourceId { get; set; }

            public DateTime? LastModifiedUtc { get; set; }

            public long? SizeBytes { get; set; }

            public DateTime UploadedAtUtc { get; set; }

            public Guid? RemoteFileId { get; set; }

            public string? RemoteFileName { get; set; }
        }
    }
}
