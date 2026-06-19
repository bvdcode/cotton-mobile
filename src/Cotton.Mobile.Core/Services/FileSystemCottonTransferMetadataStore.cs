using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonTransferMetadataStore : ICottonTransferMetadataStore
    {
        private const int SchemaVersion = 1;
        private const string MetadataFileName = "queue.json";
        private const string TemporaryFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ICottonTransferMetadataPathProvider _pathProvider;
        private readonly TimeProvider _timeProvider;

        public FileSystemCottonTransferMetadataStore(
            ICottonTransferMetadataPathProvider pathProvider,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);

            _pathProvider = pathProvider;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<IReadOnlyList<CottonTransferQueueItem>> LoadAsync(
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
                CottonStoredTransferQueue? queue =
                    await JsonSerializer.DeserializeAsync<CottonStoredTransferQueue>(
                        stream,
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                if (queue is null
                    || queue.SchemaVersion != SchemaVersion
                    || queue.Items is null)
                {
                    DeleteFile(filePath);
                    return [];
                }

                DateTime restoredAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return queue.Items
                    .Select(TryCreateTransfer)
                    .Where(item => item is not null)
                    .Select(item => item!.RestoreAfterRestart(restoredAtUtc))
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
            IReadOnlyCollection<CottonTransferQueueItem> items,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(items);

            string directory = _pathProvider.CreateTransferMetadataDirectory(instanceUri);
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
                        CreateStoredQueue(items),
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

        public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();

            DeleteFile(CreateMetadataFilePath(instanceUri));
            return Task.CompletedTask;
        }

        private string CreateMetadataFilePath(Uri instanceUri)
        {
            return Path.Combine(_pathProvider.CreateTransferMetadataDirectory(instanceUri), MetadataFileName);
        }

        private static CottonStoredTransferQueue CreateStoredQueue(IReadOnlyCollection<CottonTransferQueueItem> items)
        {
            return new CottonStoredTransferQueue
            {
                SchemaVersion = SchemaVersion,
                SavedAtUtc = DateTime.UtcNow,
                Items = items.Select(CreateStoredItem).ToList(),
            };
        }

        private static CottonStoredTransferQueueItem CreateStoredItem(CottonTransferQueueItem item)
        {
            return new CottonStoredTransferQueueItem
            {
                Id = item.Id,
                Kind = item.Kind,
                DisplayName = item.DisplayName,
                ContentType = item.ContentType,
                Source = CreateStoredSource(item.Source),
                Destination = CreateStoredDestination(item.Destination),
                Status = item.Status,
                TransferredBytes = item.Progress.TransferredBytes,
                TotalBytes = item.Progress.TotalBytes,
                AttemptCount = item.AttemptCount,
                FailureMessage = item.FailureMessage,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc,
            };
        }

        private static CottonTransferQueueItem? TryCreateTransfer(CottonStoredTransferQueueItem item)
        {
            try
            {
                return CottonTransferQueueItem.Restore(
                    item.Id,
                    item.Kind,
                    item.DisplayName ?? string.Empty,
                    item.Status,
                    item.TransferredBytes,
                    item.TotalBytes,
                    item.AttemptCount,
                    item.FailureMessage,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc,
                    TryCreateDestination(item.Destination),
                    item.ContentType,
                    TryCreateSource(item.Source));
            }
            catch (Exception exception)
                when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                return null;
            }
        }

        private static CottonStoredTransferDestination? CreateStoredDestination(
            CottonTransferDestinationSnapshot? destination)
        {
            return destination is null
                ? null
                : new CottonStoredTransferDestination
                {
                    FolderId = destination.FolderId,
                    FolderName = destination.FolderName,
                    Path = destination.Path,
                };
        }

        private static CottonTransferDestinationSnapshot? TryCreateDestination(
            CottonStoredTransferDestination? destination)
        {
            if (destination is null)
            {
                return null;
            }

            try
            {
                return new CottonTransferDestinationSnapshot(
                    destination.FolderId,
                    destination.FolderName ?? string.Empty,
                    destination.Path);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static CottonStoredTransferSource? CreateStoredSource(CottonTransferSourceSnapshot? source)
        {
            return source is null
                ? null
                : new CottonStoredTransferSource
                {
                    Kind = source.Kind,
                    SourceId = source.SourceId,
                    LastModifiedUtc = source.LastModifiedUtc,
                    SizeBytes = source.SizeBytes,
                    CapturedAtUtc = source.CapturedAtUtc,
                };
        }

        private static CottonTransferSourceSnapshot? TryCreateSource(CottonStoredTransferSource? source)
        {
            if (source is null)
            {
                return null;
            }

            try
            {
                return new CottonTransferSourceSnapshot(
                    source.Kind,
                    source.SourceId ?? string.Empty,
                    source.LastModifiedUtc,
                    source.SizeBytes,
                    source.CapturedAtUtc);
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
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

        private class CottonStoredTransferQueue
        {
            public int SchemaVersion { get; set; }

            public DateTime SavedAtUtc { get; set; }

            public List<CottonStoredTransferQueueItem>? Items { get; set; }
        }

        private class CottonStoredTransferQueueItem
        {
            public Guid Id { get; set; }

            public CottonTransferKind Kind { get; set; }

            public string? DisplayName { get; set; }

            public string? ContentType { get; set; }

            public CottonStoredTransferSource? Source { get; set; }

            public CottonStoredTransferDestination? Destination { get; set; }

            public CottonTransferStatus Status { get; set; }

            public long TransferredBytes { get; set; }

            public long? TotalBytes { get; set; }

            public int AttemptCount { get; set; }

            public string? FailureMessage { get; set; }

            public DateTime CreatedAtUtc { get; set; }

            public DateTime UpdatedAtUtc { get; set; }
        }

        private class CottonStoredTransferSource
        {
            public CottonTransferSourceKind Kind { get; set; }

            public string? SourceId { get; set; }

            public DateTime? LastModifiedUtc { get; set; }

            public long? SizeBytes { get; set; }

            public DateTime? CapturedAtUtc { get; set; }
        }

        private class CottonStoredTransferDestination
        {
            public Guid FolderId { get; set; }

            public string? FolderName { get; set; }

            public string? Path { get; set; }
        }
    }
}
