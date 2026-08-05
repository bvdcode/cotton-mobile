using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.DeviceToCloudSyncPlanExecutorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class DeviceToCloudSyncPlanExecutorTests : IDisposable
    {
        private readonly string _rootDirectory;
        private readonly CottonSyncRootSnapshot _syncRoot;
        private readonly FileSystemCottonSyncedFileManifestStore _manifestStore;
        private readonly FakeDeviceToCloudFileOperator _fileOperator;
        private readonly CottonDeviceToCloudSyncPlanExecutor _executor;

        public DeviceToCloudSyncPlanExecutorTests()
        {
            _rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "cotton-device-to-cloud-executor-tests",
                Guid.NewGuid().ToString("N"));
            _syncRoot = CreateRoot(RootFolderId);
            _manifestStore = new FileSystemCottonSyncedFileManifestStore(
                new FixedSyncedFileManifestPathProvider(_rootDirectory));
            _fileOperator = new FakeDeviceToCloudFileOperator();
            _executor = new CottonDeviceToCloudSyncPlanExecutor(
                _fileOperator,
                _manifestStore,
                new FixedTimeProvider(SyncedAt));
        }

        [Fact]
        public async Task Executor_uploads_new_file_to_root_and_writes_manifest()
        {
            CottonDeviceToCloudSyncPlanSnapshot plan = CreatePlan(
                CreateUploadNewFile("alpha.txt", "alpha.txt"));
            _fileOperator.UploadedNewFiles["alpha.txt"] = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");

            CottonDeviceToCloudSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.UploadedCount);
            Assert.True(result.HasAppliedChanges);
            UploadCall upload = Assert.Single(_fileOperator.NewUploadCalls);
            Assert.Equal(InstanceUri, upload.InstanceUri);
            Assert.Equal("alpha.txt", upload.Item.RelativePath);
            Assert.Equal(RootFolderId, upload.ParentFolder.Id);

            CottonSyncedFileSnapshot manifestItem =
                Assert.Single(await _manifestStore.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal(FirstFileId, manifestItem.FileId);
            Assert.Equal("alpha.txt", manifestItem.RelativePath);
            Assert.Equal("\"etag-1\"", manifestItem.ETag);
            Assert.Equal(SyncedAt, manifestItem.SyncedAtUtc);
        }

        [Fact]
        public async Task Executor_creates_nested_folder_before_uploading_nested_file()
        {
            CottonDeviceToCloudSyncPlanSnapshot plan = CreatePlan(
                CreateRemoteFolder("Photos", "Photos"),
                CreateUploadNewFile("summer.jpg", "Photos/summer.jpg"));
            _fileOperator.CreatedFolders["Photos"] = CreateFolder(CreatedFolderId, "Photos");
            _fileOperator.UploadedNewFiles["Photos/summer.jpg"] =
                CreateFile(FirstFileId, "summer.jpg", "\"etag-summer\"");

            CottonDeviceToCloudSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.CreatedFolderCount);
            Assert.Equal(1, result.UploadedCount);
            FolderCreateCall createCall = Assert.Single(_fileOperator.FolderCreateCalls);
            Assert.Equal(RootFolderId, createCall.ParentFolder.Id);
            UploadCall uploadCall = Assert.Single(_fileOperator.NewUploadCalls);
            Assert.Equal(CreatedFolderId, uploadCall.ParentFolder.Id);

            CottonSyncedFileSnapshot manifestItem =
                Assert.Single(await _manifestStore.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal("Photos/summer.jpg", manifestItem.RelativePath);
        }

        [Fact]
        public async Task Executor_uses_existing_nested_folder_for_upload_parent()
        {
            CottonDeviceToCloudSyncPlanSnapshot plan = CreatePlan(
                CreateExistingRemoteFolder(ExistingFolderId, "Photos", "Photos"),
                CreateUploadNewFile("summer.jpg", "Photos/summer.jpg"));
            _fileOperator.UploadedNewFiles["Photos/summer.jpg"] =
                CreateFile(FirstFileId, "summer.jpg", "\"etag-summer\"");

            CottonDeviceToCloudSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.SkippedCount);
            Assert.Equal(1, result.UploadedCount);
            Assert.Empty(_fileOperator.FolderCreateCalls);
            UploadCall uploadCall = Assert.Single(_fileOperator.NewUploadCalls);
            Assert.Equal(ExistingFolderId, uploadCall.ParentFolder.Id);
        }

        [Fact]
        public async Task Executor_updates_changed_file_and_replaces_manifest_item()
        {
            CottonSyncedFileSnapshot oldManifest = new(
                SecondFileId,
                "notes.txt",
                "\"etag-old\"",
                UpdatedAt.AddHours(-1),
                12,
                "text/plain",
                UpdatedAt.AddMinutes(-30));
            await _manifestStore.SaveAsync(InstanceUri, _syncRoot, [oldManifest]);
            CottonDeviceToCloudSyncPlanSnapshot plan = CreatePlan(
                CreateUploadChangedFile(SecondFileId, "notes.txt", "notes.txt", "\"etag-old\""));
            _fileOperator.UploadedChangedFiles["notes.txt"] =
                CreateFile(SecondFileId, "notes.txt", "\"etag-new\"");

            CottonDeviceToCloudSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.RefreshedCount);
            Assert.Empty(_fileOperator.NewUploadCalls);
            UpdateCall updateCall = Assert.Single(_fileOperator.ChangedUploadCalls);
            Assert.Equal(SecondFileId, updateCall.Item.CloudItemId);
            Assert.Equal("\"etag-old\"", updateCall.Item.ExpectedRemoteETag);

            CottonSyncedFileSnapshot manifestItem =
                Assert.Single(await _manifestStore.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal("\"etag-new\"", manifestItem.ETag);
            Assert.Equal(SyncedAt, manifestItem.SyncedAtUtc);
        }

        [Fact]
        public async Task Executor_deletes_remote_orphan_and_removes_manifest_item()
        {
            CottonSyncedFileSnapshot manifestItem = new(
                ThirdFileId,
                "old.txt",
                "\"etag-old\"",
                UpdatedAt,
                12,
                "text/plain",
                UpdatedAt);
            await _manifestStore.SaveAsync(InstanceUri, _syncRoot, [manifestItem]);
            CottonDeviceToCloudSyncPlanSnapshot plan = CreatePlan(
                CreateRemoteFileDelete(ThirdFileId, "old.txt", "old.txt", "\"etag-old\""));

            CottonDeviceToCloudSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.DeletedRemoteFileCount);
            Assert.Equal([ThirdFileId], _fileOperator.DeletedFileIds);
            Assert.Empty(await _manifestStore.LoadAsync(InstanceUri, _syncRoot));
        }

        [Fact]
        public async Task Executor_removes_manifest_orphan_without_remote_operation()
        {
            CottonSyncedFileSnapshot manifestItem = new(
                ThirdFileId,
                "old.txt",
                "\"etag-old\"",
                UpdatedAt,
                12,
                "text/plain",
                UpdatedAt);
            await _manifestStore.SaveAsync(InstanceUri, _syncRoot, [manifestItem]);
            CottonDeviceToCloudSyncPlanSnapshot plan = CreatePlan(
                CreateManifestOrphanRemoval(ThirdFileId, "old.txt", "old.txt", "\"etag-old\""));

            CottonDeviceToCloudSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.RemovedManifestCount);
            Assert.Empty(_fileOperator.DeletedFileIds);
            Assert.Empty(await _manifestStore.LoadAsync(InstanceUri, _syncRoot));
        }

        [Fact]
        public async Task Executor_counts_noop_and_blocked_items_without_mutation()
        {
            CottonDeviceToCloudSyncPlanSnapshot plan = CreatePlan(
                CreateExistingFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\""),
                CreateBlockedItem(
                    CottonDeviceToCloudSyncActionKind.RemotePathConflict,
                    "server.txt",
                    "server.txt"),
                CreateBlockedItem(
                    CottonDeviceToCloudSyncActionKind.BlockedLocalItemName,
                    "bad:name.txt",
                    "bad:name.txt"));

            CottonDeviceToCloudSyncExecutionResult result =
                await _executor.ExecuteAsync(InstanceUri, _syncRoot, plan);

            Assert.Equal(1, result.SkippedCount);
            Assert.Equal(2, result.BlockedCount);
            Assert.True(result.HasBlockedItems);
            Assert.Empty(_fileOperator.NewUploadCalls);
            Assert.Empty(_fileOperator.ChangedUploadCalls);
            Assert.Empty(_fileOperator.FolderCreateCalls);
            Assert.Empty(_fileOperator.DeletedFileIds);
        }

        [Fact]
        public async Task Executor_rejects_plan_for_different_root_or_folder()
        {
            CottonDeviceToCloudSyncPlanSnapshot plan = CreatePlan(
                CreateUploadNewFile("alpha.txt", "alpha.txt"));
            CottonSyncRootSnapshot wrongRoot = CreateRoot(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

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
