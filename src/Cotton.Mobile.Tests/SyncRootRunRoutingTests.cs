using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootRunRoutingTests
    {
        [Fact]
        public void StartingStatusUsesUploadCopy()
        {
            string status = CottonSyncRootRunRouting.CreateStartingStatus(
                SyncTestRootFactory.CreateDocumentTreeRoot());

            Assert.Equal("Uploading new files from Projects…", status);
        }

        [Fact]
        public void OfflineStatusUsesUploadCopy()
        {
            Assert.Equal(
                "Offline. Sync needs internet.",
                CottonSyncRootRunRouting.CreateOfflineUnavailableStatus(CottonSyncDirection.DeviceToCloud));
        }

        [Fact]
        public void UndefinedDirectionIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonSyncRootRunRouting.CreateFailedStatus((CottonSyncDirection)42));
        }
    }
}
