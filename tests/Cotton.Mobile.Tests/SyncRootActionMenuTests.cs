using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootActionMenuTests
    {
        [Fact]
        public void FailedRunningRootOffersDetailsRunPauseAndStop()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonAutomaticSyncRootStatusSnapshot failure = CottonAutomaticSyncRootStatusSnapshot.Failed(
                root.Id,
                new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc),
                CottonAutomaticSyncFailureKind.NetworkUnavailable);
            CottonSyncRootListItem item = new(root, automaticStatus: failure);

            IReadOnlyList<string> actions = CottonSyncRootActionMenu.CreateActions(item);

            Assert.Equal(["Failure details", "Run now", "Pause"], actions);
            Assert.Equal("Stop syncing", CottonSyncRootActionMenu.CreateDestructionAction(item));
            Assert.Equal(
                CottonSyncRootAction.ShowFailureDetails,
                CottonSyncRootActionMenu.Resolve(item, "Failure details"));
        }
    }
}
