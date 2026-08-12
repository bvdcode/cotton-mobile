using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.CloudToDeviceSyncCoordinatorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudToDeviceSyncCoordinatorTests : CloudToDeviceSyncCoordinatorTestContext
    {
        [Fact]
        public async Task Run_returns_empty_summary_when_no_roots_are_saved()
        {
            CottonCloudToDeviceSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            Assert.Equal(0, summary.RootCount);
            Assert.Equal(0, summary.CompletedRootCount);
            Assert.Empty(summary.RootResults);
            Assert.Empty(_folderContentSource.RequestedFolderIds);
        }

        [Fact]
        public async Task Run_downloads_new_files_and_updates_manifest()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonFileBrowserEntry file = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");
            await _rootStore.SaveAsync(InstanceUri, [root]);
            _folderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root, file));

            CottonCloudToDeviceSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            Assert.Equal(1, summary.RootCount);
            Assert.Equal(1, summary.CompletedRootCount);
            Assert.Equal(1, summary.DownloadedCount);
            Assert.True(summary.HasAppliedChanges);
            Assert.Equal([FolderId], _folderContentSource.RequestedFolderIds);
            Assert.Equal([FirstFileId], _fileOperator.DownloadedIds);

            CottonSyncedFileSnapshot manifestItem = Assert.Single(await _manifestStore.LoadAsync(InstanceUri, root));
            Assert.Equal(FirstFileId, manifestItem.FileId);
            Assert.Equal("\"etag-1\"", manifestItem.ETag);
            Assert.Equal(SyncedAt, manifestItem.SyncedAtUtc);
        }

        [Fact]
        public async Task Run_root_downloads_only_the_requested_root()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonSyncRootSnapshot secondRoot = CreateRoot(SecondSyncRootId, SecondFolderId, "Archive");
            CottonFileBrowserEntry file = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");
            CottonFileBrowserEntry secondFile = CreateFile(SecondFileId, "beta.txt", "\"etag-2\"");
            await _rootStore.SaveAsync(InstanceUri, [root, secondRoot]);
            _folderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root, file));
            _folderContentSource.SetContent(secondRoot.CloudFolder.FolderId, CreateContent(secondRoot, secondFile));

            CottonCloudToDeviceSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            Assert.Equal(1, summary.RootCount);
            Assert.Equal(1, summary.CompletedRootCount);
            Assert.Equal(1, summary.DownloadedCount);
            Assert.Equal([FolderId], _folderContentSource.RequestedFolderIds);
            Assert.Equal([FirstFileId], _fileOperator.DownloadedIds);
            Assert.Single(await _manifestStore.LoadAsync(InstanceUri, root));
            Assert.Empty(await _manifestStore.LoadAsync(InstanceUri, secondRoot));
        }

        [Fact]
        public async Task Run_root_skips_not_ready_and_unsupported_direction_without_remote_reads()
        {
            CottonSyncRootSnapshot notReady = CreateRoot(
                SyncRootId,
                FolderId,
                "Projects",
                CottonSyncRootPermissionStatus.Unavailable,
                CottonSyncDirection.CloudToDevice);
            CottonSyncRootSnapshot deviceToCloud = CreateRoot(
                SecondSyncRootId,
                SecondFolderId,
                "Archive",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.DeviceToCloud);

            CottonCloudToDeviceSyncRunSummary notReadySummary =
                await _coordinator.RunRootAsync(InstanceUri, notReady);
            CottonCloudToDeviceSyncRunSummary deviceToCloudSummary =
                await _coordinator.RunRootAsync(InstanceUri, deviceToCloud);

            Assert.Equal(
                CottonCloudToDeviceSyncRootRunStatus.SkippedNotReady,
                Assert.Single(notReadySummary.RootResults).Status);
            Assert.Equal(
                CottonCloudToDeviceSyncRootRunStatus.SkippedUnsupportedDirection,
                Assert.Single(deviceToCloudSummary.RootResults).Status);
            Assert.Empty(_folderContentSource.RequestedFolderIds);
            Assert.Empty(_fileOperator.DownloadedIds);
        }

        [Fact]
        public async Task Run_root_skips_paused_root_without_remote_reads()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            await _pauseStore.SetPausedAsync(InstanceUri, root.Id, isPaused: true);

            CottonCloudToDeviceSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonCloudToDeviceSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonCloudToDeviceSyncRootRunStatus.SkippedPaused, result.Status);
            Assert.Equal("Paused", result.StatusText);
            Assert.Empty(_folderContentSource.RequestedFolderIds);
            Assert.Empty(_fileOperator.DownloadedIds);
        }

        [Fact]
        public async Task Run_root_skips_legacy_user_selected_document_tree_roots()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                SyncRootId,
                FolderId,
                "Projects",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.CloudToDevice,
                CottonSyncRootStorageKind.UserSelectedDocumentTree);
            CottonCloudToDeviceSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonCloudToDeviceSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonCloudToDeviceSyncRootRunStatus.SkippedUnsupportedLocalRoot, result.Status);
            Assert.Empty(_folderContentSource.RequestedFolderIds);
            Assert.Empty(_fileOperator.DownloadedIds);
        }

        [Fact]
        public async Task Run_root_rejects_root_from_another_instance()
        {
            Uri otherInstanceUri = new Uri("https://files.cottoncloud.dev");
            CottonSyncRootSnapshot root = new(
                SyncRootId,
                otherInstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(
                    FolderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    $"app-private-sync-root-{FolderId:N}",
                    "On this device",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.CloudToDevice,
                CottonUploadOriginalRetention.KeepOriginals);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _coordinator.RunRootAsync(InstanceUri, root));
        }

        [Fact]
        public async Task Run_keeps_existing_manifest_file_without_file_operations()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonFileBrowserEntry file = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");
            await _rootStore.SaveAsync(InstanceUri, [root]);
            await _manifestStore.SaveAsync(InstanceUri, root, [CottonSyncedFileSnapshot.Create(file, SyncedAt)]);
            _folderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root, file));

            CottonCloudToDeviceSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            Assert.Equal(1, summary.CompletedRootCount);
            Assert.Equal(1, summary.SkippedItemCount);
            Assert.False(summary.HasAppliedChanges);
            Assert.Empty(_fileOperator.DownloadedIds);
            Assert.Empty(_fileOperator.RenamedIds);
            Assert.Empty(_fileOperator.RemovedIds);
        }

        [Fact]
        public async Task Run_root_reports_blocked_missing_remote_revision_without_download()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonFileBrowserEntry file = CreateFile(FirstFileId, "alpha.txt", eTag: null);
            _folderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root, file));

            CottonCloudToDeviceSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonCloudToDeviceSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonCloudToDeviceSyncRootRunStatus.Completed, result.Status);
            Assert.Equal(1, result.Plan?.BlockedCount);
            Assert.Equal(1, summary.BlockedItemCount);
            Assert.True(summary.HasBlockedItems);
            Assert.False(summary.HasAppliedChanges);
            Assert.Empty(_fileOperator.DownloadedIds);
            Assert.Empty(_fileOperator.RenamedIds);
            Assert.Empty(_fileOperator.RemovedIds);
            Assert.Empty(await _manifestStore.LoadAsync(InstanceUri, root));
        }

        [Fact]
        public async Task Run_skips_not_ready_and_unsupported_direction_roots_without_remote_reads()
        {
            CottonSyncRootSnapshot notReady = CreateRoot(
                SyncRootId,
                FolderId,
                "Projects",
                CottonSyncRootPermissionStatus.Unavailable,
                CottonSyncDirection.CloudToDevice);
            CottonSyncRootSnapshot deviceToCloud = CreateRoot(
                SecondSyncRootId,
                SecondFolderId,
                "Archive",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.DeviceToCloud);
            await _rootStore.SaveAsync(InstanceUri, [notReady, deviceToCloud]);

            CottonCloudToDeviceSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            Assert.Equal(2, summary.RootCount);
            Assert.Equal(0, summary.CompletedRootCount);
            Assert.Equal(2, summary.SkippedRootCount);
            Assert.True(summary.HasSkippedRoots);
            Assert.Contains(
                summary.RootResults,
                result => result.Status == CottonCloudToDeviceSyncRootRunStatus.SkippedNotReady);
            Assert.Contains(
                summary.RootResults,
                result => result.Status == CottonCloudToDeviceSyncRootRunStatus.SkippedUnsupportedDirection);
            Assert.Empty(_folderContentSource.RequestedFolderIds);
            Assert.Empty(_fileOperator.DownloadedIds);
        }

        [Fact]
        public async Task Run_skips_paused_roots_without_remote_reads()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            await _rootStore.SaveAsync(InstanceUri, [root]);
            await _pauseStore.SetPausedAsync(InstanceUri, root.Id, isPaused: true);

            CottonCloudToDeviceSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            CottonCloudToDeviceSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(1, summary.SkippedRootCount);
            Assert.Equal(0, summary.CompletedRootCount);
            Assert.True(summary.HasSkippedRoots);
            Assert.Equal(CottonCloudToDeviceSyncRootRunStatus.SkippedPaused, result.Status);
            Assert.Empty(_folderContentSource.RequestedFolderIds);
            Assert.Empty(_fileOperator.DownloadedIds);
        }
    }
}
