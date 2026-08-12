// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncExecutionWorkflowTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example.com");

        [Fact]
        public async Task RunRootAsyncPassesCancellationTokenToSelectedCoordinator()
        {
            RecordingCloudToDeviceSyncCoordinator cloudCoordinator = new();
            RecordingDeviceToCloudSyncCoordinator deviceCoordinator = new();
            RecordingBidirectionalSyncCoordinator bidirectionalCoordinator = new();
            SyncExecutionWorkflow workflow = CreateWorkflow(
                cloudCoordinator,
                deviceCoordinator,
                bidirectionalCoordinator);
            using CancellationTokenSource cancellation = new();

            await workflow.RunRootAsync(
                InstanceUri,
                CreateRoot(CottonSyncDirection.CloudToDevice),
                reportStatus: _ => { },
                cancellation.Token);

            Assert.Equal(1, cloudCoordinator.RunRootCount);
            Assert.Equal(cancellation.Token, cloudCoordinator.LastCancellationToken);
            Assert.Equal(0, deviceCoordinator.RunRootCount);
            Assert.Equal(0, bidirectionalCoordinator.RunRootCount);
        }

        [Fact]
        public async Task RunAllAsyncStopsBeforeNextRootAfterCancellation()
        {
            using CancellationTokenSource cancellation = new();
            RecordingCloudToDeviceSyncCoordinator cloudCoordinator = new()
            {
                OnRunRoot = cancellation.Cancel,
            };
            SyncExecutionWorkflow workflow = CreateWorkflow(
                cloudCoordinator,
                new RecordingDeviceToCloudSyncCoordinator(),
                new RecordingBidirectionalSyncCoordinator());
            CottonSyncRootSnapshot[] roots =
            [
                CreateRoot(CottonSyncDirection.CloudToDevice),
                CreateRoot(CottonSyncDirection.CloudToDevice),
            ];

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => workflow.RunAllAsync(
                    InstanceUri,
                    roots,
                    reportStatus: _ => { },
                    cancellation.Token));

            Assert.Equal(1, cloudCoordinator.RunRootCount);
        }

        [Fact]
        public async Task RunRootAsyncRejectsCancellationBeforeDispatch()
        {
            RecordingCloudToDeviceSyncCoordinator cloudCoordinator = new();
            SyncExecutionWorkflow workflow = CreateWorkflow(
                cloudCoordinator,
                new RecordingDeviceToCloudSyncCoordinator(),
                new RecordingBidirectionalSyncCoordinator());
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => workflow.RunRootAsync(
                    InstanceUri,
                    CreateRoot(CottonSyncDirection.CloudToDevice),
                    reportStatus: _ => { },
                    cancellation.Token));

            Assert.Equal(0, cloudCoordinator.RunRootCount);
        }

        private static SyncExecutionWorkflow CreateWorkflow(
            ICottonCloudToDeviceSyncCoordinator cloudCoordinator,
            ICottonDeviceToCloudSyncCoordinator deviceCoordinator,
            ICottonBidirectionalSyncCoordinator bidirectionalCoordinator)
        {
            return new SyncExecutionWorkflow(
                cloudCoordinator,
                deviceCoordinator,
                bidirectionalCoordinator,
                new StubUserDialogService());
        }

        private static CottonSyncRootSnapshot CreateRoot(CottonSyncDirection direction)
        {
            return new CottonSyncRootSnapshot(
                Guid.NewGuid(),
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(Guid.NewGuid(), "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/projects",
                    "Projects",
                    CottonSyncRootPermissionStatus.Available),
                direction,
                CottonUploadOriginalRetention.KeepOriginals);
        }
    }
}
