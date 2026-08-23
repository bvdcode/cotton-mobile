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
            CottonAutomaticSyncRunResult failed = new(
                [],
                [new CottonAutomaticSyncFailure(rootId, CottonAutomaticSyncFailureKind.NetworkUnavailable)]);
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
            CottonAutomaticSyncRunResult first = new(
                [],
                [new CottonAutomaticSyncFailure(failedRootId, CottonAutomaticSyncFailureKind.ActionRequired)]);
            CottonAutomaticSyncRunResult second = new([succeededRootId], []);

            CottonAutomaticSyncRunResult result = first.Merge(second);

            Assert.Equal([succeededRootId], result.SucceededRootIds);
            Assert.Equal([failedRootId], result.FailedRootIds);
            Assert.Empty(result.RetryableRootIds);
        }

        [Fact]
        public void LaterFailureReplacesItsPreviousFailureKind()
        {
            Guid rootId = Guid.NewGuid();
            CottonAutomaticSyncRunResult transient = new(
                [],
                [new CottonAutomaticSyncFailure(rootId, CottonAutomaticSyncFailureKind.NetworkUnavailable)]);
            CottonAutomaticSyncRunResult permanent = new(
                [],
                [new CottonAutomaticSyncFailure(rootId, CottonAutomaticSyncFailureKind.ActionRequired)]);

            CottonAutomaticSyncRunResult result = transient.Merge(permanent);

            CottonAutomaticSyncFailure failure = Assert.Single(result.Failures);
            Assert.Equal(CottonAutomaticSyncFailureKind.ActionRequired, failure.Kind);
            Assert.Empty(result.RetryableRootIds);
        }
    }
}
