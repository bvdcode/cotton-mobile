using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncProgressHubTests
    {
        [Fact]
        public void ReportRetainsLatestProgressUntilRootCompletes()
        {
            Guid rootId = Guid.NewGuid();
            CottonSyncProgressHub hub = new();

            hub.Report(CottonSyncProgressSnapshot.ScanningDevice(rootId));
            hub.Report(CottonSyncProgressSnapshot.ApplyingChanges(rootId, 2, 5));

            CottonSyncProgressSnapshot progress = Assert.Single(hub.GetCurrent());
            Assert.Equal(rootId, progress.RootId);
            Assert.Equal(CottonSyncProgressStage.ApplyingChanges, progress.Stage);
            Assert.Equal(2, progress.CompletedItemCount);
            Assert.Equal(5, progress.TotalItemCount);

            hub.Complete(rootId);

            Assert.Empty(hub.GetCurrent());
        }
    }
}
