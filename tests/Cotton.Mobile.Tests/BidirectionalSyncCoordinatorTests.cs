using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.BidirectionalSyncCoordinatorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class BidirectionalSyncCoordinatorTests : IDisposable
    {
        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly FileSystemCottonSyncRootPauseStore _pauseStore;
        private readonly FileSystemCottonSyncedFileManifestStore _manifestStore;
        private readonly BidirectionalLocalTreeReader _localTreeReader;
        private readonly BidirectionalRemoteFolderContentSource _remoteFolderContentSource;
        private readonly BidirectionalCloudToDeviceFileOperator _cloudToDeviceFileOperator;
        private readonly BidirectionalDeviceToCloudFileOperator _deviceToCloudFileOperator;
        private readonly CottonBidirectionalSyncCoordinator _coordinator;

        public BidirectionalSyncCoordinatorTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-bidirectional-coordinator-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")),
                NullLogger<FileSystemCottonSyncRootStore>.Instance);
            _pauseStore = new FileSystemCottonSyncRootPauseStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")),
                NullLogger<FileSystemCottonSyncRootPauseStore>.Instance);
            _manifestStore = new FileSystemCottonSyncedFileManifestStore(
                new FixedSyncedFileManifestPathProvider(Path.Combine(_directory, "manifest")),
                NullLogger<FileSystemCottonSyncedFileManifestStore>.Instance);
            _localTreeReader = new BidirectionalLocalTreeReader();
            _remoteFolderContentSource = new BidirectionalRemoteFolderContentSource();
            _cloudToDeviceFileOperator = new BidirectionalCloudToDeviceFileOperator();
            _deviceToCloudFileOperator = new BidirectionalDeviceToCloudFileOperator();

            CottonCloudToDeviceSyncPlanExecutor cloudExecutor = new(
                _cloudToDeviceFileOperator,
                _manifestStore,
                new FixedTimeProvider(SyncedAt));
            CottonDeviceToCloudSyncPlanExecutor deviceExecutor = new(
                _deviceToCloudFileOperator,
                _manifestStore,
                new FixedTimeProvider(SyncedAt));
            _coordinator = new CottonBidirectionalSyncCoordinator(
                _rootStore,
                _pauseStore,
                _manifestStore,
                _localTreeReader,
                _remoteFolderContentSource,
                cloudExecutor,
                deviceExecutor);
        }

        [Fact]
        public async Task Run_root_executes_safe_cloud_and_device_changes()
        {
            CottonSyncRootSnapshot root = CreateRoot();
            _localTreeReader.SetContent(
                root.Id,
                CreateLocalContent(CreateLocalFile("local.txt", "local.txt", "document:local")));
            _remoteFolderContentSource.SetContent(
                root.CloudFolder.FolderId,
                CreateContent(root, CreateFile(RemoteFileId, "remote.txt", "\"etag-remote\"")));
            _deviceToCloudFileOperator.UploadedNewFiles["local.txt"] =
                CreateFile(UploadedFileId, "local.txt", "\"etag-local\"");

            CottonBidirectionalSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonBidirectionalSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonBidirectionalSyncRootRunStatus.Completed, result.Status);
            Assert.Equal(1, summary.DownloadedCount);
            Assert.Equal(1, summary.UploadedCount);
            Assert.True(summary.HasAppliedChanges);
            Assert.False(summary.HasBlockedItems);
            Assert.Equal(["remote.txt"], _cloudToDeviceFileOperator.DownloadedRelativePaths);
            Assert.Equal(["local.txt"], _deviceToCloudFileOperator.UploadedNewRelativePaths);
            Assert.Equal(["document:local"], _deviceToCloudFileOperator.UploadedLocalSourceIds);
            IReadOnlyList<CottonSyncedFileSnapshot> manifest = await _manifestStore.LoadAsync(InstanceUri, root);
            Assert.Contains(manifest, item => item.FileId == RemoteFileId && item.RelativePath == "remote.txt");
            Assert.Contains(manifest, item => item.FileId == UploadedFileId && item.RelativePath == "local.txt");
        }

        [Fact]
        public async Task Run_root_requires_conflict_review_without_mutations()
        {
            CottonSyncRootSnapshot root = CreateRoot();
            _localTreeReader.SetContent(
                root.Id,
                CreateLocalContent(CreateLocalFile("same.txt", "same.txt", "document:same")));
            _remoteFolderContentSource.SetContent(
                root.CloudFolder.FolderId,
                CreateContent(root, CreateFile(RemoteFileId, "same.txt", "\"etag-remote\"")));

            CottonBidirectionalSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonBidirectionalSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonBidirectionalSyncRootRunStatus.SkippedConflictReviewRequired, result.Status);
            Assert.True(summary.NeedsConflictReview);
            Assert.Equal(1, summary.ConflictReviewCount);
            Assert.Empty(_cloudToDeviceFileOperator.DownloadedRelativePaths);
            Assert.Empty(_deviceToCloudFileOperator.UploadedNewRelativePaths);
            Assert.Empty(await _manifestStore.LoadAsync(InstanceUri, root));
        }

        [Fact]
        public async Task Run_root_requires_blocked_review_without_mutations()
        {
            CottonSyncRootSnapshot root = CreateRoot();
            _localTreeReader.SetContent(root.Id, CreateLocalContent());
            _remoteFolderContentSource.SetContent(
                root.CloudFolder.FolderId,
                CreateContent(root, CreateFile(RemoteFileId, "remote.txt", eTag: null)));

            CottonBidirectionalSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonBidirectionalSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonBidirectionalSyncRootRunStatus.SkippedBlockedReviewRequired, result.Status);
            Assert.Equal(CottonBidirectionalSyncStatusText.BlockedReviewRequiredStatus, result.StatusText);
            Assert.False(summary.NeedsConflictReview);
            Assert.Equal(1, summary.BlockedItemCount);
            Assert.Empty(_cloudToDeviceFileOperator.DownloadedRelativePaths);
            Assert.Empty(_deviceToCloudFileOperator.UploadedNewRelativePaths);
            Assert.Empty(await _manifestStore.LoadAsync(InstanceUri, root));
        }

        [Fact]
        public async Task Run_root_requires_destructive_review_before_remote_delete()
        {
            CottonSyncRootSnapshot root = CreateRoot();
            CottonFileBrowserEntry oldFile = CreateFile(OldFileId, "old.txt", "\"etag-old\"");
            await _manifestStore.SaveAsync(InstanceUri, root, [CottonSyncedFileSnapshot.Create(oldFile, SyncedAt)]);
            _localTreeReader.SetContent(root.Id, CreateLocalContent());
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root, oldFile));

            CottonBidirectionalSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonBidirectionalSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonBidirectionalSyncRootRunStatus.SkippedDestructiveReviewRequired, result.Status);
            Assert.True(summary.NeedsDestructiveReview);
            Assert.Equal(1, summary.DestructiveReviewRemoteDeleteCount);
            Assert.Empty(_deviceToCloudFileOperator.DeletedFileIds);
            Assert.Single(await _manifestStore.LoadAsync(InstanceUri, root));
        }

        [Fact]
        public async Task Run_root_executes_destructive_delete_when_explicitly_allowed()
        {
            CottonSyncRootSnapshot root = CreateRoot();
            CottonFileBrowserEntry oldFile = CreateFile(OldFileId, "old.txt", "\"etag-old\"");
            await _manifestStore.SaveAsync(InstanceUri, root, [CottonSyncedFileSnapshot.Create(oldFile, SyncedAt)]);
            _localTreeReader.SetContent(root.Id, CreateLocalContent());
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root, oldFile));

            CottonBidirectionalSyncRunSummary summary = await _coordinator.RunRootAsync(
                InstanceUri,
                root,
                CottonBidirectionalSyncRunOptions.AllowDestructiveDeletes);

            Assert.Equal(CottonBidirectionalSyncRootRunStatus.Completed, Assert.Single(summary.RootResults).Status);
            Assert.Equal(1, summary.DeletedRemoteFileCount);
            Assert.False(summary.NeedsDestructiveReview);
            Assert.Equal([OldFileId], _deviceToCloudFileOperator.DeletedFileIds);
            Assert.Empty(await _manifestStore.LoadAsync(InstanceUri, root));
        }

        [Fact]
        public async Task Run_root_skips_roots_that_cannot_run()
        {
            CottonSyncRootSnapshot notReady = CreateRoot(
                permissionStatus: CottonSyncRootPermissionStatus.NeedsUserGrant);
            CottonSyncRootSnapshot appPrivateRoot = CreateRoot(
                rootId: Guid.Parse("77777777-7777-7777-7777-777777777777"),
                storageKind: CottonSyncRootStorageKind.AppPrivateDirectory,
                rootKey: "app-private-bidirectional");
            CottonSyncRootSnapshot cloudToDevice = CreateRoot(
                rootId: Guid.Parse("88888888-8888-8888-8888-888888888888"),
                direction: CottonSyncDirection.CloudToDevice);
            await _pauseStore.SetPausedAsync(InstanceUri, notReady.Id, isPaused: true);

            CottonBidirectionalSyncRunSummary pausedSummary = await _coordinator.RunRootAsync(InstanceUri, notReady);
            CottonBidirectionalSyncRunSummary notReadySummary = await _coordinator.RunRootAsync(
                InstanceUri,
                CreateRoot(
                    rootId: Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    permissionStatus: CottonSyncRootPermissionStatus.NeedsUserGrant));
            CottonBidirectionalSyncRunSummary appPrivateSummary =
                await _coordinator.RunRootAsync(InstanceUri, appPrivateRoot);
            CottonBidirectionalSyncRunSummary cloudToDeviceSummary =
                await _coordinator.RunRootAsync(InstanceUri, cloudToDevice);

            Assert.Equal(
                CottonBidirectionalSyncRootRunStatus.SkippedPaused,
                Assert.Single(pausedSummary.RootResults).Status);
            Assert.Equal(
                CottonBidirectionalSyncRootRunStatus.SkippedNotReady,
                Assert.Single(notReadySummary.RootResults).Status);
            Assert.Equal(
                CottonBidirectionalSyncRootRunStatus.SkippedUnsupportedLocalRoot,
                Assert.Single(appPrivateSummary.RootResults).Status);
            Assert.Equal(
                CottonBidirectionalSyncRootRunStatus.SkippedUnsupportedDirection,
                Assert.Single(cloudToDeviceSummary.RootResults).Status);
            Assert.Empty(_cloudToDeviceFileOperator.DownloadedRelativePaths);
            Assert.Empty(_deviceToCloudFileOperator.UploadedNewRelativePaths);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

    }
}
