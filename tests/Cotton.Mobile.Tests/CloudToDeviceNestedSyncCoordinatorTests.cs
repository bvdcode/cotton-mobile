using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.CloudToDeviceSyncCoordinatorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudToDeviceNestedSyncCoordinatorTests : CloudToDeviceSyncCoordinatorTestContext
    {
        [Fact]
        public async Task RunDownloadsNestedFolderFiles()
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
            Assert.Equal(["alpha.txt", "Nested/nested.txt"], _fileOperator.DownloadedRelativePaths);

            IReadOnlyList<CottonSyncedFileSnapshot> manifest = await _manifestStore.LoadAsync(InstanceUri, root);
            Assert.Contains(manifest, item => item.FileId == FirstFileId && item.RelativePath == "alpha.txt");
            Assert.Contains(manifest, item => item.FileId == nestedFile.Id && item.RelativePath == "Nested/nested.txt");
        }
    }
}
