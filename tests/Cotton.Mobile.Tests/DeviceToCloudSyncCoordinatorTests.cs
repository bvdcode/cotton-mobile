using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class DeviceToCloudSyncCoordinatorTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid SecondSyncRootId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid SecondFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 17, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SyncedAt = new(2026, 6, 20, 17, 5, 0, DateTimeKind.Utc);

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly FileSystemCottonSyncRootPauseStore _pauseStore;
        private readonly FileSystemCottonSyncedFileManifestStore _manifestStore;
        private readonly FakeDeviceToCloudLocalTreeReader _localTreeReader;
        private readonly FakeDeviceToCloudRemoteFolderContentSource _remoteFolderContentSource;
        private readonly FakeDeviceToCloudFileOperator _fileOperator;
        private readonly CottonDeviceToCloudSyncCoordinator _coordinator;

        public DeviceToCloudSyncCoordinatorTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-device-to-cloud-coordinator-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")));
            _pauseStore = new FileSystemCottonSyncRootPauseStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")));
            _manifestStore = new FileSystemCottonSyncedFileManifestStore(
                new FixedSyncedFileManifestPathProvider(Path.Combine(_directory, "manifest")));
            _localTreeReader = new FakeDeviceToCloudLocalTreeReader();
            _remoteFolderContentSource = new FakeDeviceToCloudRemoteFolderContentSource();
            _fileOperator = new FakeDeviceToCloudFileOperator();
            var executor = new CottonDeviceToCloudSyncPlanExecutor(
                _fileOperator,
                _manifestStore,
                new FixedTimeProvider(SyncedAt));
            _coordinator = new CottonDeviceToCloudSyncCoordinator(
                _rootStore,
                _pauseStore,
                _manifestStore,
                _localTreeReader,
                _remoteFolderContentSource,
                executor);
        }

        [Fact]
        public async Task Run_returns_empty_summary_when_no_roots_are_saved()
        {
            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            Assert.Equal(0, summary.RootCount);
            Assert.Equal(0, summary.CompletedRootCount);
            Assert.Empty(summary.RootResults);
            Assert.Empty(_localTreeReader.ReadRootIds);
            Assert.Empty(_remoteFolderContentSource.RequestedFolderIds);
        }

        [Fact]
        public async Task Run_uploads_new_local_file_and_updates_manifest()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            await _rootStore.SaveAsync(InstanceUri, [root]);
            _localTreeReader.SetContent(
                root.Id,
                CreateLocalContent(CreateLocalFile("alpha.txt", "alpha.txt")));
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root));
            _fileOperator.UploadedNewFiles["alpha.txt"] = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            Assert.Equal(1, summary.RootCount);
            Assert.Equal(1, summary.CompletedRootCount);
            Assert.Equal(1, summary.UploadedCount);
            Assert.True(summary.HasAppliedChanges);
            Assert.Equal([root.Id], _localTreeReader.ReadRootIds);
            Assert.Equal([FolderId], _remoteFolderContentSource.RequestedFolderIds);
            Assert.Equal(["alpha.txt"], _fileOperator.UploadedNewRelativePaths);

            CottonSyncedFileSnapshot manifestItem = Assert.Single(await _manifestStore.LoadAsync(InstanceUri, root));
            Assert.Equal(FirstFileId, manifestItem.FileId);
            Assert.Equal("\"etag-1\"", manifestItem.ETag);
            Assert.Equal(SyncedAt, manifestItem.SyncedAtUtc);
        }

        [Fact]
        public async Task Run_root_uploads_only_the_requested_root()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonSyncRootSnapshot secondRoot = CreateRoot(SecondSyncRootId, SecondFolderId, "Archive");
            await _rootStore.SaveAsync(InstanceUri, [root, secondRoot]);
            _localTreeReader.SetContent(
                root.Id,
                CreateLocalContent(CreateLocalFile("alpha.txt", "alpha.txt")));
            _localTreeReader.SetContent(
                secondRoot.Id,
                CreateLocalContent(CreateLocalFile("beta.txt", "beta.txt")));
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root));
            _remoteFolderContentSource.SetContent(secondRoot.CloudFolder.FolderId, CreateContent(secondRoot));
            _fileOperator.UploadedNewFiles["alpha.txt"] = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");
            _fileOperator.UploadedNewFiles["beta.txt"] = CreateFile(SecondFileId, "beta.txt", "\"etag-2\"");

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            Assert.Equal(1, summary.RootCount);
            Assert.Equal(1, summary.CompletedRootCount);
            Assert.Equal(1, summary.UploadedCount);
            Assert.Equal([root.Id], _localTreeReader.ReadRootIds);
            Assert.Equal([FolderId], _remoteFolderContentSource.RequestedFolderIds);
            Assert.Equal(["alpha.txt"], _fileOperator.UploadedNewRelativePaths);
            Assert.Single(await _manifestStore.LoadAsync(InstanceUri, root));
            Assert.Empty(await _manifestStore.LoadAsync(InstanceUri, secondRoot));
        }

        [Fact]
        public async Task Run_root_skips_not_ready_and_unsupported_direction_without_reads()
        {
            CottonSyncRootSnapshot notReady = CreateRoot(
                SyncRootId,
                FolderId,
                "Projects",
                CottonSyncRootPermissionStatus.Unavailable,
                CottonSyncDirection.DeviceToCloud);
            CottonSyncRootSnapshot cloudToDevice = CreateRoot(
                SecondSyncRootId,
                SecondFolderId,
                "Archive",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.CloudToDevice);

            CottonDeviceToCloudSyncRunSummary notReadySummary =
                await _coordinator.RunRootAsync(InstanceUri, notReady);
            CottonDeviceToCloudSyncRunSummary cloudToDeviceSummary =
                await _coordinator.RunRootAsync(InstanceUri, cloudToDevice);

            Assert.Equal(
                CottonDeviceToCloudSyncRootRunStatus.SkippedNotReady,
                Assert.Single(notReadySummary.RootResults).Status);
            Assert.Equal(
                CottonDeviceToCloudSyncRootRunStatus.SkippedUnsupportedDirection,
                Assert.Single(cloudToDeviceSummary.RootResults).Status);
            Assert.Empty(_localTreeReader.ReadRootIds);
            Assert.Empty(_remoteFolderContentSource.RequestedFolderIds);
        }

        [Fact]
        public async Task Run_root_skips_paused_root_without_reads()
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
        public async Task Run_root_skips_app_private_root_as_unsupported_local_source()
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
        public async Task Run_root_supports_bidirectional_user_selected_roots()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                SyncRootId,
                FolderId,
                "Projects",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.Bidirectional);
            _localTreeReader.SetContent(root.Id, CreateLocalContent(CreateLocalFile("alpha.txt", "alpha.txt")));
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root));
            _fileOperator.UploadedNewFiles["alpha.txt"] = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonDeviceToCloudSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonDeviceToCloudSyncRootRunStatus.Completed, result.Status);
            Assert.Equal(1, summary.UploadedCount);
        }

        [Fact]
        public async Task Run_traverses_remote_folders_before_planning()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonFileBrowserEntry folder = CreateFolder(SecondFileId, "Photos");
            CottonFileBrowserEntry nestedFile = CreateFile(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                "summer.jpg",
                "\"etag-summer\"");
            CottonSyncedFileSnapshot manifestItem = new(
                nestedFile.Id,
                nestedFile.Name,
                nestedFile.ETag!,
                nestedFile.UpdatedAtUtc,
                nestedFile.SizeBytes,
                nestedFile.ContentType,
                SyncedAt,
                "Photos/summer.jpg");
            await _rootStore.SaveAsync(InstanceUri, [root]);
            await _manifestStore.SaveAsync(InstanceUri, root, [manifestItem]);
            _localTreeReader.SetContent(
                root.Id,
                CreateLocalContent(
                    CreateLocalFolder("Photos", "Photos"),
                    CreateLocalFile("summer.jpg", "Photos/summer.jpg")));
            _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root, folder));
            _remoteFolderContentSource.SetContent(
                folder.Id,
                new CottonFolderContent(folder.Id, folder.Name, [nestedFile]));

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            CottonDeviceToCloudSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.True(result.IsCompleted);
            Assert.Equal(2, summary.SkippedItemCount);
            Assert.Equal([FolderId, folder.Id], _remoteFolderContentSource.RequestedFolderIds);
            Assert.Empty(_fileOperator.UploadedNewRelativePaths);
            Assert.False(summary.HasAppliedChanges);
        }

        [Fact]
        public async Task Run_root_requires_review_before_destructive_remote_delete()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonSyncedFileSnapshot manifestItem = new(
                FirstFileId,
                "old.txt",
                "\"etag-old\"",
                UpdatedAt,
                42,
                "text/plain",
                SyncedAt,
                "old.txt");
            await _manifestStore.SaveAsync(InstanceUri, root, [manifestItem]);
            _localTreeReader.SetContent(root.Id, CreateLocalContent());
            _remoteFolderContentSource.SetContent(
                root.CloudFolder.FolderId,
                CreateContent(root, CreateFile(FirstFileId, "old.txt", "\"etag-old\"")));

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonDeviceToCloudSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonDeviceToCloudSyncRootRunStatus.SkippedDestructiveReviewRequired, result.Status);
            Assert.Equal(CottonDeviceToCloudSyncStatusText.DestructiveReviewRequiredStatus, result.StatusText);
            Assert.NotNull(result.Plan);
            Assert.Equal(1, result.Plan.RemoteDeleteCount);
            Assert.Equal(1, summary.SkippedRootCount);
            Assert.Equal(0, summary.DeletedRemoteFileCount);
            Assert.True(summary.NeedsDestructiveReview);
            Assert.Equal(1, summary.DestructiveReviewRemoteDeleteCount);
            Assert.True(summary.HasBlockedItems);
            Assert.Empty(_fileOperator.DeletedFileIds);
            Assert.Single(await _manifestStore.LoadAsync(InstanceUri, root));
        }

        [Fact]
        public async Task Run_root_executes_destructive_remote_delete_when_explicitly_allowed()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonSyncedFileSnapshot manifestItem = new(
                FirstFileId,
                "old.txt",
                "\"etag-old\"",
                UpdatedAt,
                42,
                "text/plain",
                SyncedAt,
                "old.txt");
            await _manifestStore.SaveAsync(InstanceUri, root, [manifestItem]);
            _localTreeReader.SetContent(root.Id, CreateLocalContent());
            _remoteFolderContentSource.SetContent(
                root.CloudFolder.FolderId,
                CreateContent(root, CreateFile(FirstFileId, "old.txt", "\"etag-old\"")));

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunRootAsync(
                InstanceUri,
                root,
                CottonDeviceToCloudSyncRunOptions.AllowRemoteDeletes);

            CottonDeviceToCloudSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonDeviceToCloudSyncRootRunStatus.Completed, result.Status);
            Assert.Equal(1, summary.DeletedRemoteFileCount);
            Assert.False(summary.NeedsDestructiveReview);
            Assert.Equal(0, summary.DestructiveReviewRemoteDeleteCount);
            Assert.True(summary.HasAppliedChanges);
            Assert.Equal([FirstFileId], _fileOperator.DeletedFileIds);
            Assert.Empty(await _manifestStore.LoadAsync(InstanceUri, root));
        }

        [Fact]
        public async Task Run_root_rejects_root_from_another_instance()
        {
            var otherInstanceUri = new Uri("https://files.cottoncloud.dev");
            CottonSyncRootSnapshot root = new(
                SyncRootId,
                otherInstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(
                    FolderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://com.android.externalstorage.documents/tree/primary%3AProjects",
                    "Device folder",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.DeviceToCloud);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _coordinator.RunRootAsync(InstanceUri, root));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid syncRootId,
            Guid folderId,
            string folderName,
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available,
            CottonSyncDirection direction = CottonSyncDirection.DeviceToCloud,
            CottonSyncRootStorageKind storageKind = CottonSyncRootStorageKind.UserSelectedDocumentTree)
        {
            return new CottonSyncRootSnapshot(
                syncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(
                    folderId,
                    folderName,
                    $"Files / {folderName}"),
                new CottonSyncLocalRootSnapshot(
                    storageKind,
                    storageKind == CottonSyncRootStorageKind.AppPrivateDirectory
                        ? $"app-private-sync-root-{folderId:N}"
                        : $"content://com.android.externalstorage.documents/tree/primary%3A{folderName}",
                    storageKind == CottonSyncRootStorageKind.AppPrivateDirectory
                        ? "On this device"
                        : "Device folder",
                    permissionStatus),
                direction);
        }

        private static CottonDeviceToCloudLocalContentSnapshot CreateLocalContent(
            params CottonDeviceToCloudLocalItemSnapshot[] items)
        {
            return new CottonDeviceToCloudLocalContentSnapshot("Device folder", items, problems: []);
        }

        private static CottonDeviceToCloudLocalItemSnapshot CreateLocalFile(string name, string relativePath)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                name,
                relativePath,
                UpdatedAt,
                42,
                "text/plain");
        }

        private static CottonDeviceToCloudLocalItemSnapshot CreateLocalFolder(string name, string relativePath)
        {
            return CottonDeviceToCloudLocalItemSnapshot.CreateFolder(name, relativePath, UpdatedAt);
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

        private static CottonFileBrowserEntry CreateFolder(Guid id, string name)
        {
            return CottonFileBrowserEntry.CreateCached(
                id,
                CottonFileBrowserEntryType.Folder,
                name,
                "Folder",
                "Folder",
                "Open",
                "Folder",
                UpdatedAt,
                sizeBytes: null,
                contentType: null,
                previewHashEncryptedHex: null,
                eTag: null);
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

        private class FakeDeviceToCloudLocalTreeReader : ICottonDeviceToCloudLocalTreeReader
        {
            private readonly Dictionary<Guid, CottonDeviceToCloudLocalContentSnapshot> _contentByRootId = [];

            public List<Guid> ReadRootIds { get; } = [];

            public void SetContent(Guid rootId, CottonDeviceToCloudLocalContentSnapshot content)
            {
                _contentByRootId[rootId] = content;
            }

            public Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CancellationToken cancellationToken = default)
            {
                ReadRootIds.Add(root.Id);
                return Task.FromResult(_contentByRootId[root.Id]);
            }
        }

        private class FakeDeviceToCloudRemoteFolderContentSource : ICottonDeviceToCloudRemoteFolderContentSource
        {
            private readonly Dictionary<Guid, CottonFolderContent> _contentByFolderId = [];

            public List<Guid> RequestedFolderIds { get; } = [];

            public void SetContent(Guid folderId, CottonFolderContent content)
            {
                _contentByFolderId[folderId] = content;
            }

            public Task<CottonFolderContent> LoadAsync(
                Uri instanceUri,
                CottonFolderHandle folder,
                CancellationToken cancellationToken = default)
            {
                RequestedFolderIds.Add(folder.Id);
                return Task.FromResult(_contentByFolderId[folder.Id]);
            }
        }

        private class FakeDeviceToCloudFileOperator : ICottonDeviceToCloudSyncFileOperator
        {
            public Dictionary<string, CottonFileBrowserEntry> UploadedNewFiles { get; } =
                new(StringComparer.Ordinal);

            public List<string> UploadedNewRelativePaths { get; } = [];

            public List<Guid> DeletedFileIds { get; } = [];

            public Task<CottonFileBrowserEntry> UploadNewFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                UploadedNewRelativePaths.Add(item.RelativePath);
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
