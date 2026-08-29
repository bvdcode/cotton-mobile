using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ContentRevisionCheckpointTests
    {
        private const string SourceVersion = "source-v1";

        [Fact]
        public async Task ProgressCheckpointRetainsUnvisitedPersistedRevisions()
        {
            RecordingContentRevisionStore store = new();
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateMediaStoreRoot();
            CottonContentRevisionSnapshot first = CreateRevision("first", 1);
            CottonContentRevisionSnapshot second = CreateRevision("second", 1);
            CottonContentRevisionIndexSnapshot persisted = new(SourceVersion, [first, second]);
            CottonContentRevisionCheckpoint checkpoint = new(
                store,
                SyncTestRootFactory.InstanceUri,
                root,
                SourceVersion,
                persisted,
                itemInterval: 1);
            CottonContentRevisionSnapshot changedFirst = CreateRevision("first", 2);

            await checkpoint.SaveProgressIfDueAsync([changedFirst], TestContext.Current.CancellationToken);

            CottonContentRevisionIndexSnapshot saved = Assert.Single(store.SavedIndexes);
            Assert.Equal(2, saved.Revisions.Count);
            Assert.Contains(saved.Revisions, revision =>
                revision.LocalSourceId == changedFirst.LocalSourceId && revision.Generation == 2);
            Assert.Contains(saved.Revisions, revision => revision.LocalSourceId == second.LocalSourceId);
        }

        [Fact]
        public async Task FinalCheckpointDropsDeletedPersistedRevisions()
        {
            RecordingContentRevisionStore store = new();
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateMediaStoreRoot();
            CottonContentRevisionSnapshot retained = CreateRevision("retained", 1);
            CottonContentRevisionSnapshot deleted = CreateRevision("deleted", 1);
            CottonContentRevisionIndexSnapshot persisted = new(SourceVersion, [retained, deleted]);
            CottonContentRevisionCheckpoint checkpoint = new(
                store,
                SyncTestRootFactory.InstanceUri,
                root,
                SourceVersion,
                persisted,
                itemInterval: 1);

            await checkpoint.SaveFinalAsync([retained], TestContext.Current.CancellationToken);

            CottonContentRevisionIndexSnapshot saved = Assert.Single(store.SavedIndexes);
            Assert.Equal([retained.LocalSourceId], saved.Revisions.Select(revision => revision.LocalSourceId));
        }

        [Fact]
        public async Task UnchangedProgressDoesNotRewritePersistedIndex()
        {
            RecordingContentRevisionStore store = new();
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateMediaStoreRoot();
            CottonContentRevisionSnapshot revision = CreateRevision("same", 1);
            CottonContentRevisionIndexSnapshot persisted = new(SourceVersion, [revision]);
            CottonContentRevisionCheckpoint checkpoint = new(
                store,
                SyncTestRootFactory.InstanceUri,
                root,
                SourceVersion,
                persisted,
                itemInterval: 1);

            await checkpoint.SaveProgressIfDueAsync([revision], TestContext.Current.CancellationToken);
            await checkpoint.SaveFinalAsync([revision], TestContext.Current.CancellationToken);

            Assert.Empty(store.SavedIndexes);
        }

        private static CottonContentRevisionSnapshot CreateRevision(string sourceId, long generation)
        {
            return new CottonContentRevisionSnapshot(
                sourceId,
                generation,
                TestContentHashes.First,
                sizeBytes: 42);
        }
    }
}
