using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class QueuedUploadExecutorTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid TransferId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid DestinationFolderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 20, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ExecutedAt = new(2026, 6, 19, 20, 5, 0, DateTimeKind.Utc);
        private static readonly Guid RemoteFileId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        private static readonly DateTime CameraModifiedAt = new(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime CameraCapturedAt = new(2026, 6, 18, 7, 30, 0, DateTimeKind.Utc);

        [Fact]
        public async Task ExecuteNext_runs_first_queued_upload_and_deletes_staged_file()
        {
            CottonTransferQueueItem queued = CreateQueuedUpload();
            var metadataStore = new FakeTransferMetadataStore([queued]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([40, 100]);
            var notificationService = new FakeLocalNotificationService();
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                notificationService);

            CottonQueuedUploadExecutionResult result = await executor.ExecuteNextAsync(InstanceUri);

            Assert.Equal(CottonQueuedUploadExecutionStatus.Completed, result.Status);
            Assert.Equal(CottonTransferStatus.Completed, result.Transfer?.Status);
            Assert.Equal(100, result.Transfer?.Progress.TransferredBytes);
            Assert.Equal(1, result.Transfer?.AttemptCount);
            Assert.Equal(TransferId, uploadClient.UploadedTransferIds.Single());
            Assert.Equal(TransferId, stagingStore.DeletedTransferIds.Single());
            CottonLocalNotificationSnapshot notification = Assert.Single(notificationService.Notifications);
            Assert.Equal(CottonLocalNotificationKind.TransferCompleted, notification.Kind);
            Assert.Equal(CottonNotificationChannelKind.Transfers, notification.ChannelKind);
            Assert.Equal("upload.bin uploaded to Default.", notification.Message);
            Assert.Contains(
                metadataStore.SaveSnapshots,
                snapshot => snapshot.Single().Status == CottonTransferStatus.Running);
            Assert.Contains(
                metadataStore.SaveSnapshots,
                snapshot => snapshot.Single().Progress.TransferredBytes == 40);
            Assert.Equal(CottonTransferStatus.Completed, metadataStore.Items.Single().Status);
        }

        [Fact]
        public async Task ExecuteNext_notifies_transfer_activity_when_queue_metadata_changes()
        {
            CottonTransferQueueItem queued = CreateQueuedUpload();
            var metadataStore = new FakeTransferMetadataStore([queued]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([40, 100]);
            var signal = new CottonTransferActivitySignal();
            int eventCount = 0;
            signal.TransferActivityChanged += (_, _) => eventCount++;
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                new FakeCameraBackupUploadedMediaStore(),
                notificationService: null,
                transferActivitySignal: signal);

            CottonQueuedUploadExecutionResult result = await executor.ExecuteNextAsync(InstanceUri);

            Assert.Equal(CottonQueuedUploadExecutionStatus.Completed, result.Status);
            Assert.Equal(metadataStore.SaveSnapshots.Count, eventCount);
            Assert.Contains(
                metadataStore.SaveSnapshots,
                snapshot => snapshot.Single().Status == CottonTransferStatus.Running);
            Assert.Contains(
                metadataStore.SaveSnapshots,
                snapshot => snapshot.Single().Status == CottonTransferStatus.Completed);
        }

        [Fact]
        public async Task ExecuteNext_notifies_running_upload_progress()
        {
            CottonTransferQueueItem queued = CreateQueuedUpload();
            var metadataStore = new FakeTransferMetadataStore([queued]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([40, 100]);
            var progressSignal = new CottonTransferProgressSignal();
            IReadOnlyList<CottonTransferProgressChangedEventArgs> progressEvents = [];
            progressSignal.TransferProgressChanged += (_, args) =>
            {
                progressEvents = progressEvents.Concat([args]).ToList();
            };
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                new FakeCameraBackupUploadedMediaStore(),
                notificationService: null,
                transferActivitySignal: null,
                transferProgressSignal: progressSignal);

            CottonQueuedUploadExecutionResult result = await executor.ExecuteNextAsync(InstanceUri);

            Assert.Equal(CottonQueuedUploadExecutionStatus.Completed, result.Status);
            Assert.Equal(
                [0L, 40L, 100L],
                progressEvents.Select(item => item.Progress.TransferredBytes).ToArray());
            Assert.All(progressEvents, item => Assert.Equal(TransferId, item.TransferId));
            Assert.All(progressEvents, item => Assert.Equal(100, item.Progress.TotalBytes));
        }

        [Fact]
        public async Task Execute_runs_requested_queued_upload_without_running_earlier_waiting_upload()
        {
            Guid firstTransferId = Guid.Parse("99999999-aaaa-bbbb-cccc-dddddddddddd");
            CottonTransferQueueItem firstQueued = CottonTransferQueueItem.CreateUpload(
                firstTransferId,
                "first.bin",
                50,
                CreatedAt,
                new CottonTransferDestinationSnapshot(
                    DestinationFolderId,
                    "Default",
                    "Default"));
            CottonTransferQueueItem requestedQueued = CreateQueuedUpload();
            var metadataStore = new FakeTransferMetadataStore([firstQueued, requestedQueued]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([100]);
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient);

            CottonQueuedUploadExecutionResult result =
                await executor.ExecuteAsync(InstanceUri, TransferId);

            Assert.Equal(CottonQueuedUploadExecutionStatus.Completed, result.Status);
            Assert.Equal(TransferId, uploadClient.UploadedTransferIds.Single());
            Assert.Equal(CottonTransferStatus.Queued, metadataStore.Items.Single(item => item.Id == firstTransferId).Status);
            Assert.Equal(CottonTransferStatus.Completed, metadataStore.Items.Single(item => item.Id == TransferId).Status);
            Assert.DoesNotContain(
                metadataStore.SaveSnapshots,
                snapshot => snapshot.Any(item => item.Id == firstTransferId && item.Status != CottonTransferStatus.Queued));
        }

        [Fact]
        public async Task Execute_returns_not_found_without_saving_when_scheduled_transfer_is_missing()
        {
            CottonTransferQueueItem queued = CreateQueuedUpload();
            var metadataStore = new FakeTransferMetadataStore([queued]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([100]);
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient);

            CottonQueuedUploadExecutionResult result =
                await executor.ExecuteAsync(
                    InstanceUri,
                    Guid.Parse("aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb"));

            Assert.Equal(CottonQueuedUploadExecutionStatus.TransferNotFound, result.Status);
            Assert.Null(result.Transfer);
            Assert.Equal("Upload transfer is no longer in the queue.", result.FailureMessage);
            Assert.Empty(uploadClient.UploadedTransferIds);
            Assert.Empty(metadataStore.SaveSnapshots);
        }

        [Fact]
        public async Task Execute_returns_not_queued_without_saving_when_scheduled_transfer_is_already_terminal()
        {
            CottonTransferQueueItem completed = CreateQueuedUpload()
                .Start(CreatedAt.AddSeconds(1))
                .Complete(CreatedAt.AddSeconds(2));
            var metadataStore = new FakeTransferMetadataStore([completed]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([100]);
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient);

            CottonQueuedUploadExecutionResult result =
                await executor.ExecuteAsync(InstanceUri, TransferId);

            Assert.Equal(CottonQueuedUploadExecutionStatus.TransferNotQueued, result.Status);
            Assert.Equal(CottonTransferStatus.Completed, result.Transfer?.Status);
            Assert.Equal("Upload transfer is not waiting to run.", result.FailureMessage);
            Assert.Empty(uploadClient.UploadedTransferIds);
            Assert.Empty(metadataStore.SaveSnapshots);
        }

        [Fact]
        public async Task ExecuteNext_records_camera_backup_upload_after_completed_upload()
        {
            CottonTransferQueueItem queued = CreateCameraBackupQueuedUpload();
            var metadataStore = new FakeTransferMetadataStore([queued]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([100]);
            var uploadedMediaStore = new FakeCameraBackupUploadedMediaStore();
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                uploadedMediaStore);

            CottonQueuedUploadExecutionResult result = await executor.ExecuteNextAsync(InstanceUri);

            Assert.Equal(CottonQueuedUploadExecutionStatus.Completed, result.Status);
            CottonCameraBackupUploadedMediaSnapshot uploaded = Assert.Single(uploadedMediaStore.Items);
            Assert.Equal("content://media/external/images/media/100", uploaded.Identity.SourceId);
            Assert.Equal(CameraModifiedAt, uploaded.Identity.LastModifiedUtc);
            Assert.Equal(100, uploaded.Identity.SizeBytes);
            Assert.Equal(ExecutedAt, uploaded.UploadedAtUtc);
            Assert.Equal(RemoteFileId, uploaded.RemoteFileId);
            Assert.Equal("remote-upload.bin", uploaded.RemoteFileName);
        }

        [Fact]
        public async Task ExecuteNext_keeps_completed_upload_when_notification_delivery_fails()
        {
            CottonTransferQueueItem queued = CreateQueuedUpload();
            var metadataStore = new FakeTransferMetadataStore([queued]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([100]);
            var notificationService = new FakeLocalNotificationService(new InvalidOperationException("No notification permission"));
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                notificationService);

            CottonQueuedUploadExecutionResult result = await executor.ExecuteNextAsync(InstanceUri);

            Assert.Equal(CottonQueuedUploadExecutionStatus.Completed, result.Status);
            Assert.Equal(CottonTransferStatus.Completed, metadataStore.Items.Single().Status);
            Assert.Equal(TransferId, stagingStore.DeletedTransferIds.Single());
        }

        [Fact]
        public async Task ExecuteNext_does_not_record_non_camera_backup_upload()
        {
            CottonTransferQueueItem queued = CreateQueuedUpload();
            var metadataStore = new FakeTransferMetadataStore([queued]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([100]);
            var uploadedMediaStore = new FakeCameraBackupUploadedMediaStore();
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                uploadedMediaStore);

            CottonQueuedUploadExecutionResult result = await executor.ExecuteNextAsync(InstanceUri);

            Assert.Equal(CottonQueuedUploadExecutionStatus.Completed, result.Status);
            Assert.Empty(uploadedMediaStore.Items);
        }

        [Fact]
        public async Task ExecuteNext_marks_missing_staged_file_as_failed_without_uploading()
        {
            CottonTransferQueueItem queued = CreateQueuedUpload();
            var metadataStore = new FakeTransferMetadataStore([queued]);
            var stagingStore = new FakeTransferStagingStore(stagedFile: null);
            var uploadClient = new FakeQueuedUploadClient([]);
            var notificationService = new FakeLocalNotificationService();
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                notificationService);

            CottonQueuedUploadExecutionResult result = await executor.ExecuteNextAsync(InstanceUri);

            Assert.Equal(CottonQueuedUploadExecutionStatus.MissingStagedFile, result.Status);
            Assert.Equal(CottonTransferStatus.Failed, result.Transfer?.Status);
            Assert.Equal("Upload file is no longer available on this device.", result.FailureMessage);
            Assert.Empty(uploadClient.UploadedTransferIds);
            Assert.Equal(CottonTransferStatus.Failed, metadataStore.Items.Single().Status);
            CottonLocalNotificationSnapshot notification = Assert.Single(notificationService.Notifications);
            Assert.Equal(CottonLocalNotificationKind.TransferFailed, notification.Kind);
            Assert.Equal("upload.bin: Upload file is no longer available on this device.", notification.Message);
        }

        [Fact]
        public async Task ExecuteNext_marks_missing_destination_as_failed_without_uploading()
        {
            CottonTransferQueueItem queued = CottonTransferQueueItem.CreateUpload(
                TransferId,
                "upload.bin",
                100,
                CreatedAt);
            var metadataStore = new FakeTransferMetadataStore([queued]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([]);
            var notificationService = new FakeLocalNotificationService();
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                notificationService);

            CottonQueuedUploadExecutionResult result = await executor.ExecuteNextAsync(InstanceUri);

            Assert.Equal(CottonQueuedUploadExecutionStatus.MissingDestination, result.Status);
            Assert.Equal("Upload destination is missing.", result.FailureMessage);
            Assert.Empty(uploadClient.UploadedTransferIds);
            Assert.Equal(CottonTransferStatus.Failed, metadataStore.Items.Single().Status);
            CottonLocalNotificationSnapshot notification = Assert.Single(notificationService.Notifications);
            Assert.Equal(CottonLocalNotificationKind.TransferFailed, notification.Kind);
            Assert.Equal("upload.bin: Upload destination is missing.", notification.Message);
        }

        [Fact]
        public async Task ExecuteNext_marks_upload_exception_as_failed_and_keeps_staged_file_for_retry()
        {
            CottonTransferQueueItem queued = CreateQueuedUpload();
            var metadataStore = new FakeTransferMetadataStore([queued]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([], new InvalidOperationException("Offline"));
            var notificationService = new FakeLocalNotificationService();
            CottonQueuedUploadExecutor executor = CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                notificationService);

            CottonQueuedUploadExecutionResult result = await executor.ExecuteNextAsync(InstanceUri);

            Assert.Equal(CottonQueuedUploadExecutionStatus.Failed, result.Status);
            Assert.Equal(CottonTransferStatus.Failed, result.Transfer?.Status);
            Assert.Equal("Offline", result.FailureMessage);
            Assert.Empty(stagingStore.DeletedTransferIds);
            Assert.True(metadataStore.Items.Single().CanRetry);
            CottonLocalNotificationSnapshot notification = Assert.Single(notificationService.Notifications);
            Assert.Equal(CottonLocalNotificationKind.TransferFailed, notification.Kind);
            Assert.Equal("upload.bin: Offline", notification.Message);
        }

        [Fact]
        public async Task ExecuteNext_returns_no_work_when_queue_has_no_queued_uploads()
        {
            CottonTransferQueueItem failed = CreateQueuedUpload()
                .Start(CreatedAt.AddSeconds(1))
                .Fail("Offline", CreatedAt.AddSeconds(2));
            var metadataStore = new FakeTransferMetadataStore([failed]);
            var stagingStore = new FakeTransferStagingStore(CreateStagedFile());
            var uploadClient = new FakeQueuedUploadClient([]);
            CottonQueuedUploadExecutor executor = CreateExecutor(metadataStore, stagingStore, uploadClient);

            CottonQueuedUploadExecutionResult result = await executor.ExecuteNextAsync(InstanceUri);

            Assert.Equal(CottonQueuedUploadExecutionStatus.NoQueuedUpload, result.Status);
            Assert.Null(result.Transfer);
            Assert.Empty(metadataStore.SaveSnapshots);
        }

        private static CottonQueuedUploadExecutor CreateExecutor(
            FakeTransferMetadataStore metadataStore,
            FakeTransferStagingStore stagingStore,
            FakeQueuedUploadClient uploadClient)
        {
            return CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                new FakeCameraBackupUploadedMediaStore());
        }

        private static CottonQueuedUploadExecutor CreateExecutor(
            FakeTransferMetadataStore metadataStore,
            FakeTransferStagingStore stagingStore,
            FakeQueuedUploadClient uploadClient,
            FakeLocalNotificationService notificationService)
        {
            return CreateExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                new FakeCameraBackupUploadedMediaStore(),
                notificationService);
        }

        private static CottonQueuedUploadExecutor CreateExecutor(
            FakeTransferMetadataStore metadataStore,
            FakeTransferStagingStore stagingStore,
            FakeQueuedUploadClient uploadClient,
            FakeCameraBackupUploadedMediaStore uploadedMediaStore,
            FakeLocalNotificationService? notificationService = null,
            ICottonTransferActivitySignal? transferActivitySignal = null,
            ICottonTransferProgressSignal? transferProgressSignal = null)
        {
            return new CottonQueuedUploadExecutor(
                metadataStore,
                stagingStore,
                uploadClient,
                uploadedMediaStore,
                notificationService,
                new FixedTimeProvider(ExecutedAt),
                transferActivitySignal,
                transferProgressSignal);
        }

        private static CottonTransferQueueItem CreateQueuedUpload()
        {
            return CottonTransferQueueItem.CreateUpload(
                TransferId,
                "upload.bin",
                100,
                CreatedAt,
                new CottonTransferDestinationSnapshot(
                    DestinationFolderId,
                    "Default",
                    "Default"));
        }

        private static CottonTransferQueueItem CreateCameraBackupQueuedUpload()
        {
            return CottonTransferQueueItem.CreateUpload(
                TransferId,
                "upload.bin",
                100,
                CreatedAt,
                new CottonTransferDestinationSnapshot(
                    DestinationFolderId,
                    "Camera",
                    "Files / Camera"),
                "image/jpeg",
                new CottonTransferSourceSnapshot(
                    CottonTransferSourceKind.CameraBackup,
                    "content://media/external/images/media/100",
                    CameraModifiedAt,
                    100,
                    CameraCapturedAt));
        }

        private static CottonTransferStagedFileSnapshot CreateStagedFile()
        {
            return new CottonTransferStagedFileSnapshot(
                TransferId,
                "upload.bin",
                "/tmp/upload.bin",
                100);
        }

        private class FakeTransferMetadataStore : ICottonTransferMetadataStore
        {
            public FakeTransferMetadataStore(IReadOnlyList<CottonTransferQueueItem> items)
            {
                Items = items.ToList();
            }

            public IReadOnlyList<CottonTransferQueueItem> Items { get; private set; }

            public IReadOnlyList<IReadOnlyList<CottonTransferQueueItem>> SaveSnapshots { get; private set; } = [];

            public Task<IReadOnlyList<CottonTransferQueueItem>> LoadAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Items);
            }

            public Task SaveAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonTransferQueueItem> items,
                CancellationToken cancellationToken = default)
            {
                Items = items.ToList();
                SaveSnapshots = SaveSnapshots.Concat([Items]).ToList();
                return Task.CompletedTask;
            }

            public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
            {
                Items = [];
                return Task.CompletedTask;
            }
        }

        private class FakeTransferStagingStore : ICottonTransferStagingStore
        {
            private CottonTransferStagedFileSnapshot? _stagedFile;

            public FakeTransferStagingStore(CottonTransferStagedFileSnapshot? stagedFile)
            {
                _stagedFile = stagedFile;
            }

            public IReadOnlyList<Guid> DeletedTransferIds { get; private set; } = [];

            public Task<CottonTransferStagedFileSnapshot> StageAsync(
                Uri instanceUri,
                Guid transferId,
                string fileName,
                Stream content,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CottonTransferStagedFileSnapshot?> GetAsync(
                Uri instanceUri,
                Guid transferId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_stagedFile?.TransferId == transferId ? _stagedFile : null);
            }

            public Task<IReadOnlyList<CottonTransferStagedFileSnapshot>> ListAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<CottonTransferStagedFileSnapshot>>(
                    _stagedFile is null ? [] : [_stagedFile]);
            }

            public Task DeleteAsync(Uri instanceUri, Guid transferId, CancellationToken cancellationToken = default)
            {
                if (_stagedFile?.TransferId == transferId)
                {
                    _stagedFile = null;
                    DeletedTransferIds = DeletedTransferIds.Concat([transferId]).ToList();
                }

                return Task.CompletedTask;
            }

            public Task<CottonTransferStagedFileCleanupResult> CleanupAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonTransferQueueItem> queueItems,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CottonTransferStagedFileCleanupResult.Empty);
            }
        }

        private class FakeQueuedUploadClient : ICottonQueuedUploadClient
        {
            private readonly IReadOnlyList<long> _progressValues;
            private readonly Exception? _exception;

            public FakeQueuedUploadClient(IReadOnlyList<long> progressValues, Exception? exception = null)
            {
                _progressValues = progressValues;
                _exception = exception;
            }

            public IReadOnlyList<Guid> UploadedTransferIds { get; private set; } = [];

            public async Task<CottonQueuedUploadClientResult> UploadAsync(
                Uri instanceUri,
                CottonTransferQueueItem transfer,
                CottonTransferStagedFileSnapshot stagedFile,
                Func<long, CancellationToken, Task> reportProgressAsync,
                CancellationToken cancellationToken = default)
            {
                UploadedTransferIds = UploadedTransferIds.Concat([transfer.Id]).ToList();
                foreach (long progressValue in _progressValues)
                {
                    await reportProgressAsync(progressValue, cancellationToken);
                }

                if (_exception is not null)
                {
                    throw _exception;
                }

                return new CottonQueuedUploadClientResult(RemoteFileId, "remote-upload.bin");
            }
        }

        private class FakeCameraBackupUploadedMediaStore : ICottonCameraBackupUploadedMediaStore
        {
            public IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot> Items { get; private set; } = [];

            public Task<IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot>> LoadAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Items);
            }

            public Task SaveAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonCameraBackupUploadedMediaSnapshot> items,
                CancellationToken cancellationToken = default)
            {
                Items = items.ToList();
                return Task.CompletedTask;
            }

            public Task AddOrReplaceAsync(
                Uri instanceUri,
                CottonCameraBackupUploadedMediaSnapshot item,
                CancellationToken cancellationToken = default)
            {
                Items = Items
                    .Where(existing => !existing.Identity.Equals(item.Identity))
                    .Concat([item])
                    .ToList();
                return Task.CompletedTask;
            }

            public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
            {
                Items = [];
                return Task.CompletedTask;
            }
        }

        private class FakeLocalNotificationService : ICottonLocalNotificationService
        {
            private readonly Exception? _exception;

            public FakeLocalNotificationService(Exception? exception = null)
            {
                _exception = exception;
            }

            public IReadOnlyList<CottonLocalNotificationSnapshot> Notifications { get; private set; } = [];

            public Task<CottonLocalNotificationDeliveryResult> ShowAsync(
                CottonLocalNotificationSnapshot notification,
                CancellationToken cancellationToken = default)
            {
                if (_exception is not null)
                {
                    throw _exception;
                }

                Notifications = Notifications.Concat([notification]).ToList();
                return Task.FromResult(CottonLocalNotificationDeliveryResult.Posted);
            }
        }

        private class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
            }

            public override DateTimeOffset GetUtcNow()
            {
                return _utcNow;
            }
        }
    }
}
