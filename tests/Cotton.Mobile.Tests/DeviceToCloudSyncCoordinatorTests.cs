using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.DeviceToCloudSyncCoordinatorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class DeviceToCloudSyncCoordinatorTests : IDisposable
    {
        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly FileSystemCottonSyncRootPauseStore _pauseStore;
        private readonly DeviceToCloudCoordinatorUploadReceiptStore _uploadReceiptStore;
        private readonly DeviceToCloudCoordinatorLocalTreeReader _localTreeReader;
        private readonly DeviceToCloudCoordinatorRemoteFolderContentSource _remoteFolderContentSource;
        private readonly DeviceToCloudCoordinatorFileOperator _fileOperator;
        private readonly CottonSyncProgressHub _progressHub;
        private readonly CottonDeviceToCloudSyncCoordinator _coordinator;

        public DeviceToCloudSyncCoordinatorTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-device-to-cloud-coordinator-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")),
                NullLogger<FileSystemCottonSyncRootStore>.Instance, TimeProvider.System);
            _pauseStore = new FileSystemCottonSyncRootPauseStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")),
                NullLogger<FileSystemCottonSyncRootPauseStore>.Instance, TimeProvider.System);
            _uploadReceiptStore = new DeviceToCloudCoordinatorUploadReceiptStore();
            _localTreeReader = new DeviceToCloudCoordinatorLocalTreeReader();
            _remoteFolderContentSource = new DeviceToCloudCoordinatorRemoteFolderContentSource();
            _fileOperator = new DeviceToCloudCoordinatorFileOperator();
            _progressHub = new CottonSyncProgressHub();
            CottonUploadOnlySyncPlanExecutor executor = new(
                _fileOperator,
                new DeviceToCloudCoordinatorLocalFileOperator(),
                _uploadReceiptStore,
                _progressHub,
                new FixedTimeProvider(SyncedAt));
            _coordinator = new CottonDeviceToCloudSyncCoordinator(
                _rootStore,
                _pauseStore,
                _uploadReceiptStore,
                _localTreeReader,
                new CottonRecursiveRemoteContentLoader(_remoteFolderContentSource),
                executor,
                new CottonSyncRootExecutionLock(),
                _progressHub);
        }

        [Fact]
        public async Task RunReturnsEmptySummaryWhenNoRootsAreSaved()
        {
            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            Assert.Equal(0, summary.RootCount);
            Assert.Equal(0, summary.CompletedRootCount);
            Assert.Empty(summary.RootResults);
            Assert.Empty(_localTreeReader.ReadRootIds);
            Assert.Empty(_remoteFolderContentSource.RequestedFolderIds);
        }

        [Fact]
        public async Task RunUploadsNewLocalFileAndRecordsUploadedReceipt()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            await _rootStore.SaveAsync(InstanceUri, [root]);
            _localTreeReader.SetContent(
                root.Id,
                CreateLocalContent(CreateLocalFile("alpha.txt", "alpha.txt", "document:alpha")));
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root));
            _fileOperator.SetUploadResult("alpha.txt", FirstFileId, "\"etag-1\"");

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            Assert.Equal(1, summary.RootCount);
            Assert.Equal(1, summary.CompletedRootCount);
            Assert.Equal(1, summary.UploadedCount);
            Assert.Equal(0, summary.ConfirmedUploadCount);
            Assert.True(summary.HasAppliedChanges);
            Assert.Equal([root.Id], _localTreeReader.ReadRootIds);
            Assert.Equal([FolderId], _remoteFolderContentSource.RequestedFolderIds);
            CottonDeviceToCloudSyncPlanItem uploadedItem = Assert.Single(_fileOperator.UploadedItems);
            Assert.Equal("alpha.txt", uploadedItem.RelativePath);
            Guid uploadOperationId = Assert.IsType<Guid>(uploadedItem.UploadOperationId);

            CottonUploadReceiptSnapshot receipt = Assert.Single(
                await _uploadReceiptStore.LoadAsync(InstanceUri, root));
            Assert.True(receipt.IsUploaded);
            Assert.Equal("document:alpha", receipt.LocalSourceId);
            Assert.Equal(uploadOperationId, receipt.OperationId);
            Assert.Equal(FirstFileId, receipt.RemoteFileId);
            Assert.Equal("\"etag-1\"", receipt.RemoteETag);
            Assert.Equal(SyncedAt, receipt.RecordedAtUtc);
            Assert.Collection(
                _uploadReceiptStore.SavedReceipts,
                pending => Assert.True(pending.IsPending),
                uploaded => Assert.True(uploaded.IsUploaded));
        }

        [Fact]
        public async Task RunDoesNotUploadSameLocalSourceAgainWhenRemoteFileDisappears()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            await _rootStore.SaveAsync(InstanceUri, [root]);
            _localTreeReader.SetContent(
                root.Id,
                CreateLocalContent(CreateLocalFile("photo.jpg", "photo.jpg", "document:photo")));
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root));
            _fileOperator.SetUploadResult("photo.jpg", FirstFileId, "\"etag-photo\"");

            CottonDeviceToCloudSyncRunSummary firstRun = await _coordinator.RunAsync(InstanceUri);
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root));
            CottonDeviceToCloudSyncRunSummary secondRun = await _coordinator.RunAsync(InstanceUri);

            Assert.Equal(1, firstRun.UploadedCount);
            Assert.Equal(0, secondRun.UploadedCount);
            Assert.Equal(1, secondRun.SkippedItemCount);
            Assert.False(secondRun.HasAppliedChanges);
            Assert.Single(_fileOperator.UploadedItems);
            CottonUploadReceiptSnapshot receipt = Assert.Single(
                await _uploadReceiptStore.LoadAsync(InstanceUri, root));
            Assert.True(receipt.IsUploaded);
            Assert.Equal(FirstFileId, receipt.RemoteFileId);
        }

        [Fact]
        public async Task RunRootUploadsOnlyTheRequestedRoot()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonSyncRootSnapshot secondRoot = CreateRoot(SecondSyncRootId, SecondFolderId, "Archive");
            await _rootStore.SaveAsync(InstanceUri, [root, secondRoot]);
            _localTreeReader.SetContent(
                root.Id,
                CreateLocalContent(CreateLocalFile("alpha.txt", "alpha.txt", "document:alpha")));
            _localTreeReader.SetContent(
                secondRoot.Id,
                CreateLocalContent(CreateLocalFile("beta.txt", "beta.txt", "document:beta")));
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root));
            _remoteFolderContentSource.SetContent(secondRoot.CloudFolder.FolderId, CreateContent(secondRoot));
            _fileOperator.SetUploadResult("alpha.txt", FirstFileId, "\"etag-1\"");
            _fileOperator.SetUploadResult("beta.txt", SecondFileId, "\"etag-2\"");

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            Assert.Equal(1, summary.RootCount);
            Assert.Equal(1, summary.UploadedCount);
            Assert.Equal([root.Id], _localTreeReader.ReadRootIds);
            Assert.Equal([FolderId], _remoteFolderContentSource.RequestedFolderIds);
            Assert.Equal("alpha.txt", Assert.Single(_fileOperator.UploadedItems).RelativePath);
            Assert.Single(await _uploadReceiptStore.LoadAsync(InstanceUri, root));
            Assert.Empty(await _uploadReceiptStore.LoadAsync(InstanceUri, secondRoot));
        }

        [Fact]
        public async Task RunRootReportsScanningCloudAndAppliedChanges()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            _localTreeReader.SetContent(
                root.Id,
                CreateLocalContent(CreateLocalFile("alpha.txt", "alpha.txt", "document:alpha")));
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root));
            _fileOperator.SetUploadResult("alpha.txt", FirstFileId, "\"etag-1\"");
            List<CottonSyncProgressSnapshot?> progress = [];
            _progressHub.ProgressChanged += (_, eventArgs) => progress.Add(eventArgs.Progress);

            _ = await _coordinator.RunRootAsync(InstanceUri, root);

            Assert.Collection(
                progress,
                item => Assert.Equal(CottonSyncProgressStage.ScanningDevice, item?.Stage),
                item => Assert.Equal(CottonSyncProgressStage.CheckingCloud, item?.Stage),
                item => AssertProgress(item, completedItemCount: 0, totalItemCount: 1),
                item => AssertProgress(item, completedItemCount: 1, totalItemCount: 1),
                Assert.Null);
        }

        [Fact]
        public async Task RunRootSkipsNotReadyRootWithoutReads()
        {
            CottonSyncRootSnapshot notReady = CreateRoot(
                SyncRootId,
                FolderId,
                "Projects",
                CottonSyncRootPermissionStatus.Unavailable,
                CottonSyncDirection.DeviceToCloud);
            CottonDeviceToCloudSyncRunSummary notReadySummary =
                await _coordinator.RunRootAsync(InstanceUri, notReady);

            Assert.Equal(
                CottonDeviceToCloudSyncRootRunStatus.SkippedNotReady,
                Assert.Single(notReadySummary.RootResults).Status);
            Assert.Empty(_localTreeReader.ReadRootIds);
            Assert.Empty(_remoteFolderContentSource.RequestedFolderIds);
        }

        [Fact]
        public async Task RunRootSkipsPausedRootWithoutReads()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            await _pauseStore.SetPausedAsync(InstanceUri, root.Id, isPaused: true);

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonDeviceToCloudSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonDeviceToCloudSyncRootRunStatus.SkippedPaused, result.Status);
            Assert.Equal("Paused", result.StatusText);
            Assert.Empty(_localTreeReader.ReadRootIds);
            Assert.Empty(_remoteFolderContentSource.RequestedFolderIds);
        }

        [Fact]
        public async Task RunRootSkipsAppPrivateRootAsUnsupportedLocalSource()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                SyncRootId,
                FolderId,
                "Projects",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.DeviceToCloud,
                CottonSyncRootStorageKind.AppPrivateDirectory);

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonDeviceToCloudSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonDeviceToCloudSyncRootRunStatus.SkippedUnsupportedLocalRoot, result.Status);
            Assert.Equal(CottonDeviceToCloudSyncRootCapability.UnsupportedLocalRootStatusText, result.StatusText);
            Assert.Empty(_localTreeReader.ReadRootIds);
            Assert.Empty(_remoteFolderContentSource.RequestedFolderIds);
        }

        [Fact]
        public async Task RunTraversesRemoteFoldersBeforePlanning()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonFileBrowserEntry folder = CreateFolder(SecondFileId, "Photos");
            CottonFileBrowserEntry nestedFile = CreateFile(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                "summer.jpg",
                "\"etag-summer\"");
            await _rootStore.SaveAsync(InstanceUri, [root]);
            _localTreeReader.SetContent(root.Id, CreateLocalContent());
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root, folder));
            _remoteFolderContentSource.SetContent(
                folder.Id,
                new CottonFolderContent(folder.Id, folder.Name, [nestedFile]));

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            CottonDeviceToCloudSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.True(result.IsCompleted);
            Assert.Equal([FolderId, folder.Id], _remoteFolderContentSource.RequestedFolderIds);
            Assert.Empty(_fileOperator.UploadedItems);
            Assert.False(summary.HasAppliedChanges);
        }

        [Fact]
        public async Task RunRootRejectsRootFromAnotherInstance()
        {
            Uri otherInstanceUri = new("https://files.cottoncloud.dev");
            CottonSyncRootSnapshot root = new(
                SyncRootId,
                otherInstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(FolderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://com.android.externalstorage.documents/tree/primary%3AProjects",
                    "Device folder",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.DeviceToCloud,
                CottonUploadOriginalRetention.KeepOriginals);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _coordinator.RunRootAsync(InstanceUri, root));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }

        private static void AssertProgress(
            CottonSyncProgressSnapshot? progress,
            int completedItemCount,
            int totalItemCount)
        {
            Assert.NotNull(progress);
            Assert.Equal(CottonSyncProgressStage.ApplyingChanges, progress.Stage);
            Assert.Equal(completedItemCount, progress.CompletedItemCount);
            Assert.Equal(totalItemCount, progress.TotalItemCount);
        }
    }
}
