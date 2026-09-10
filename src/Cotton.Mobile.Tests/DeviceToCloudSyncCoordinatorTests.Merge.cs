using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.DeviceToCloudSyncCoordinatorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public partial class DeviceToCloudSyncCoordinatorTests
    {
        [Fact]
        public async Task RunConfirmsMatchingPendingUploadByPathAndDoesNotUploadAgain()
        {
            CottonSyncRootSnapshot root = CreateRoot(SyncRootId, FolderId, "Projects");
            CottonDeviceToCloudLocalItemSnapshot localFile =
                CreateLocalFile("alpha.txt", "alpha.txt", "document:alpha");
            CottonUploadReceiptSnapshot pendingReceipt = CottonUploadReceiptSnapshot.CreatePending(
                CottonDeviceToCloudSyncPlanItemFactory.CreateLocal(
                    CottonDeviceToCloudSyncActionKind.UploadNewFile,
                    localFile),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                SyncedAt.AddMinutes(-1));
            CottonFileBrowserEntry remoteFile = CreateFile(
                FirstFileId,
                "alpha.txt",
                "\"etag-1\"");

            await _rootStore.SaveAsync(InstanceUri, [root], TestContext.Current.CancellationToken);
            await _uploadReceiptStore.SaveAsync(
                InstanceUri,
                root,
                pendingReceipt,
                TestContext.Current.CancellationToken);
            _localTreeReader.SetContent(root.Id, CreateLocalContent(localFile));
            _remoteFolderContentSource.SetContent(
                root.CloudFolder.FolderId,
                CreateContent(root, remoteFile));

            CottonDeviceToCloudSyncRunSummary firstRun = await _coordinator.RunAsync(
                InstanceUri,
                TestContext.Current.CancellationToken);
            CottonDeviceToCloudSyncRunSummary secondRun = await _coordinator.RunAsync(
                InstanceUri,
                TestContext.Current.CancellationToken);

            Assert.Equal(1, firstRun.ConfirmedUploadCount);
            Assert.Equal(0, firstRun.UploadedCount);
            Assert.False(firstRun.HasBlockedItems);
            Assert.Equal(0, secondRun.ConfirmedUploadCount);
            Assert.Equal(0, secondRun.UploadedCount);
            Assert.Equal(1, secondRun.SkippedItemCount);
            Assert.False(secondRun.HasBlockedItems);
            Assert.Empty(_fileOperator.UploadedItems);
            CottonUploadReceiptSnapshot uploadedReceipt = Assert.Single(
                await _uploadReceiptStore.LoadAsync(
                    InstanceUri,
                    root,
                    TestContext.Current.CancellationToken));
            Assert.True(uploadedReceipt.IsUploaded);
            Assert.Equal(FirstFileId, uploadedReceipt.RemoteFileId);
            Assert.Equal("\"etag-1\"", uploadedReceipt.RemoteETag);
        }
    }
}
