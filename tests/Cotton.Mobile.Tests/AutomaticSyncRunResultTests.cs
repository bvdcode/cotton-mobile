using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AutomaticSyncRunResultTests
    {
        [Fact]
        public void LaterSuccessClearsPreviousFailure()
        {
            Guid rootId = Guid.NewGuid();
            CottonAutomaticSyncRunResult failed = new([], [rootId]);
            CottonAutomaticSyncRunResult succeeded = new([rootId], []);

            CottonAutomaticSyncRunResult result = failed.Merge(succeeded);

            Assert.Equal([rootId], result.SucceededRootIds);
            Assert.Empty(result.FailedRootIds);
        }

        [Fact]
        public void UnrepeatedFailureRemainsAfterMerge()
        {
            Guid failedRootId = Guid.NewGuid();
            Guid succeededRootId = Guid.NewGuid();
            CottonAutomaticSyncRunResult first = new([], [failedRootId]);
            CottonAutomaticSyncRunResult second = new([succeededRootId], []);

            CottonAutomaticSyncRunResult result = first.Merge(second);

            Assert.Equal([succeededRootId], result.SucceededRootIds);
            Assert.Equal([failedRootId], result.FailedRootIds);
        }
    }
}
