using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.SyncRootListPresentationTestData;

namespace Cotton.Mobile.Tests
{
    public class SyncRootListManagementPresentationTests
    {
        [Theory]
        [InlineData(CottonSyncDirection.DeviceToCloud, "Camera", "Files / Camera", "Upload new files · On this device")]
        [InlineData(CottonSyncDirection.Bidirectional, "Projects", "Files / Projects", "Bidirectional · On this device")]
        public void AppPrivateUploadSourceIsExplicitlyUnsupported(
            CottonSyncDirection direction,
            string folderName,
            string path,
            string expectedDetails)
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                folderName,
                path,
                CottonSyncRootPermissionStatus.Available,
                direction);

            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create([root]);
            CottonSyncRootListItem item = Assert.Single(state.Items);

            Assert.False(state.CanRunAny);
            Assert.Equal(expectedDetails, item.DetailText);
            Assert.Equal("Unsupported", item.StatusText);
            Assert.True(item.IsUnsupportedLocalRoot);
            Assert.False(item.IsReady);
            Assert.True(item.IsAttentionVisible);
            Assert.False(item.CanRunNow);
        }

        [Fact]
        public void StopSyncManagementCopyIsExplicitAboutLocalFiles()
        {
            Assert.Equal("Stop syncing Projects?", CottonSyncRootManagementText.CreateStopTitle(" Projects "));
            Assert.Equal(
                "This stops future sync for this folder. Files already on this device are not deleted.",
                CottonSyncRootManagementText.StopMessage);
            Assert.Equal("Stopped syncing Projects.", CottonSyncRootManagementText.CreateStoppedStatus("Projects"));
            Assert.Equal("Sync folder is no longer configured.", CottonSyncRootManagementText.RootMissingStatus);
            Assert.Equal("Sync paused. Resume this folder first.", CottonSyncRootManagementText.RootPausedStatus);
            Assert.Equal("Could not pause syncing this folder.", CottonSyncRootManagementText.PauseFailedStatus);
            Assert.Equal("Could not resume syncing this folder.", CottonSyncRootManagementText.ResumeFailedStatus);
            Assert.Equal("Could not stop syncing this folder.", CottonSyncRootManagementText.StopFailedStatus);
            Assert.Equal("Paused syncing Projects.", CottonSyncRootManagementText.CreatePausedStatus("Projects"));
            Assert.Equal("Resumed syncing Projects.", CottonSyncRootManagementText.CreateResumedStatus("Projects"));
        }

        [Fact]
        public void StopSyncManagementCopyHandlesBlankFolderName()
        {
            Assert.Equal("Stop syncing this folder?", CottonSyncRootManagementText.CreateStopTitle(" "));
            Assert.Equal("Stopped syncing this folder.", CottonSyncRootManagementText.CreateStoppedStatus(string.Empty));
        }
    }
}
