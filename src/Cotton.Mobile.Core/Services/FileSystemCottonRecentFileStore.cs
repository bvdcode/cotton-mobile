using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonRecentFileStore : ICottonRecentFileStore
    {
        private const int SchemaVersion = 1;
        private const string TemporaryFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public const string MetadataFileName = "recent-files.json";

        private readonly ICottonRecentFileMetadataPathProvider _pathProvider;
        private readonly CottonRecentFileStoreOptions _options;

        public FileSystemCottonRecentFileStore(
            ICottonRecentFileMetadataPathProvider pathProvider,
            CottonRecentFileStoreOptions options)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);
            ArgumentNullException.ThrowIfNull(options);

            _pathProvider = pathProvider;
            _options = options;
        }

        public async Task<IReadOnlyList<CottonRecentFileSnapshot>> LoadAsync(
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
                CottonStoredRecentFileManifest? stored =
                    await JsonSerializer.DeserializeAsync<CottonStoredRecentFileManifest>(
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

                return Normalize(stored.Items
                    .Select(TryCreateRecentFile)
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
            IReadOnlyCollection<CottonRecentFileSnapshot> items,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(items);

            string directory = _pathProvider.CreateRecentFileMetadataDirectory(instanceUri);
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

        public async Task RecordAsync(
            Uri instanceUri,
            CottonRecentFileSnapshot item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(item);

            IReadOnlyList<CottonRecentFileSnapshot> current =
                await LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonRecentFileSnapshot> updated = current
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

            IReadOnlyList<CottonRecentFileSnapshot> current =
                await LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonRecentFileSnapshot> updated = current
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
                _pathProvider.CreateRecentFileMetadataDirectory(instanceUri),
                MetadataFileName);
        }

        private CottonStoredRecentFileManifest CreateStoredManifest(
            IEnumerable<CottonRecentFileSnapshot> items)
        {
            return new CottonStoredRecentFileManifest
            {
                SchemaVersion = SchemaVersion,
                SavedAtUtc = DateTime.UtcNow,
                Items = Normalize(items)
                    .Select(CreateStoredItem)
                    .ToList(),
            };
        }

        private IReadOnlyList<CottonRecentFileSnapshot> Normalize(
            IEnumerable<CottonRecentFileSnapshot> items)
        {
            return items
                .Select((item, index) => new { Item = item, Index = index })
                .GroupBy(candidate => candidate.Item.FileId)
                .Select(group => group
                    .OrderBy(candidate => candidate.Item.LastUsedAtUtc)
                    .ThenBy(candidate => candidate.Index)
                    .Last())
                .OrderByDescending(candidate => candidate.Item.LastUsedAtUtc)
                .ThenByDescending(candidate => candidate.Index)
                .Select(candidate => candidate.Item)
                .Take(_options.MaxItems)
                .ToList();
        }

        private static CottonStoredRecentFileItem CreateStoredItem(CottonRecentFileSnapshot item)
        {
            return new CottonStoredRecentFileItem
            {
                FileId = item.FileId,
                FileName = item.FileName,
                Kind = item.Kind,
                BadgeText = item.BadgeText,
                RemoteUpdatedAtUtc = item.RemoteUpdatedAtUtc,
                SizeBytes = item.SizeBytes,
                ContentType = item.ContentType,
                LastUsedAtUtc = item.LastUsedAtUtc,
                LastAction = item.LastAction,
            };
        }

        private static CottonRecentFileSnapshot? TryCreateRecentFile(CottonStoredRecentFileItem item)
        {
            try
            {
                return new CottonRecentFileSnapshot(
                    item.FileId,
                    item.FileName ?? string.Empty,
                    item.Kind ?? string.Empty,
                    item.BadgeText ?? string.Empty,
                    item.RemoteUpdatedAtUtc,
                    item.SizeBytes,
                    item.ContentType,
                    item.LastUsedAtUtc,
                    item.LastAction);
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
