using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class BidirectionalSyncExecutionPlannerTests
    {
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid NotesFileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid RemoteFileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid RenamedFileId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        private static readonly Guid PhotosFolderId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 19, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SyncedAt = new(2026, 6, 20, 19, 5, 0, DateTimeKind.Utc);

        [Fact]
        public void Create_splits_conflict_free_preflight_into_existing_execution_plans()
        {
            CottonBidirectionalSyncPlanSnapshot preflightPlan = CreatePlan(
                CreateItem(
                    CottonBidirectionalSyncActionKind.RefreshLocalFile,
                    "remote.txt",
                    "remote.txt",
                    RemoteFileId,
                    "\"etag-remote\"",
                    localUpdatedAtUtc: null,
                    remoteUpdatedAtUtc: UpdatedAt,
                    sizeBytes: 10,
                    contentType: "text/plain"),
                CreateItem(
                    CottonBidirectionalSyncActionKind.RenameLocalFile,
                    "renamed.txt",
                    "renamed.txt",
                    RenamedFileId,
                    "\"etag-rename\"",
                    localUpdatedAtUtc: null,
                    remoteUpdatedAtUtc: UpdatedAt,
                    sizeBytes: 12,
                    contentType: "text/plain",
                    previousRelativePath: "old.txt"),
                CreateItem(
                    CottonBidirectionalSyncActionKind.KeepExistingFolder,
                    "Photos",
                    "Photos",
                    PhotosFolderId,
                    expectedRemoteETag: null,
                    localUpdatedAtUtc: UpdatedAt,
                    remoteUpdatedAtUtc: UpdatedAt,
                    sizeBytes: null,
                    contentType: null,
                    targetType: CottonFileBrowserEntryType.Folder),
                CreateItem(
                    CottonBidirectionalSyncActionKind.UploadChangedFile,
                    "notes.txt",
                    "notes.txt",
                    NotesFileId,
                    "\"etag-notes\"",
                    SyncedAt.AddMinutes(1),
                    UpdatedAt,
                    sizeBytes: 42,
                    contentType: "text/plain",
                    localSourceId: "document:notes"),
                CreateItem(
                    CottonBidirectionalSyncActionKind.UploadNewFile,
                    "summer.txt",
                    "Photos/summer.txt",
                    cloudItemId: null,
                    expectedRemoteETag: null,
                    SyncedAt.AddMinutes(2),
                    remoteUpdatedAtUtc: null,
                    sizeBytes: 99,
                    contentType: "text/plain",
                    localSourceId: "document:photos/summer"));

            CottonBidirectionalSyncExecutionPlan executionPlan =
                CottonBidirectionalSyncExecutionPlanner.Create(preflightPlan);

            Assert.True(executionPlan.CanExecute);
            Assert.True(executionPlan.HasCloudToDeviceWork);
            Assert.True(executionPlan.HasDeviceToCloudWork);
            Assert.Equal(2, executionPlan.CloudToDevicePlan.Items.Count);
            Assert.Equal(3, executionPlan.DeviceToCloudPlan.Items.Count);

            Assert.Equal(
                [
                    CottonCloudToDeviceSyncActionKind.RefreshChangedFile,
                    CottonCloudToDeviceSyncActionKind.RenameLocalFile,
                ],
                executionPlan.CloudToDevicePlan.Items.Select(item => item.Action).ToArray());
            Assert.Equal("old.txt", executionPlan.CloudToDevicePlan.Items[1].PreviousRelativePath);

            Assert.Equal(
                [
                    CottonDeviceToCloudSyncActionKind.KeepExistingFolder,
                    CottonDeviceToCloudSyncActionKind.UploadChangedFile,
                    CottonDeviceToCloudSyncActionKind.UploadNewFile,
                ],
                executionPlan.DeviceToCloudPlan.Items.Select(item => item.Action).ToArray());
            Assert.Equal("document:notes", executionPlan.DeviceToCloudPlan.Items[1].LocalSourceId);
            Assert.Equal("document:photos/summer", executionPlan.DeviceToCloudPlan.Items[2].LocalSourceId);
        }

        [Fact]
        public void Create_keeps_blocked_preflight_non_executable()
        {
            CottonBidirectionalSyncPlanSnapshot preflightPlan = CreatePlan(
                CreateItem(
                    CottonBidirectionalSyncActionKind.FileChangedOnBothSides,
                    "notes.txt",
                    "notes.txt",
                    NotesFileId,
                    "\"etag-remote\"",
                    SyncedAt.AddMinutes(1),
                    UpdatedAt,
                    sizeBytes: 42,
                    contentType: "text/plain",
                    localSourceId: "document:notes"));

            CottonBidirectionalSyncExecutionPlan executionPlan =
                CottonBidirectionalSyncExecutionPlanner.Create(preflightPlan);

            Assert.False(executionPlan.CanExecute);
            Assert.False(executionPlan.HasExecutableChanges);
            Assert.Equal(1, executionPlan.BlockedCount);
            Assert.Empty(executionPlan.CloudToDevicePlan.Items);
            Assert.Empty(executionPlan.DeviceToCloudPlan.Items);
        }

        [Fact]
        public void Create_maps_destructive_local_and_remote_deletes()
        {
            CottonBidirectionalSyncPlanSnapshot preflightPlan = CreatePlan(
                CreateItem(
                    CottonBidirectionalSyncActionKind.RemoveLocalFile,
                    "cloud-deleted.txt",
                    "cloud-deleted.txt",
                    RemoteFileId,
                    "\"etag-old\"",
                    localUpdatedAtUtc: null,
                    remoteUpdatedAtUtc: null,
                    sizeBytes: 10,
                    contentType: "text/plain"),
                CreateItem(
                    CottonBidirectionalSyncActionKind.DeleteRemoteFile,
                    "device-deleted.txt",
                    "device-deleted.txt",
                    NotesFileId,
                    "\"etag-notes\"",
                    localUpdatedAtUtc: null,
                    remoteUpdatedAtUtc: UpdatedAt,
                    sizeBytes: 20,
                    contentType: "text/plain"));

            CottonBidirectionalSyncExecutionPlan executionPlan =
                CottonBidirectionalSyncExecutionPlanner.Create(preflightPlan);

            Assert.True(executionPlan.CanExecute);
            Assert.True(executionPlan.HasDestructiveChanges);
            CottonCloudToDeviceSyncPlanItem cloudItem = Assert.Single(executionPlan.CloudToDevicePlan.Items);
            CottonDeviceToCloudSyncPlanItem deviceItem = Assert.Single(executionPlan.DeviceToCloudPlan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.RemoveLocalOrphan, cloudItem.Action);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.DeleteRemoteFile, deviceItem.Action);
            Assert.Equal(NotesFileId, deviceItem.CloudItemId);
            Assert.Equal("\"etag-notes\"", deviceItem.ExpectedRemoteETag);
        }

        private static CottonBidirectionalSyncPlanSnapshot CreatePlan(
            params CottonBidirectionalSyncPlanItem[] items)
        {
            return new CottonBidirectionalSyncPlanSnapshot(
                SyncRootId,
                FolderId,
                "Projects",
                items);
        }

        private static CottonBidirectionalSyncPlanItem CreateItem(
            CottonBidirectionalSyncActionKind action,
            string displayName,
            string relativePath,
            Guid? cloudItemId,
            string? expectedRemoteETag,
            DateTime? localUpdatedAtUtc,
            DateTime? remoteUpdatedAtUtc,
            long? sizeBytes,
            string? contentType,
            CottonFileBrowserEntryType targetType = CottonFileBrowserEntryType.File,
            string? previousRelativePath = null,
            string? localSourceId = null)
        {
            return new CottonBidirectionalSyncPlanItem(
                action,
                targetType,
                displayName,
                relativePath,
                previousRelativePath,
                cloudItemId,
                expectedRemoteETag,
                localUpdatedAtUtc,
                remoteUpdatedAtUtc,
                sizeBytes,
                contentType,
                localSourceId);
        }
    }
}
