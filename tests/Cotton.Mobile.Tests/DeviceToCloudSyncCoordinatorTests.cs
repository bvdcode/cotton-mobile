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
        private readonly FakeUploadReceiptStore _uploadReceiptStore;
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
            _uploadReceiptStore = new FakeUploadReceiptStore();
            _localTreeReader = new FakeDeviceToCloudLocalTreeReader();
            _remoteFolderContentSource = new FakeDeviceToCloudRemoteFolderContentSource();
            _fileOperator = new FakeDeviceToCloudFileOperator();
            CottonUploadOnlySyncPlanExecutor executor = new(
                _fileOperator,
                new FakeDeviceToCloudLocalFileOperator(),
                _uploadReceiptStore,
                new FixedTimeProvider(SyncedAt));
            _coordinator = new CottonDeviceToCloudSyncCoordinator(
                _rootStore,
                _pauseStore,
                _uploadReceiptStore,
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
        public async Task Run_uploads_new_local_file_and_records_uploaded_receipt()
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
        public async Task Run_does_not_upload_same_local_source_again_when_remote_file_disappears()
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
        public async Task Run_root_uploads_only_the_requested_root()
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
        public async Task Run_root_skips_bidirectional_root_without_reads()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                SyncRootId,
                FolderId,
                "Projects",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.Bidirectional);

            CottonDeviceToCloudSyncRunSummary summary = await _coordinator.RunRootAsync(InstanceUri, root);

            CottonDeviceToCloudSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.Equal(CottonDeviceToCloudSyncRootRunStatus.SkippedUnsupportedDirection, result.Status);
            Assert.Equal(CottonDeviceToCloudSyncStatusText.UnsupportedDirectionStatus, result.StatusText);
            Assert.Empty(_localTreeReader.ReadRootIds);
            Assert.Empty(_remoteFolderContentSource.RequestedFolderIds);
            Assert.Empty(_fileOperator.UploadedItems);
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
        public async Task Run_root_rejects_root_from_another_instance()
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
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid syncRootId,
            Guid folderId,
            string folderName,
            CottonSyncRootPermissionStatus permissionStatus = CottonSyncRootPermissionStatus.Available,
            CottonSyncDirection direction = CottonSyncDirection.DeviceToCloud,
            CottonSyncRootStorageKind storageKind = CottonSyncRootStorageKind.UserSelectedDocumentTree)
        {
            string localRootId = storageKind switch
            {
                CottonSyncRootStorageKind.AppPrivateDirectory => $"app-private-sync-root-{folderId:N}",
                CottonSyncRootStorageKind.UserSelectedDocumentTree =>
                    $"content://com.android.externalstorage.documents/tree/primary%3A{folderName}",
                _ => throw new ArgumentOutOfRangeException(nameof(storageKind)),
            };
            string localRootName = storageKind switch
            {
                CottonSyncRootStorageKind.AppPrivateDirectory => "On this device",
                CottonSyncRootStorageKind.UserSelectedDocumentTree => "Device folder",
                _ => throw new ArgumentOutOfRangeException(nameof(storageKind)),
            };

            return new CottonSyncRootSnapshot(
                syncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(folderId, folderName, $"Files / {folderName}"),
                new CottonSyncLocalRootSnapshot(storageKind, localRootId, localRootName, permissionStatus),
                direction,
                CottonUploadOriginalRetention.KeepOriginals);
        }

        private static CottonDeviceToCloudLocalContentSnapshot CreateLocalContent(
            params CottonDeviceToCloudLocalItemSnapshot[] items)
        {
            return new CottonDeviceToCloudLocalContentSnapshot("Device folder", items, problems: []);
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
                localSourceId,
                TestContentHashes.First);
        }

        private static CottonFolderContent CreateContent(
            CottonSyncRootSnapshot root,
            params CottonFileBrowserEntry[] entries)
        {
            return new CottonFolderContent(root.CloudFolder.FolderId, root.CloudFolder.FolderName, entries);
        }

        private static CottonFileBrowserEntry CreateFile(
            Guid id,
            string name,
            string? eTag,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return CottonFileBrowserEntry.CreateFile(
                id,
                name,
                UpdatedAt,
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag: eTag,
                metadata: metadata,
                contentHash: TestContentHashes.First);
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

        private class FakeUploadReceiptStore : ICottonUploadReceiptStore
        {
            private readonly Dictionary<Guid, Dictionary<string, CottonUploadReceiptSnapshot>> _receiptsByRootId = [];

            public List<CottonUploadReceiptSnapshot> SavedReceipts { get; } = [];

            public Task<IReadOnlyList<CottonUploadReceiptSnapshot>> LoadAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!_receiptsByRootId.TryGetValue(
                    root.Id,
                    out Dictionary<string, CottonUploadReceiptSnapshot>? receipts))
                {
                    return Task.FromResult<IReadOnlyList<CottonUploadReceiptSnapshot>>([]);
                }

                return Task.FromResult<IReadOnlyList<CottonUploadReceiptSnapshot>>(receipts.Values.ToArray());
            }

            public Task SaveAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonUploadReceiptSnapshot receipt,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!_receiptsByRootId.TryGetValue(
                    root.Id,
                    out Dictionary<string, CottonUploadReceiptSnapshot>? receipts))
                {
                    receipts = new Dictionary<string, CottonUploadReceiptSnapshot>(StringComparer.Ordinal);
                    _receiptsByRootId.Add(root.Id, receipts);
                }

                receipts[receipt.LocalSourceId] = receipt;
                SavedReceipts.Add(receipt);
                return Task.CompletedTask;
            }

            public Task ClearAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _receiptsByRootId.Remove(root.Id);
                return Task.CompletedTask;
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
            private readonly Dictionary<string, (Guid FileId, string ETag)> _uploadResults =
                new(StringComparer.Ordinal);

            public List<CottonDeviceToCloudSyncPlanItem> UploadedItems { get; } = [];

            public void SetUploadResult(string relativePath, Guid fileId, string eTag)
            {
                _uploadResults[relativePath] = (fileId, eTag);
            }

            public Task<CottonFileBrowserEntry> UploadNewFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                Guid operationId = item.UploadOperationId
                    ?? throw new InvalidOperationException("Upload operation id was not assigned.");
                (Guid fileId, string eTag) = _uploadResults[item.RelativePath];
                var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [CottonFileUploadMetadataKeys.UploadOperationId] = operationId.ToString("N"),
                };
                UploadedItems.Add(item);
                return Task.FromResult(CreateFile(fileId, item.DisplayName, eTag, metadata));
            }

            public Task<CottonFileBrowserEntry> UploadChangedFileAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CottonFolderHandle parentFolder,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("Changed uploads are not used by upload-only sync.");
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
                throw new NotSupportedException("Remote deletes are not supported by upload-only sync.");
            }
        }

        private class FakeDeviceToCloudLocalFileOperator : ICottonDeviceToCloudLocalFileOperator
        {
            public Task<CottonDeviceToCloudLocalFileDeleteStatus> DeleteIfUnchangedAsync(
                Uri instanceUri,
                CottonSyncRootSnapshot root,
                CottonDeviceToCloudSyncPlanItem item,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CottonDeviceToCloudLocalFileDeleteStatus.Unsupported);
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
