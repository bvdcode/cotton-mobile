using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class BidirectionalSyncCoordinatorTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid RemoteFileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid UploadedFileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid OldFileId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 20, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SyncedAt = new(2026, 6, 20, 20, 5, 0, DateTimeKind.Utc);

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly FileSystemCottonSyncRootPauseStore _pauseStore;
        private readonly FileSystemCottonSyncedFileManifestStore _manifestStore;
        private readonly FakeLocalTreeReader _localTreeReader;
        private readonly FakeRemoteFolderContentSource _remoteFolderContentSource;
        private readonly FakeCloudToDeviceFileOperator _cloudToDeviceFileOperator;
        private readonly FakeDeviceToCloudFileOperator _deviceToCloudFileOperator;
        private readonly CottonBidirectionalSyncCoordinator _coordinator;

        public BidirectionalSyncCoordinatorTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-bidirectional-coordinator-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")));
            _pauseStore = new FileSystemCottonSyncRootPauseStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")));
            _manifestStore = new FileSystemCottonSyncedFileManifestStore(
                new FixedSyncedFileManifestPathProvider(Path.Combine(_directory, "manifest")));
            _localTreeReader = new FakeLocalTreeReader();
            _remoteFolderContentSource = new FakeRemoteFolderContentSource();
            _cloudToDeviceFileOperator = new FakeCloudToDeviceFileOperator();
            _deviceToCloudFileOperator = new FakeDeviceToCloudFileOperator();

            var cloudExecutor = new CottonCloudToDeviceSyncPlanExecutor(
                _cloudToDeviceFileOperator,
                _manifestStore,
                new FixedTimeProvider(SyncedAt));
            var deviceExecutor = new CottonDeviceToCloudSyncPlanExecutor(
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

        private static CottonSyncRootSnapshot CreateRoot(
            Guid? rootId = null,
            CottonSyncDirection direction = CottonSyncDirection.Bidirectional,
            CottonSyncRootStorageKind storageKind = CottonSyncRootStorageKind.UserSelectedDocumentTree,
            string rootKey = "content://tree/projects",
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available)
        {
            return new CottonSyncRootSnapshot(
                rootId ?? SyncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(FolderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    storageKind,
                    rootKey,
                    "Projects",
                    permissionStatus),
                direction);
        }

        private static CottonDeviceToCloudLocalContentSnapshot CreateLocalContent(
            params CottonDeviceToCloudLocalItemSnapshot[] items)
        {
            return new CottonDeviceToCloudLocalContentSnapshot("Projects", items);
        }

        private static CottonDeviceToCloudLocalItemSnapshot CreateLocalFile(
            string name,
            string relativePath,
            string localSourceId)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                name,
                relativePath,
                UpdatedAt,
                42,
                "text/plain",
                localSourceId);
        }

        private static CottonFolderContent CreateContent(
            CottonSyncRootSnapshot root,
            params CottonFileBrowserEntry[] entries)
        {
            return new CottonFolderContent(root.CloudFolder.FolderId, root.CloudFolder.FolderName, entries);
        }

        private static CottonFileBrowserEntry CreateFile(Guid id, string name, string? eTag)
        {
            return CottonFileBrowserEntry.CreateCached(
                id,
                CottonFileBrowserEntryType.File,
                name,
                "Text",
                "42 B · Text",
                "More",
                "TXT",
                UpdatedAt,
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag);
        }

        private class FixedSyncRootMetadataPathProvider : ICottonSyncRootMetadataPathProvider
        {
            private readonly string _directory;

            public FixedSyncRootMetadataPathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateSyncRootMetadataDirectory(Uri instanceUri)
            {
                return _directory;
            }
        }

        private class FixedSyncedFileManifestPathProvider : ICottonSyncedFileManifestPathProvider
        {
            private readonly string _rootDirectory;

            public FixedSyncedFileManifestPathProvider(string rootDirectory)
            {
                _rootDirectory = rootDirectory;
            }

            public string CreateSyncedFileManifestDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
            {
                return Path.Combine(_rootDirectory, instanceUri.Host, root.StableKey);
            }
        }

        private class FakeLocalTreeReader : ICottonDeviceToCloudLocalTreeReader
        {
            private readonly Dictionary<Guid, CottonDeviceToCloudLocalContentSnapshot> _contentByRootId = [];

            public void SetContent(Guid rootId, CottonDeviceToCloudLocalContentSnapshot content)
            {
                _contentByRootId[rootId] = content;
            }

            public Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_contentByRootId[root.Id]);
            }
        }

        private class FakeRemoteFolderContentSource : ICottonDeviceToCloudRemoteFolderContentSource
        {
            private readonly Dictionary<Guid, CottonFolderContent> _contentByFolderId = [];

            public void SetContent(Guid folderId, CottonFolderContent content)
            {
                _contentByFolderId[folderId] = content;
            }

            public Task<CottonFolderContent> LoadAsync(
                Uri instanceUri,
                CottonFolderHandle folder,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_contentByFolderId[folder.Id]);
            }
        }

        private class FakeCloudToDeviceFileOperator : ICottonCloudToDeviceSyncFileOperator
        {
            public List<string> DownloadedRelativePaths { get; } = [];

            public Task DownloadOrReplaceAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonCloudToDeviceSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                DownloadedRelativePaths.Add(item.RelativePath);
                return Task.CompletedTask;
            }

            public Task RenameAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonCloudToDeviceSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("Rename is not used by these tests.");
            }

            public Task RemoveAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonCloudToDeviceSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("Local remove is not used by these tests.");
            }
        }

        private class FakeDeviceToCloudFileOperator : ICottonDeviceToCloudSyncFileOperator
        {
            public Dictionary<string, CottonFileBrowserEntry> UploadedNewFiles { get; } =
                new(StringComparer.Ordinal);

            public List<string> UploadedNewRelativePaths { get; } = [];

            public List<string?> UploadedLocalSourceIds { get; } = [];

            public List<Guid> DeletedFileIds { get; } = [];

            public Task<CottonFileBrowserEntry> UploadNewFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                UploadedNewRelativePaths.Add(item.RelativePath);
                UploadedLocalSourceIds.Add(item.LocalSourceId);
                return Task.FromResult(UploadedNewFiles[item.RelativePath]);
            }

            public Task<CottonFileBrowserEntry> UploadChangedFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("Changed uploads are not used by these tests.");
            }

            public Task<CottonFileBrowserEntry> CreateFolderAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("Folder creation is not used by these tests.");
            }

            public Task DeleteRemoteFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                if (item.CloudItemId.HasValue)
                {
                    DeletedFileIds.Add(item.CloudItemId.Value);
                }

                return Task.CompletedTask;
            }
        }

        private class FixedTimeProvider : TimeProvider
        {
            private readonly DateTime _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
            }

            public override DateTimeOffset GetUtcNow()
            {
                return new DateTimeOffset(_utcNow);
            }
        }
    }
}
