using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudToDeviceSyncPlanExecutorTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid ThirdFileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 15, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SyncedAt = new(2026, 6, 20, 15, 5, 0, DateTimeKind.Utc);

        private readonly string _rootDirectory;
        private readonly CottonSyncRootSnapshot _syncRoot;
        private readonly FileSystemCottonSyncedFileManifestStore _manifestStore;
        private readonly FakeCloudToDeviceFileOperator _fileOperator;
        private readonly CottonCloudToDeviceSyncPlanExecutor _executor;

        public CloudToDeviceSyncPlanExecutorTests()
        {
            _rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "cotton-cloud-to-device-executor-tests",
                Guid.NewGuid().ToString("N"));
            _syncRoot = CreateRoot(FolderId);
            _manifestStore = new FileSystemCottonSyncedFileManifestStore(
                new FixedSyncedFileManifestPathProvider(_rootDirectory));
            _fileOperator = new FakeCloudToDeviceFileOperator();
            _executor = new CottonCloudToDeviceSyncPlanExecutor(
                _fileOperator,
                _manifestStore,
                new FixedTimeProvider(SyncedAt));
        }

        [Fact]
        public async Task Executor_downloads_and_refreshes_files_then_updates_manifest()
        {
            CottonFolderContent remote = CreateContent(
                CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""),
                CreateFile(SecondFileId, "beta.txt", "\"etag-2\""));
            CottonSyncedFileSnapshot oldSecond = new(
                SecondFileId,
                "beta.txt",
                "\"etag-old\"",
                UpdatedAt.AddHours(-1),
                42,
                "text/plain",
                UpdatedAt.AddMinutes(-30));
            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                _syncRoot,
                remote,
                [oldSecond]);

            CottonCloudToDeviceSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.DownloadedCount);
            Assert.Equal(1, result.RefreshedCount);
            Assert.True(result.HasAppliedChanges);
            Assert.Equal([FirstFileId, SecondFileId], _fileOperator.DownloadedIds);
            Assert.All(_fileOperator.DownloadedInstanceUris, uri => Assert.Equal(InstanceUri, uri));

            IReadOnlyList<CottonSyncedFileSnapshot> manifest =
                await _manifestStore.LoadAsync(InstanceUri, _syncRoot);
            Assert.Equal([FirstFileId, SecondFileId], manifest.Select(item => item.FileId).Order().ToArray());
            Assert.All(manifest, item => Assert.Equal(SyncedAt, item.SyncedAtUtc));
            Assert.Contains(manifest, item => item.FileId == SecondFileId && item.ETag == "\"etag-2\"");
        }

        [Fact]
        public async Task Executor_renames_local_file_and_updates_manifest()
        {
            CottonSyncedFileSnapshot localFile = new(
                FirstFileId,
                "alpha.txt",
                "\"etag-1\"",
                UpdatedAt,
                42,
                "text/plain",
                UpdatedAt.AddMinutes(1));
            await _manifestStore.SaveAsync(InstanceUri, _syncRoot, [localFile]);
            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                _syncRoot,
                CreateContent(CreateFile(FirstFileId, "renamed.txt", "\"etag-1\"")),
                [localFile]);

            CottonCloudToDeviceSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.RenamedCount);
            Assert.Equal([FirstFileId], _fileOperator.RenamedIds);
            Uri renamedInstanceUri = Assert.Single(_fileOperator.RenamedInstanceUris);
            Assert.Equal(InstanceUri, renamedInstanceUri);
            CottonSyncedFileSnapshot manifestItem = Assert.Single(await _manifestStore.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal("renamed.txt", manifestItem.FileName);
            Assert.Equal(SyncedAt, manifestItem.SyncedAtUtc);
        }

        [Fact]
        public async Task Executor_removes_local_orphan_and_manifest_item()
        {
            CottonSyncedFileSnapshot orphan = new(
                ThirdFileId,
                "orphan.txt",
                "\"etag-old\"",
                UpdatedAt.AddDays(-1),
                100,
                "text/plain",
                UpdatedAt.AddHours(-1));
            await _manifestStore.SaveAsync(InstanceUri, _syncRoot, [orphan]);
            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                _syncRoot,
                CreateContent(),
                [orphan]);

            CottonCloudToDeviceSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.RemovedCount);
            Assert.Equal([ThirdFileId], _fileOperator.RemovedIds);
            Uri removedInstanceUri = Assert.Single(_fileOperator.RemovedInstanceUris);
            Assert.Equal(InstanceUri, removedInstanceUri);
            Assert.Empty(await _manifestStore.LoadAsync(InstanceUri, _syncRoot));
        }

        [Fact]
        public async Task Executor_skips_noop_and_blocked_items_without_file_operations()
        {
            CottonFileBrowserEntry existing = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");
            CottonSyncedFileSnapshot localFile = CottonSyncedFileSnapshot.Create(existing, SyncedAt);
            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                _syncRoot,
                CreateContent(
                    existing,
                    CreateFile(SecondFileId, "missing-etag.txt", eTag: null),
                    CreateFolder("Archive")),
                [localFile]);

            CottonCloudToDeviceSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.SkippedCount);
            Assert.Equal(2, result.BlockedCount);
            Assert.True(result.HasBlockedItems);
            Assert.Empty(_fileOperator.DownloadedIds);
            Assert.Empty(_fileOperator.RenamedIds);
            Assert.Empty(_fileOperator.RemovedIds);
            Assert.Empty(_fileOperator.DownloadedInstanceUris);
            Assert.Empty(_fileOperator.RenamedInstanceUris);
            Assert.Empty(_fileOperator.RemovedInstanceUris);
        }

        [Fact]
        public async Task Executor_rejects_plan_for_different_root_or_folder()
        {
            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                _syncRoot,
                CreateContent(CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"")),
                []);
            CottonSyncRootSnapshot wrongRoot = CreateRoot(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _executor.ExecuteAsync(InstanceUri, wrongRoot, plan));
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
        }

        private static CottonSyncRootSnapshot CreateRoot(Guid folderId)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(
                    folderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    "app-private-sync-root",
                    "On this device",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.CloudToDevice);
        }

        private static CottonFolderContent CreateContent(params CottonFileBrowserEntry[] entries)
        {
            return new CottonFolderContent(FolderId, "Projects", entries);
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

        private static CottonFileBrowserEntry CreateFolder(string name)
        {
            return CottonFileBrowserEntry.CreateCached(
                Guid.NewGuid(),
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

        private class FakeCloudToDeviceFileOperator : ICottonCloudToDeviceSyncFileOperator
        {
            public List<Guid> DownloadedIds { get; } = [];

            public List<Uri> DownloadedInstanceUris { get; } = [];

            public List<Guid> RenamedIds { get; } = [];

            public List<Uri> RenamedInstanceUris { get; } = [];

            public List<Guid> RemovedIds { get; } = [];

            public List<Uri> RemovedInstanceUris { get; } = [];

            public Task DownloadOrReplaceAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonCloudToDeviceSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                DownloadedInstanceUris.Add(instanceUri);
                DownloadedIds.Add(item.TargetId);
                return Task.CompletedTask;
            }

            public Task RenameAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonCloudToDeviceSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                RenamedInstanceUris.Add(instanceUri);
                RenamedIds.Add(item.TargetId);
                return Task.CompletedTask;
            }

            public Task RemoveAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonCloudToDeviceSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                RemovedInstanceUris.Add(instanceUri);
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
