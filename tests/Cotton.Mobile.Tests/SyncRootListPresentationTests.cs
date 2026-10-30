using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootListPresentationTests
    {
        [Fact]
        public void ReadyDocumentTreeRootCanRunAndDeleteOriginals()
        {
            CottonSyncRootListItem item = new(
                SyncTestRootFactory.CreateDocumentTreeRoot(
                    retention: CottonUploadOriginalRetention.DeleteAfterConfirmedUpload),
                isPaused: false);

            Assert.True(item.CanRunNow);
            Assert.False(item.CanReconnect);
            Assert.Contains("Upload", item.DetailText, StringComparison.Ordinal);
        }

        [Fact]
        public void ReadyMediaStoreRootCanRunAndKeepsOriginals()
        {
            CottonSyncRootListItem item = new(
                SyncTestRootFactory.CreateMediaStoreRoot(),
                isPaused: false);

            Assert.True(item.CanRunNow);
            Assert.False(item.CanReconnect);
            Assert.Contains("Photos and videos", item.DetailText, StringComparison.Ordinal);
        }

        [Fact]
        public void RevokedMediaStoreRootCanReconnect()
        {
            CottonSyncRootListItem item = new(
                SyncTestRootFactory.CreateMediaStoreRoot(CottonSyncRootPermissionStatus.Revoked),
                isPaused: false);

            Assert.False(item.CanRunNow);
            Assert.True(item.CanReconnect);
        }

        [Fact]
        public void PausedRootCannotRun()
        {
            CottonSyncRootListItem item = new(
                SyncTestRootFactory.CreateDocumentTreeRoot(),
                isPaused: true);

            Assert.False(item.CanRunNow);
            Assert.True(item.IsPaused);
        }
    }
}
