using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.CloudToDeviceSyncPlanExecutorTestData;

namespace Cotton.Mobile.Tests
{
    public class CloudToDeviceSyncPlanExecutorTests : IDisposable
    {
        private readonly string _rootDirectory;
        private readonly CottonSyncRootSnapshot _syncRoot;
        private readonly FileSystemCottonSyncedFileManifestStore _manifestStore;
        private readonly CloudToDevicePlanExecutorFileOperator _fileOperator;
        private readonly CottonCloudToDeviceSyncPlanExecutor _executor;

        public CloudToDeviceSyncPlanExecutorTests()
        {
            _rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "cotton-cloud-to-device-executor-tests",
                Guid.NewGuid().ToString("N"));
            _syncRoot = CreateRoot(FolderId);
            _manifestStore = new FileSystemCottonSyncedFileManifestStore(
                new FixedSyncedFileManifestPathProvider(_rootDirectory),
                NullLogger<FileSystemCottonSyncedFileManifestStore>.Instance, TimeProvider.System);
            _fileOperator = new CloudToDevicePlanExecutorFileOperator();
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
            Assert.Equal([FirstFileId, SecondFileId], [.. manifest.Select(item => item.FileId).Order()]);
            Assert.All(manifest, item => Assert.Equal(SyncedAt, item.SyncedAtUtc));
            Assert.Contains(manifest, item => item.FileId == SecondFileId && item.ETag == "\"etag-2\"");
            Assert.All(manifest, item => Assert.Equal(item.FileName, item.RelativePath));
        }

        [Fact]
        public async Task Executor_refreshes_remote_replacement_and_replaces_manifest_path()
        {
            CottonSyncedFileSnapshot oldLocal = new(
                FirstFileId,
                "alpha.txt",
                "\"etag-old\"",
                UpdatedAt.AddHours(-1),
                42,
                "text/plain",
                UpdatedAt.AddMinutes(-30));
            await _manifestStore.SaveAsync(InstanceUri, _syncRoot, [oldLocal]);
            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                _syncRoot,
                CreateContent(CreateFile(SecondFileId, "alpha.txt", "\"etag-new\"")),
                [oldLocal]);

            CottonCloudToDeviceSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.RefreshedCount);
            Assert.Equal([SecondFileId], _fileOperator.DownloadedIds);
            Assert.Empty(_fileOperator.RemovedIds);
            CottonSyncedFileSnapshot manifestItem = Assert.Single(await _manifestStore.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal(SecondFileId, manifestItem.FileId);
            Assert.Equal("alpha.txt", manifestItem.RelativePath);
            Assert.Equal("\"etag-new\"", manifestItem.ETag);
            Assert.Equal(SyncedAt, manifestItem.SyncedAtUtc);
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
            Assert.Equal("renamed.txt", manifestItem.RelativePath);
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
    }
}
