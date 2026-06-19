using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonShareIntakeStore : ICottonShareIntakeStore
    {
        private const int SchemaVersion = 1;
        private const string MetadataFileName = "inbox.json";
        private const string TemporaryFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ICottonShareIntakePathProvider _pathProvider;

        public FileSystemCottonShareIntakeStore(ICottonShareIntakePathProvider pathProvider)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);

            _pathProvider = pathProvider;
        }

        public async Task<IReadOnlyList<CottonShareIntakeSnapshot>> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            string filePath = CreateMetadataFilePath();
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
                CottonStoredShareIntakeInbox? inbox =
                    await JsonSerializer.DeserializeAsync<CottonStoredShareIntakeInbox>(
                        stream,
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                if (inbox is null
                    || inbox.SchemaVersion != SchemaVersion
                    || inbox.Items is null)
                {
                    DeleteFile(filePath);
                    return [];
                }

                return inbox.Items
                    .Select(TryCreateSnapshot)
                    .Where(item => item is not null)
                    .Select(item => item!)
                    .OrderBy(item => item.ReceivedAtUtc)
                    .ThenBy(item => item.Id)
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

        public async Task AddAsync(
            CottonShareIntakeSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            IReadOnlyList<CottonShareIntakeSnapshot> snapshots =
                await LoadAsync(cancellationToken).ConfigureAwait(false);
            await SaveAsync(
                snapshots
                    .Where(item => item.Id != snapshot.Id)
                    .Append(snapshot)
                    .ToList(),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task SaveAsync(
            IReadOnlyCollection<CottonShareIntakeSnapshot> snapshots,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshots);

            string directory = _pathProvider.CreateShareIntakeDirectory();
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
                        CreateStoredInbox(snapshots),
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

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DeleteFile(CreateMetadataFilePath());
            return Task.CompletedTask;
        }

        private string CreateMetadataFilePath()
        {
            return Path.Combine(_pathProvider.CreateShareIntakeDirectory(), MetadataFileName);
        }

        private static CottonStoredShareIntakeInbox CreateStoredInbox(
            IReadOnlyCollection<CottonShareIntakeSnapshot> snapshots)
        {
            return new CottonStoredShareIntakeInbox
            {
                SchemaVersion = SchemaVersion,
                SavedAtUtc = DateTime.UtcNow,
                Items = snapshots.Select(CreateStoredSnapshot).ToList(),
            };
        }

        private static CottonStoredShareIntakeSnapshot CreateStoredSnapshot(CottonShareIntakeSnapshot snapshot)
        {
            return new CottonStoredShareIntakeSnapshot
            {
                Id = snapshot.Id,
                Kind = snapshot.Kind,
                Status = snapshot.Status,
                SourceMimeType = snapshot.SourceMimeType,
                Items = snapshot.Items.Select(CreateStoredItem).ToList(),
                FailureMessage = snapshot.FailureMessage,
                ReceivedAtUtc = snapshot.ReceivedAtUtc,
            };
        }

        private static CottonStoredShareIntakeItem CreateStoredItem(CottonShareIntakeItemSnapshot item)
        {
            return new CottonStoredShareIntakeItem
            {
                Id = item.Id,
                Type = item.Type,
                Value = item.Value,
                DisplayName = item.DisplayName,
                MimeType = item.MimeType,
                StagedFileName = item.StagedFileName,
                StagedPath = item.StagedPath,
                StagedSizeBytes = item.StagedSizeBytes,
            };
        }

        private static CottonShareIntakeSnapshot? TryCreateSnapshot(CottonStoredShareIntakeSnapshot item)
        {
            try
            {
                IReadOnlyList<CottonShareIntakeItemSnapshot> items = item.Items?
                    .Select(TryCreateItem)
                    .Where(snapshot => snapshot is not null)
                    .Select(snapshot => snapshot!)
                    .ToList() ?? [];
                if (items.Count == 0)
                {
                    return null;
                }

                return new CottonShareIntakeSnapshot(
                    item.Id,
                    item.Kind,
                    item.Status,
                    item.SourceMimeType,
                    items,
                    item.FailureMessage,
                    item.ReceivedAtUtc);
            }
            catch (Exception exception)
                when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return null;
            }
        }

        private static CottonShareIntakeItemSnapshot? TryCreateItem(CottonStoredShareIntakeItem item)
        {
            try
            {
                return new CottonShareIntakeItemSnapshot(
                    item.Id,
                    item.Type,
                    item.Value ?? string.Empty,
                    item.DisplayName,
                    item.MimeType,
                    item.StagedFileName,
                    item.StagedPath,
                    item.StagedSizeBytes);
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

        private class CottonStoredShareIntakeInbox
        {
            public int SchemaVersion { get; set; }

            public DateTime SavedAtUtc { get; set; }

            public List<CottonStoredShareIntakeSnapshot>? Items { get; set; }
        }

        private class CottonStoredShareIntakeSnapshot
        {
            public Guid Id { get; set; }

            public CottonShareIntakeKind Kind { get; set; }

            public CottonShareIntakeStatus Status { get; set; }

            public string? SourceMimeType { get; set; }

            public List<CottonStoredShareIntakeItem>? Items { get; set; }

            public string? FailureMessage { get; set; }

            public DateTime ReceivedAtUtc { get; set; }
        }

        private class CottonStoredShareIntakeItem
        {
            public Guid Id { get; set; }

            public CottonShareIntakeItemType Type { get; set; }

            public string? Value { get; set; }

            public string? DisplayName { get; set; }

            public string? MimeType { get; set; }

            public string? StagedFileName { get; set; }

            public string? StagedPath { get; set; }

            public long? StagedSizeBytes { get; set; }
        }
    }
}
