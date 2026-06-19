namespace Cotton.Mobile.Services
{
    public class CottonQueuedUploadExecutor : ICottonQueuedUploadExecutor
    {
        private const string MissingDestinationMessage = "Upload destination is missing.";
        private const string MissingStagedFileMessage = "Upload file is no longer available on this device.";

        private readonly ICottonTransferMetadataStore _metadataStore;
        private readonly ICottonTransferStagingStore _stagingStore;
        private readonly ICottonQueuedUploadClient _uploadClient;
        private readonly ICottonCameraBackupUploadedMediaStore _cameraBackupUploadedMediaStore;
        private readonly ICottonLocalNotificationService _localNotificationService;
        private readonly TimeProvider _timeProvider;

        public CottonQueuedUploadExecutor(
            ICottonTransferMetadataStore metadataStore,
            ICottonTransferStagingStore stagingStore,
            ICottonQueuedUploadClient uploadClient,
            ICottonCameraBackupUploadedMediaStore cameraBackupUploadedMediaStore,
            TimeProvider? timeProvider)
            : this(
                metadataStore,
                stagingStore,
                uploadClient,
                cameraBackupUploadedMediaStore,
                null,
                timeProvider)
        {
        }

        public CottonQueuedUploadExecutor(
            ICottonTransferMetadataStore metadataStore,
            ICottonTransferStagingStore stagingStore,
            ICottonQueuedUploadClient uploadClient,
            ICottonCameraBackupUploadedMediaStore cameraBackupUploadedMediaStore,
            ICottonLocalNotificationService? localNotificationService = null,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(metadataStore);
            ArgumentNullException.ThrowIfNull(stagingStore);
            ArgumentNullException.ThrowIfNull(uploadClient);
            ArgumentNullException.ThrowIfNull(cameraBackupUploadedMediaStore);

            _metadataStore = metadataStore;
            _stagingStore = stagingStore;
            _uploadClient = uploadClient;
            _cameraBackupUploadedMediaStore = cameraBackupUploadedMediaStore;
            _localNotificationService = localNotificationService ?? NullCottonLocalNotificationService.Instance;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<CottonQueuedUploadExecutionResult> ExecuteNextAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            List<CottonTransferQueueItem> queue = (await _metadataStore
                    .LoadAsync(instanceUri, cancellationToken)
                    .ConfigureAwait(false))
                .ToList();
            int transferIndex = queue.FindIndex(
                item => item.Kind == CottonTransferKind.Upload
                    && item.Status == CottonTransferStatus.Queued);
            if (transferIndex < 0)
            {
                return new CottonQueuedUploadExecutionResult(
                    CottonQueuedUploadExecutionStatus.NoQueuedUpload,
                    null,
                    null);
            }

            CottonTransferQueueItem transfer = queue[transferIndex];
            if (transfer.Destination is null)
            {
                CottonTransferQueueItem failed = transfer.MarkFailed(
                    MissingDestinationMessage,
                    GetUtcNow());
                await SaveTransferAsync(instanceUri, queue, transferIndex, failed, cancellationToken)
                    .ConfigureAwait(false);
                await ShowFailedNotificationAsync(failed, cancellationToken)
                    .ConfigureAwait(false);
                return new CottonQueuedUploadExecutionResult(
                    CottonQueuedUploadExecutionStatus.MissingDestination,
                    failed,
                    failed.FailureMessage);
            }

            CottonTransferStagedFileSnapshot? stagedFile = await _stagingStore
                .GetAsync(instanceUri, transfer.Id, cancellationToken)
                .ConfigureAwait(false);
            if (stagedFile is null)
            {
                CottonTransferQueueItem failed = transfer.MarkFailed(
                    MissingStagedFileMessage,
                    GetUtcNow());
                await SaveTransferAsync(instanceUri, queue, transferIndex, failed, cancellationToken)
                    .ConfigureAwait(false);
                await ShowFailedNotificationAsync(failed, cancellationToken)
                    .ConfigureAwait(false);
                return new CottonQueuedUploadExecutionResult(
                    CottonQueuedUploadExecutionStatus.MissingStagedFile,
                    failed,
                    failed.FailureMessage);
            }

            CottonTransferQueueItem running = transfer.Start(GetUtcNow());
            await SaveTransferAsync(instanceUri, queue, transferIndex, running, cancellationToken)
                .ConfigureAwait(false);

            CottonTransferQueueItem current = running;
            try
            {
                CottonQueuedUploadClientResult uploadResult = await _uploadClient
                    .UploadAsync(
                        instanceUri,
                        running,
                        stagedFile,
                        async (transferredBytes, token) =>
                        {
                            current = current.ReportProgress(transferredBytes, GetUtcNow());
                            await SaveTransferAsync(instanceUri, queue, transferIndex, current, token)
                                .ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                CottonTransferQueueItem completed = current.Complete(GetUtcNow());
                await SaveTransferAsync(instanceUri, queue, transferIndex, completed, cancellationToken)
                    .ConfigureAwait(false);
                await RecordCameraBackupUploadAsync(
                        instanceUri,
                        completed,
                        uploadResult,
                        cancellationToken)
                    .ConfigureAwait(false);
                await ShowCompletedNotificationAsync(completed, cancellationToken)
                    .ConfigureAwait(false);
                await _stagingStore.DeleteAsync(instanceUri, completed.Id, cancellationToken)
                    .ConfigureAwait(false);
                return new CottonQueuedUploadExecutionResult(
                    CottonQueuedUploadExecutionStatus.Completed,
                    completed,
                    null);
            }
            catch (OperationCanceledException)
            {
                CottonTransferQueueItem restored = current.RestoreAfterRestart(GetUtcNow());
                await SaveTransferAsync(instanceUri, queue, transferIndex, restored, CancellationToken.None)
                    .ConfigureAwait(false);
                throw;
            }
            catch (Exception exception)
            {
                CottonTransferQueueItem failed = current.MarkFailed(CreateFailureMessage(exception), GetUtcNow());
                await SaveTransferAsync(instanceUri, queue, transferIndex, failed, cancellationToken)
                    .ConfigureAwait(false);
                await ShowFailedNotificationAsync(failed, cancellationToken)
                    .ConfigureAwait(false);
                return new CottonQueuedUploadExecutionResult(
                    CottonQueuedUploadExecutionStatus.Failed,
                    failed,
                    failed.FailureMessage);
            }
        }

        private async Task SaveTransferAsync(
            Uri instanceUri,
            List<CottonTransferQueueItem> queue,
            int transferIndex,
            CottonTransferQueueItem transfer,
            CancellationToken cancellationToken)
        {
            queue[transferIndex] = transfer;
            await _metadataStore.SaveAsync(instanceUri, queue, cancellationToken).ConfigureAwait(false);
        }

        private DateTime GetUtcNow()
        {
            return _timeProvider.GetUtcNow().UtcDateTime;
        }

        private async Task RecordCameraBackupUploadAsync(
            Uri instanceUri,
            CottonTransferQueueItem completed,
            CottonQueuedUploadClientResult uploadResult,
            CancellationToken cancellationToken)
        {
            CottonTransferSourceSnapshot? source = completed.Source;
            if (source?.Kind != CottonTransferSourceKind.CameraBackup)
            {
                return;
            }

            var uploaded = new CottonCameraBackupUploadedMediaSnapshot(
                new CottonCameraBackupMediaIdentity(
                    source.SourceId,
                    source.LastModifiedUtc,
                    source.SizeBytes),
                completed.UpdatedAtUtc,
                uploadResult.RemoteFileId,
                uploadResult.RemoteFileName ?? completed.DisplayName);
            await _cameraBackupUploadedMediaStore
                .AddOrReplaceAsync(instanceUri, uploaded, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task ShowCompletedNotificationAsync(
            CottonTransferQueueItem completed,
            CancellationToken cancellationToken)
        {
            try
            {
                CottonLocalNotificationSnapshot notification =
                    CottonTransferNotificationFactory.CreateCompletedUpload(completed);
                await _localNotificationService
                    .ShowAsync(notification, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Notification delivery must never turn a completed upload into a failed transfer.
            }
        }

        private async Task ShowFailedNotificationAsync(
            CottonTransferQueueItem failed,
            CancellationToken cancellationToken)
        {
            try
            {
                CottonLocalNotificationSnapshot notification =
                    CottonTransferNotificationFactory.CreateFailedUpload(failed);
                await _localNotificationService
                    .ShowAsync(notification, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Notification delivery must never hide or replace the persisted failure state.
            }
        }

        private static string CreateFailureMessage(Exception exception)
        {
            return string.IsNullOrWhiteSpace(exception.Message)
                ? "Upload failed."
                : exception.Message.Trim();
        }
    }
}
