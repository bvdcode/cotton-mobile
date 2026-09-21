// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Diagnostics;
using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.DeviceToCloudSyncCoordinatorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public partial class DeviceToCloudSyncCoordinatorTests
    {
        [Fact]
        public async Task FiveThousandMatchingFilesNeedNoCloudQueriesOnTheNextPass()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Photos");
            await _rootStore.SaveAsync(InstanceUri, [root], TestContext.Current.CancellationToken);
            CottonDeviceToCloudLocalItemSnapshot[] local = [.. Enumerable.Range(0, 5000)
                .Select(index => CreateLocalFile($"photo-{index}.jpg", $"photo-{index}.jpg", $"document:{index}"))];
            _localTreeReader.SetContent(root.Id, CreateLocalContent(local));
            _remoteFolderContentSource.SetContent(FolderId, CreateContent(root,
                [.. local.Select(file => CreateFile(Guid.NewGuid(), file.DisplayName, "etag"))]));

            Stopwatch timer = Stopwatch.StartNew();
            CottonDeviceToCloudSyncRunSummary first = await _coordinator.RunAsync(InstanceUri, TestContext.Current.CancellationToken);
            double firstMilliseconds = timer.Elapsed.TotalMilliseconds;
            _remoteFolderContentSource.RequestedFolderIds.Clear();
            timer.Restart();
            CottonDeviceToCloudSyncRunSummary second = await _coordinator.RunAsync(InstanceUri, TestContext.Current.CancellationToken);
            double cachedMilliseconds = timer.Elapsed.TotalMilliseconds;

            Assert.Equal(5000, first.SkippedItemCount);
            Assert.Equal(5000, second.SkippedItemCount);
            Assert.Empty(_remoteFolderContentSource.RequestedFolderIds);
            Assert.Empty(_uploadReceiptStore.SavedReceipts);
            Assert.Empty(_fileOperator.UploadedItems);
            Assert.Equal(5000, (await _reviewStore.LoadAsync(root, TestContext.Current.CancellationToken)).VerifiedFiles.Count);
            TestContext.Current.TestOutputHelper!.WriteLine(
                $"5000 files: initial comparison {firstMilliseconds:F2} ms; cached comparison {cachedMilliseconds:F2} ms; cloud queries 0; receipt writes 0.");
        }

        [Fact]
        public async Task NewPhotoQueriesItsCloudBranchWithoutTraversingAnUnchangedBranch()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Photos");
            Guid camera = Guid.NewGuid();
            Guid archive = Guid.NewGuid();
            CottonDeviceToCloudLocalItemSnapshot first = CreateLocalFile("first.jpg", "Camera/first.jpg", "first");
            CottonDeviceToCloudLocalItemSnapshot old = CreateLocalFile("old.jpg", "Archive/old.jpg", "old");
            CottonDeviceToCloudLocalItemSnapshot cameraFolder = CottonDeviceToCloudLocalItemSnapshot.CreateFolder("Camera", "Camera", UpdatedAt, "camera");
            CottonDeviceToCloudLocalItemSnapshot archiveFolder = CottonDeviceToCloudLocalItemSnapshot.CreateFolder("Archive", "Archive", UpdatedAt, "archive");
            await _rootStore.SaveAsync(InstanceUri, [root], TestContext.Current.CancellationToken);
            _localTreeReader.SetContent(root.Id, CreateLocalContent(cameraFolder, archiveFolder, first, old));
            _remoteFolderContentSource.SetContent(FolderId, CreateContent(root, CreateFolder(camera, "Camera"), CreateFolder(archive, "Archive")));
            _remoteFolderContentSource.SetContent(camera, new CottonFolderContent(camera, "Camera", [CreateFile(FirstFileId, "first.jpg", "etag")]));
            _remoteFolderContentSource.SetContent(archive, new CottonFolderContent(archive, "Archive", [CreateFile(SecondFileId, "old.jpg", "etag")]));
            await _coordinator.RunAsync(InstanceUri, TestContext.Current.CancellationToken);
            _remoteFolderContentSource.RequestedFolderIds.Clear();
            CottonDeviceToCloudLocalItemSnapshot added = CreateLocalFile("new.jpg", "Camera/new.jpg", "new");
            _localTreeReader.SetContent(root.Id, CreateLocalContent(cameraFolder, archiveFolder, first, old, added));
            _fileOperator.SetUploadResult("Camera/new.jpg", Guid.NewGuid(), "new-etag");

            CottonDeviceToCloudSyncRunSummary result = await _coordinator.RunAsync(InstanceUri, TestContext.Current.CancellationToken);

            Assert.Equal(1, result.UploadedCount);
            Assert.Equal(3, result.SkippedItemCount);
            Assert.Equal([FolderId, camera], _remoteFolderContentSource.RequestedFolderIds);
            Assert.Equal("Camera/new.jpg", Assert.Single(_fileOperator.UploadedItems).RelativePath);
        }

        [Fact]
        public async Task PendingUploadStillChecksItsPreviousCloudPathAfterThePhoneFileWasRenamed()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Photos");
            Guid archive = Guid.NewGuid();
            CottonDeviceToCloudLocalItemSnapshot existing = CreateLocalFile("existing.jpg", "existing.jpg", "existing");
            await _rootStore.SaveAsync(InstanceUri, [root], TestContext.Current.CancellationToken);
            _localTreeReader.SetContent(root.Id, CreateLocalContent(existing));
            _remoteFolderContentSource.SetContent(FolderId, CreateContent(root,
                CreateFile(FirstFileId, "existing.jpg", "etag"), CreateFolder(archive, "Archive")));
            _remoteFolderContentSource.SetContent(archive, new CottonFolderContent(archive, "Archive", []));
            await _coordinator.RunAsync(InstanceUri, TestContext.Current.CancellationToken);
            Guid operation = Guid.NewGuid();
            await _uploadReceiptStore.SaveAsync(InstanceUri, root, new CottonUploadReceiptSnapshot("pending",
                "Archive/photo.jpg", UpdatedAt, 42, "text/plain", operation, CottonUploadReceiptStatus.Pending,
                SyncedAt, null, null, TestContentHashes.First), TestContext.Current.CancellationToken);
            _localTreeReader.SetContent(root.Id, CreateLocalContent(existing,
                CreateLocalFile("renamed.jpg", "renamed.jpg", "pending")));
            _remoteFolderContentSource.SetContent(archive, new CottonFolderContent(archive, "Archive",
                [CreateFile(SecondFileId, "photo.jpg", "etag-uploaded", new Dictionary<string, string>
                {
                    [CottonFileUploadMetadataKeys.UploadOperationId] = operation.ToString("N"),
                })]));
            _remoteFolderContentSource.RequestedFolderIds.Clear();

            CottonDeviceToCloudSyncRunSummary result = await _coordinator.RunAsync(InstanceUri, TestContext.Current.CancellationToken);

            Assert.Equal(1, result.ConfirmedUploadCount);
            Assert.Contains(archive, _remoteFolderContentSource.RequestedFolderIds);
            Assert.Empty(_fileOperator.UploadedItems);
        }

        [Fact]
        public async Task DeleteAfterUploadAlwaysChecksCloudBeforeDeletingLocalFiles()
        {
            CottonSyncRootSnapshot original = CreateRoot(SyncRootId, FolderId, "Photos");
            CottonSyncRootSnapshot root = new(original.Id, original.InstanceUri, original.AccountScopeKey,
                original.CloudFolder, original.LocalRoot, original.Direction,
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload);
            await _rootStore.SaveAsync(InstanceUri, [root], TestContext.Current.CancellationToken);
            _localTreeReader.SetContent(root.Id, CreateLocalContent(CreateLocalFile("photo.jpg", "photo.jpg", "photo")));
            _remoteFolderContentSource.SetContent(FolderId, CreateContent(root, CreateFile(FirstFileId, "photo.jpg", "etag")));
            await _coordinator.RunAsync(InstanceUri, TestContext.Current.CancellationToken);
            _remoteFolderContentSource.RequestedFolderIds.Clear();

            await _coordinator.RunAsync(InstanceUri, TestContext.Current.CancellationToken);

            Assert.Equal([FolderId], _remoteFolderContentSource.RequestedFolderIds);
            Assert.Null((await _reviewStore.LoadAsync(root, TestContext.Current.CancellationToken)).BaselineFingerprint);
        }

        [Fact]
        public async Task ContentChangeInvalidatesTheSavedComparison()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Photos");
            CottonDeviceToCloudLocalItemSnapshot file = CreateLocalFile("photo.jpg", "photo.jpg", "photo");
            await _rootStore.SaveAsync(InstanceUri, [root], TestContext.Current.CancellationToken);
            _localTreeReader.SetContent(root.Id, CreateLocalContent(file));
            _remoteFolderContentSource.SetContent(FolderId, CreateContent(root, CreateFile(FirstFileId, "photo.jpg", "etag")));
            await _coordinator.RunAsync(InstanceUri, TestContext.Current.CancellationToken);
            _remoteFolderContentSource.RequestedFolderIds.Clear();
            _localTreeReader.SetContent(root.Id, CreateLocalContent(CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                file.DisplayName, file.RelativePath, file.LocalUpdatedAtUtc, file.SizeBytes,
                file.ContentType, file.LocalSourceId!, TestContentHashes.Second)));

            CottonDeviceToCloudSyncRunSummary result = await _coordinator.RunAsync(InstanceUri, TestContext.Current.CancellationToken);
            Assert.True(result.HasBlockedItems);
            Assert.Equal([FolderId], _remoteFolderContentSource.RequestedFolderIds);
        }
    }
}
