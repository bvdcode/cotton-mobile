using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudToDeviceSyncCoordinatorTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid SecondSyncRootId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid SecondFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 16, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SyncedAt = new(2026, 6, 20, 16, 5, 0, DateTimeKind.Utc);

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _rootStore;
        private readonly FileSystemCottonSyncRootPauseStore _pauseStore;
        private readonly FileSystemCottonSyncedFileManifestStore _manifestStore;
        private readonly FakeCloudToDeviceFolderContentSource _folderContentSource;
        private readonly FakeCloudToDeviceFileOperator _fileOperator;
        private readonly CottonCloudToDeviceSyncCoordinator _coordinator;

        public CloudToDeviceSyncCoordinatorTests()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "cotton-cloud-to-device-coordinator-tests",
                Guid.NewGuid().ToString("N"));
            _rootStore = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")));
            _pauseStore = new FileSystemCottonSyncRootPauseStore(
                new FixedSyncRootMetadataPathProvider(Path.Combine(_directory, "roots")));
            _manifestStore = new FileSystemCottonSyncedFileManifestStore(
                new FixedSyncedFileManifestPathProvider(Path.Combine(_directory, "manifest")));
            _folderContentSource = new FakeCloudToDeviceFolderContentSource();
            _fileOperator = new FakeCloudToDeviceFileOperator();
            var executor = new CottonCloudToDeviceSyncPlanExecutor(
                _fileOperator,
                _manifestStore,
                new FixedTimeProvider(SyncedAt));
            _coordinator = new CottonCloudToDeviceSyncCoordinator(
                _rootStore,
                _pauseStore,
                _manifestStore,
                _folderContentSource,
                executor);
        }

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
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    $"app-private-sync-root-{FolderId:N}",
                    "On this device",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.CloudToDevice);

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

        [Fact]
        public async Task Run_downloads_nested_folder_files()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonFileBrowserEntry file = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");
            CottonFileBrowserEntry folder = CreateFolder(SecondFileId, "Nested");
            CottonFileBrowserEntry nestedFile = CreateFile(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                "nested.txt",
                "\"etag-nested\"");
            await _rootStore.SaveAsync(InstanceUri, [root]);
            _folderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root, file, folder));
            _folderContentSource.SetContent(
                folder.Id,
                new CottonFolderContent(folder.Id, folder.Name, [nestedFile]));

            CottonCloudToDeviceSyncRunSummary summary = await _coordinator.RunAsync(InstanceUri);

            CottonCloudToDeviceSyncRootRunResult rootResult = Assert.Single(summary.RootResults);
            Assert.True(rootResult.IsCompleted);
            Assert.False(rootResult.HasBlockedItems);
            Assert.Equal(2, summary.DownloadedCount);
            Assert.Equal(0, summary.BlockedItemCount);
            Assert.False(summary.HasBlockedItems);
            Assert.Equal([FolderId, folder.Id], _folderContentSource.RequestedFolderIds);
            Assert.Equal([FirstFileId, nestedFile.Id], _fileOperator.DownloadedIds);
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
            CottonSyncDirection direction = CottonSyncDirection.CloudToDevice)
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
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    $"app-private-sync-root-{folderId:N}",
                    "On this device",
                    permissionStatus),
                direction);
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

        private class FakeCloudToDeviceFolderContentSource : ICottonCloudToDeviceSyncFolderContentSource
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

        private class FakeCloudToDeviceFileOperator : ICottonCloudToDeviceSyncFileOperator
        {
            public List<Guid> DownloadedIds { get; } = [];

            public List<Guid> RenamedIds { get; } = [];

            public List<Guid> RemovedIds { get; } = [];

            public Task DownloadOrReplaceAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonCloudToDeviceSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                DownloadedIds.Add(item.TargetId);
                return Task.CompletedTask;
            }

            public Task RenameAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonCloudToDeviceSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                RenamedIds.Add(item.TargetId);
                return Task.CompletedTask;
            }

            public Task RemoveAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonCloudToDeviceSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                RemovedIds.Add(item.TargetId);
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
