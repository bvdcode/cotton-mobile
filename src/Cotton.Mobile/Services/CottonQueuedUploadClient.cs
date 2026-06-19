namespace Cotton.Mobile.Services
{
    public class CottonQueuedUploadClient : ICottonQueuedUploadClient
    {
        private readonly ICottonFileUploadService _uploadService;

        public CottonQueuedUploadClient(ICottonFileUploadService uploadService)
        {
            ArgumentNullException.ThrowIfNull(uploadService);

            _uploadService = uploadService;
        }

        public async Task<CottonQueuedUploadClientResult> UploadAsync(
            Uri instanceUri,
            CottonTransferQueueItem transfer,
            CottonTransferStagedFileSnapshot stagedFile,
            Func<long, CancellationToken, Task> reportProgressAsync,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(transfer);
            ArgumentNullException.ThrowIfNull(stagedFile);
            ArgumentNullException.ThrowIfNull(reportProgressAsync);

            CottonTransferDestinationSnapshot destination = transfer.Destination
                ?? throw new InvalidOperationException("Upload destination is missing.");
            var source = new CottonFileUploadSource(
                new CottonFileUploadSourceSnapshot(
                    transfer.DisplayName,
                    transfer.ContentType,
                    stagedFile.SizeBytes,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [CottonFileUploadMetadataKeys.Source] = "queued-upload",
                    }),
                token =>
                {
                    Stream stream = new FileStream(
                        stagedFile.Path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        bufferSize: 81920,
                        useAsync: true);
                    token.ThrowIfCancellationRequested();
                    return Task.FromResult(stream);
                });
            var progress = new QueuedUploadProgress(reportProgressAsync, cancellationToken);
            CottonFileBrowserEntry uploadedFile = await _uploadService
                .UploadAsync(
                    instanceUri,
                    new CottonFolderHandle(destination.FolderId, destination.FolderName),
                    source,
                    progress,
                    cancellationToken)
                .ConfigureAwait(false);
            return new CottonQueuedUploadClientResult(uploadedFile.Id, uploadedFile.Name);
        }

        private sealed class QueuedUploadProgress : IProgress<long>
        {
            private readonly Func<long, CancellationToken, Task> _reportProgressAsync;
            private readonly CancellationToken _cancellationToken;

            public QueuedUploadProgress(
                Func<long, CancellationToken, Task> reportProgressAsync,
                CancellationToken cancellationToken)
            {
                _reportProgressAsync = reportProgressAsync;
                _cancellationToken = cancellationToken;
            }

            public void Report(long value)
            {
                _reportProgressAsync(value, _cancellationToken).GetAwaiter().GetResult();
            }
        }
    }
}
