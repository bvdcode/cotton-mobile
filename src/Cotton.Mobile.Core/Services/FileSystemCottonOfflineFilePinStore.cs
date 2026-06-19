using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonOfflineFilePinStore : ICottonOfflineFilePinStore
    {
        private const int SchemaVersion = 1;
        private const string TemporaryFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public const string MetadataFileName = "offline-files.json";

        private readonly ICottonOfflineFileMetadataPathProvider _pathProvider;

        public FileSystemCottonOfflineFilePinStore(
            ICottonOfflineFileMetadataPathProvider pathProvider)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);

            _pathProvider = pathProvider;
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
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 16384,
                    useAsync: true);
                CottonStoredOfflineFilePinManifest? stored =
                    await JsonSerializer.DeserializeAsync<CottonStoredOfflineFilePinManifest>(
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
                DeleteFile(filePath);
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

            string directory = _pathProvider.CreateOfflineFileMetadataDirectory(instanceUri);
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
                        CreateStoredManifest(items),
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

            DeleteFile(CreateMetadataFilePath(instanceUri));
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
