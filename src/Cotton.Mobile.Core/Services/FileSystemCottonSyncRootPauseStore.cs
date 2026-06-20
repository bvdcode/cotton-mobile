using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonSyncRootPauseStore : ICottonSyncRootPauseStore
    {
        private const int SchemaVersion = 1;
        private const string TemporaryFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public const string MetadataFileName = "paused-sync-roots.json";

        private readonly ICottonSyncRootMetadataPathProvider _pathProvider;

        public FileSystemCottonSyncRootPauseStore(ICottonSyncRootMetadataPathProvider pathProvider)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);

            _pathProvider = pathProvider;
        }

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
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 16384,
                    useAsync: true);
                CottonStoredPausedSyncRootCollection? stored =
                    await JsonSerializer.DeserializeAsync<CottonStoredPausedSyncRootCollection>(
                        stream,
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                if (stored is null
                    || stored.SchemaVersion != SchemaVersion
                    || stored.RootIds is null)
                {
                    DeleteFile(filePath);
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
                DeleteFile(filePath);
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

            DeleteFile(CreateMetadataFilePath(instanceUri));
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
                DeleteFile(filePath);
                return;
            }

            string directory = _pathProvider.CreateSyncRootMetadataDirectory(instanceUri);
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
                        CreateStoredCollection(validRootIds),
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

        private string CreateMetadataFilePath(Uri instanceUri)
        {
            return Path.Combine(_pathProvider.CreateSyncRootMetadataDirectory(instanceUri), MetadataFileName);
        }

        private static CottonStoredPausedSyncRootCollection CreateStoredCollection(
            IReadOnlyCollection<Guid> rootIds)
        {
            return new CottonStoredPausedSyncRootCollection
            {
                SchemaVersion = SchemaVersion,
                SavedAtUtc = DateTime.UtcNow,
                RootIds = rootIds.ToList(),
            };
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
