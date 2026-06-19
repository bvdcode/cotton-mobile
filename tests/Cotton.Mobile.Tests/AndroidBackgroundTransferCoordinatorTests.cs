using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AndroidBackgroundTransferCoordinatorTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid TransferId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid ShareItemId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 19, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ReceivedAt = new(2026, 6, 19, 18, 30, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Share_inbox_upload_uses_user_initiated_host_on_android_14_plus()
        {
            CottonTransferQueueItem transfer = CreateQueuedUpload(
                source: CottonTransferSourceSnapshot.CreateShareInbox(
                    ShareItemId,
                    ReceivedAt,
                    sizeBytes: 123));
            var host = new FakeBackgroundTransferHost();
            CottonAndroidBackgroundTransferCoordinator coordinator = CreateCoordinator(
                [transfer],
                host);

            CottonAndroidBackgroundTransferScheduleResult result =
                await coordinator.ScheduleNextQueuedUploadAsync(InstanceUri, androidApiLevel: 34);

            Assert.True(result.IsScheduled);
            CottonAndroidBackgroundTransferRequest request = Assert.Single(host.Requests);
            Assert.Equal(CottonAndroidTransferWorkKind.ShareInboxUpload, request.WorkKind);
            Assert.Equal(CottonAndroidTransferExecutionHost.UserInitiatedDataTransfer, request.Host);
            Assert.Equal(TransferId, request.TransferId);
            Assert.Equal("upload.bin", request.DisplayName);
            Assert.Equal(1239595930, request.ScheduleIdentity.JobId);
            Assert.Equal(
                "cotton-transfer-d20f82574bbbb5a5-aaaaaaaabbbbccccddddeeeeeeeeeeee",
                request.ScheduleIdentity.UniqueWorkName);
            Assert.Equal(
                "cotton-transfer-aaaaaaaabbbbccccddddeeeeeeeeeeee",
                request.ScheduleIdentity.TransferTag);
            Assert.DoesNotContain("app.cottoncloud.dev", request.ScheduleIdentity.UniqueWorkName);
            Assert.Equal(123, request.EstimatedUploadBytes);
            Assert.True(request.RequiresNetwork);
            Assert.False(request.RequiresUnmeteredNetwork);
            Assert.False(request.RequiresCharging);
            Assert.Same(request, result.Request);
        }

        [Fact]
        public async Task Camera_backup_upload_uses_workmanager_host_with_saved_constraints()
        {
            CottonTransferQueueItem transfer = CreateQueuedUpload(
                source: new CottonTransferSourceSnapshot(
                    CottonTransferSourceKind.CameraBackup,
                    "content://media/external/images/media/42",
                    CreatedAt.AddMinutes(-5),
                    sizeBytes: 456,
                    capturedAtUtc: CreatedAt.AddMinutes(-10)));
            var settings = new CottonCameraBackupSettings(
                isEnabled: false,
                destination: null,
                photosOnly: true,
                wifiOnly: true,
                allowCellular: false,
                chargingOnly: true);
            var host = new FakeBackgroundTransferHost();
            CottonAndroidBackgroundTransferCoordinator coordinator = CreateCoordinator(
                [transfer],
                host,
                settings);

            CottonAndroidBackgroundTransferScheduleResult result =
                await coordinator.ScheduleNextQueuedUploadAsync(InstanceUri, androidApiLevel: 36);

            Assert.True(result.IsScheduled);
            CottonAndroidBackgroundTransferRequest request = Assert.Single(host.Requests);
            Assert.Equal(CottonAndroidTransferWorkKind.CameraBackupUpload, request.WorkKind);
            Assert.Equal(CottonAndroidTransferExecutionHost.WorkManagerConstrained, request.Host);
            Assert.Equal(456, request.EstimatedUploadBytes);
            Assert.True(request.RequiresNetwork);
            Assert.True(request.RequiresUnmeteredNetwork);
            Assert.True(request.RequiresCharging);
        }

        [Fact]
        public async Task Camera_backup_scheduler_skips_manual_uploads()
        {
            Guid cameraTransferId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000");
            CottonTransferQueueItem manualTransfer = CreateQueuedUpload(
                TransferId,
                source: null);
            CottonTransferQueueItem cameraTransfer = CreateQueuedUpload(
                cameraTransferId,
                source: new CottonTransferSourceSnapshot(
                    CottonTransferSourceKind.CameraBackup,
                    "content://media/external/images/media/42",
                    CreatedAt.AddMinutes(-5),
                    sizeBytes: 456,
                    capturedAtUtc: CreatedAt.AddMinutes(-10)));
            var host = new FakeBackgroundTransferHost();
            CottonAndroidBackgroundTransferCoordinator coordinator = CreateCoordinator(
                [manualTransfer, cameraTransfer],
                host);

            CottonAndroidBackgroundTransferScheduleResult result =
                await coordinator.ScheduleNextQueuedCameraBackupUploadAsync(InstanceUri, androidApiLevel: 36);

            Assert.True(result.IsScheduled);
            CottonAndroidBackgroundTransferRequest request = Assert.Single(host.Requests);
            Assert.Equal(cameraTransferId, request.TransferId);
            Assert.Equal(CottonAndroidTransferWorkKind.CameraBackupUpload, request.WorkKind);
            Assert.Equal(CottonAndroidTransferExecutionHost.WorkManagerConstrained, request.Host);
        }

        [Fact]
        public void Schedule_identity_is_stable_and_transfer_scoped()
        {
            CottonAndroidBackgroundTransferScheduleIdentity first =
                CottonAndroidBackgroundTransferScheduleIdentity.Create(InstanceUri, TransferId);
            CottonAndroidBackgroundTransferScheduleIdentity second =
                CottonAndroidBackgroundTransferScheduleIdentity.Create(InstanceUri, TransferId);
            CottonAndroidBackgroundTransferScheduleIdentity otherTransfer =
                CottonAndroidBackgroundTransferScheduleIdentity.Create(
                    InstanceUri,
                    Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000"));

            Assert.Equal(first.JobId, second.JobId);
            Assert.Equal(first.UniqueWorkName, second.UniqueWorkName);
            Assert.Equal(first.TransferTag, second.TransferTag);
            Assert.NotEqual(first.JobId, otherTransfer.JobId);
            Assert.NotEqual(first.UniqueWorkName, otherTransfer.UniqueWorkName);
            Assert.True(first.JobId > 0);
        }

        [Fact]
        public async Task Manual_upload_before_android_14_requires_foreground_execution_without_calling_host()
        {
            CottonTransferQueueItem transfer = CreateQueuedUpload(source: null);
            var host = new FakeBackgroundTransferHost();
            CottonAndroidBackgroundTransferCoordinator coordinator = CreateCoordinator(
                [transfer],
                host);

            CottonAndroidBackgroundTransferScheduleResult result =
                await coordinator.ScheduleNextQueuedUploadAsync(InstanceUri, androidApiLevel: 33);

            Assert.Equal(CottonAndroidBackgroundTransferScheduleStatus.ForegroundRequired, result.Status);
            Assert.Empty(host.Requests);
            Assert.Equal(CottonAndroidTransferWorkKind.ManualUpload, result.Request?.WorkKind);
            Assert.Equal(CottonAndroidTransferExecutionHost.ForegroundManual, result.Request?.Host);
        }

        [Fact]
        public async Task Returns_no_queued_transfer_when_queue_has_no_waiting_uploads()
        {
            CottonTransferQueueItem failed = CreateQueuedUpload(source: null)
                .Start(CreatedAt.AddSeconds(1))
                .Fail("Offline", CreatedAt.AddSeconds(2));
            var host = new FakeBackgroundTransferHost();
            CottonAndroidBackgroundTransferCoordinator coordinator = CreateCoordinator(
                [failed],
                host);

            CottonAndroidBackgroundTransferScheduleResult result =
                await coordinator.ScheduleNextQueuedUploadAsync(InstanceUri, androidApiLevel: 36);

            Assert.Equal(CottonAndroidBackgroundTransferScheduleStatus.NoQueuedTransfer, result.Status);
            Assert.Null(result.Request);
            Assert.Empty(host.Requests);
        }

        [Fact]
        public async Task Disabled_host_returns_unsupported_without_marking_request_scheduled()
        {
            CottonTransferQueueItem transfer = CreateQueuedUpload(
                source: CottonTransferSourceSnapshot.CreateShareInbox(
                    ShareItemId,
                    ReceivedAt,
                    sizeBytes: 123));
            CottonAndroidBackgroundTransferCoordinator coordinator = CreateCoordinator(
                [transfer],
                DisabledCottonAndroidBackgroundTransferHost.Instance);

            CottonAndroidBackgroundTransferScheduleResult result =
                await coordinator.ScheduleNextQueuedUploadAsync(InstanceUri, androidApiLevel: 34);

            Assert.Equal(CottonAndroidBackgroundTransferScheduleStatus.Unsupported, result.Status);
            Assert.False(result.IsScheduled);
            Assert.Equal(CottonAndroidTransferExecutionHost.UserInitiatedDataTransfer, result.Request?.Host);
        }

        private static CottonAndroidBackgroundTransferCoordinator CreateCoordinator(
            IReadOnlyList<CottonTransferQueueItem> transfers,
            ICottonAndroidBackgroundTransferHost host,
            CottonCameraBackupSettings? settings = null)
        {
            return new CottonAndroidBackgroundTransferCoordinator(
                new FakeTransferMetadataStore(transfers),
                new FakeCameraBackupSettingsStore(settings ?? CottonCameraBackupSettings.Default),
                host);
        }

        private static CottonTransferQueueItem CreateQueuedUpload(CottonTransferSourceSnapshot? source)
        {
            return CreateQueuedUpload(TransferId, source);
        }

        private static CottonTransferQueueItem CreateQueuedUpload(
            Guid transferId,
            CottonTransferSourceSnapshot? source)
        {
            return CottonTransferQueueItem.CreateUpload(
                transferId,
                "upload.bin",
                totalBytes: source?.SizeBytes ?? 100,
                CreatedAt,
                new CottonTransferDestinationSnapshot(
                    Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    "Default",
                    "Default"),
                "application/octet-stream",
                source);
        }

        private sealed class FakeTransferMetadataStore : ICottonTransferMetadataStore
        {
            private readonly IReadOnlyList<CottonTransferQueueItem> _items;

            public FakeTransferMetadataStore(IReadOnlyList<CottonTransferQueueItem> items)
            {
                _items = items;
            }

            public Task<IReadOnlyList<CottonTransferQueueItem>> LoadAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_items);
            }

            public Task SaveAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonTransferQueueItem> items,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task ClearAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class FakeCameraBackupSettingsStore : ICottonCameraBackupSettingsStore
        {
            private readonly CottonCameraBackupSettings _settings;

            public FakeCameraBackupSettingsStore(CottonCameraBackupSettings settings)
            {
                _settings = settings;
            }

            public Task<CottonCameraBackupSettings> GetAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_settings);
            }

            public Task SaveAsync(
                Uri instanceUri,
                CottonCameraBackupSettings settings,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class FakeBackgroundTransferHost : ICottonAndroidBackgroundTransferHost
        {
            public List<CottonAndroidBackgroundTransferRequest> Requests { get; } = [];

            public Task<CottonAndroidBackgroundTransferScheduleResult> ScheduleAsync(
                CottonAndroidBackgroundTransferRequest request,
                CancellationToken cancellationToken = default)
            {
                Requests.Add(request);
                return Task.FromResult(
                    CottonAndroidBackgroundTransferScheduleResult.Scheduled(
                        request,
                        "Scheduled Android background transfer."));
            }
        }
    }
}
