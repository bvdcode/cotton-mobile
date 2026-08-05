using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class DeviceToCloudSyncPlanExecutorTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid RootFolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid ExistingFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid CreatedFolderId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid ThirdFileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 15, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SyncedAt = new(2026, 6, 20, 15, 5, 0, DateTimeKind.Utc);

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
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://com.android.externalstorage.documents/tree/primary%3AProjects",
                    "Projects",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.Bidirectional,
                CottonUploadOriginalRetention.KeepOriginals);
        }

        private static CottonDeviceToCloudSyncPlanSnapshot CreatePlan(
            params CottonDeviceToCloudSyncPlanItem[] items)
        {
            return new CottonDeviceToCloudSyncPlanSnapshot(
                SyncRootId,
                RootFolderId,
                "Projects",
                items);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateUploadNewFile(
            string name,
            string relativePath)
        {
            return CreateFilePlanItem(
                CottonDeviceToCloudSyncActionKind.UploadNewFile,
                name,
                relativePath,
                cloudItemId: null,
                expectedRemoteETag: null,
                localUpdatedAtUtc: UpdatedAt,
                localSourceId: $"source:{relativePath}");
        }

        private static CottonDeviceToCloudSyncPlanItem CreateUploadChangedFile(
            Guid cloudItemId,
            string name,
            string relativePath,
            string expectedRemoteETag)
        {
            return CreateFilePlanItem(
                CottonDeviceToCloudSyncActionKind.UploadChangedFile,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                UpdatedAt,
                $"source:{relativePath}");
        }

        private static CottonDeviceToCloudSyncPlanItem CreateExistingFile(
            Guid cloudItemId,
            string name,
            string relativePath,
            string expectedRemoteETag)
        {
            return CreateFilePlanItem(
                CottonDeviceToCloudSyncActionKind.KeepExistingFile,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                UpdatedAt,
                $"source:{relativePath}");
        }

        private static CottonDeviceToCloudSyncPlanItem CreateRemoteFileDelete(
            Guid cloudItemId,
            string name,
            string relativePath,
            string expectedRemoteETag)
        {
            return CreateFilePlanItem(
                CottonDeviceToCloudSyncActionKind.DeleteRemoteFile,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                localUpdatedAtUtc: null,
                localSourceId: null);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateManifestOrphanRemoval(
            Guid cloudItemId,
            string name,
            string relativePath,
            string expectedRemoteETag)
        {
            return CreateFilePlanItem(
                CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                localUpdatedAtUtc: null,
                localSourceId: null);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateBlockedItem(
            CottonDeviceToCloudSyncActionKind action,
            string name,
            string relativePath)
        {
            return CreateFilePlanItem(
                action,
                name,
                relativePath,
                cloudItemId: null,
                expectedRemoteETag: null,
                localUpdatedAtUtc: UpdatedAt,
                localSourceId: $"source:{relativePath}");
        }

        private static CottonDeviceToCloudSyncPlanItem CreateFilePlanItem(
            CottonDeviceToCloudSyncActionKind action,
            string name,
            string relativePath,
            Guid? cloudItemId,
            string? expectedRemoteETag,
            DateTime? localUpdatedAtUtc,
            string? localSourceId)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                action,
                CottonFileBrowserEntryType.File,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                localUpdatedAtUtc,
                42,
                "text/plain",
                localSourceId,
                uploadOperationId: null,
                TestContentHashes.First);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateRemoteFolder(
            string name,
            string relativePath)
        {
            return CreateFolderPlanItem(
                CottonDeviceToCloudSyncActionKind.CreateRemoteFolder,
                name,
                relativePath,
                cloudItemId: null,
                localSourceId: $"source:{relativePath}");
        }

        private static CottonDeviceToCloudSyncPlanItem CreateExistingRemoteFolder(
            Guid cloudItemId,
            string name,
            string relativePath)
        {
            return CreateFolderPlanItem(
                CottonDeviceToCloudSyncActionKind.KeepExistingFolder,
                name,
                relativePath,
                cloudItemId,
                localSourceId: $"source:{relativePath}");
        }

        private static CottonDeviceToCloudSyncPlanItem CreateFolderPlanItem(
            CottonDeviceToCloudSyncActionKind action,
            string name,
            string relativePath,
            Guid? cloudItemId,
            string? localSourceId)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                action,
                CottonFileBrowserEntryType.Folder,
                name,
                relativePath,
                cloudItemId,
                expectedRemoteETag: null,
                UpdatedAt,
                sizeBytes: null,
                contentType: null,
                localSourceId);
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
                eTag,
                TestContentHashes.First);
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

        private class FakeDeviceToCloudFileOperator : ICottonDeviceToCloudSyncFileOperator
        {
            public Dictionary<string, CottonFileBrowserEntry> UploadedNewFiles { get; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, CottonFileBrowserEntry> UploadedChangedFiles { get; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, CottonFileBrowserEntry> CreatedFolders { get; } =
                new(StringComparer.Ordinal);

            public List<UploadCall> NewUploadCalls { get; } = [];

            public List<UpdateCall> ChangedUploadCalls { get; } = [];

            public List<FolderCreateCall> FolderCreateCalls { get; } = [];

            public List<Guid> DeletedFileIds { get; } = [];

            public Task<CottonFileBrowserEntry> UploadNewFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                NewUploadCalls.Add(new UploadCall(instanceUri, item, parentFolder));
                return Task.FromResult(UploadedNewFiles[item.RelativePath]);
            }

            public Task<CottonFileBrowserEntry> UploadChangedFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                ChangedUploadCalls.Add(new UpdateCall(instanceUri, item));
                return Task.FromResult(UploadedChangedFiles[item.RelativePath]);
            }

            public Task<CottonFileBrowserEntry> CreateFolderAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                FolderCreateCalls.Add(new FolderCreateCall(instanceUri, item, parentFolder));
                return Task.FromResult(CreatedFolders[item.RelativePath]);
            }

            public Task DeleteRemoteFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                DeletedFileIds.Add(item.CloudItemId!.Value);
                return Task.CompletedTask;
            }
        }

        private class UploadCall
        {
            public UploadCall(
                Uri instanceUri,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder)
            {
                InstanceUri = instanceUri;
                Item = item;
                ParentFolder = parentFolder;
            }

            public Uri InstanceUri { get; }

            public CottonDeviceToCloudSyncPlanItem Item { get; }

            public CottonFolderHandle ParentFolder { get; }
        }

        private class UpdateCall
        {
            public UpdateCall(Uri instanceUri, CottonDeviceToCloudSyncPlanItem item)
            {
                InstanceUri = instanceUri;
                Item = item;
            }

            public Uri InstanceUri { get; }

            public CottonDeviceToCloudSyncPlanItem Item { get; }
        }

        private class FolderCreateCall
        {
            public FolderCreateCall(
                Uri instanceUri,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder)
            {
                InstanceUri = instanceUri;
                Item = item;
                ParentFolder = parentFolder;
            }

            public Uri InstanceUri { get; }

            public CottonDeviceToCloudSyncPlanItem Item { get; }

            public CottonFolderHandle ParentFolder { get; }
        }

    }
}
