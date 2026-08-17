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
            Assert.Equal("Projects", item.DisplayPathText);
            Assert.Same(item, item.PrimaryAction.Item);
            Assert.Equal(CottonSyncRootAction.UsePrimaryAction, item.PrimaryAction.Action);
        }

        [Fact]
        public void ReadyMediaStoreRootCanRunAndKeepsOriginals()
        {
            CottonSyncRootListItem item = new(
                SyncTestRootFactory.CreateMediaStoreRoot(),
                isPaused: false);

            Assert.True(item.CanRunNow);
            Assert.False(item.CanReconnect);
            Assert.Equal("Projects", item.DisplayPathText);
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

        [Fact]
        public void ListUsesDividersOnlyBetweenRoots()
        {
            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create(
            [
                SyncTestRootFactory.CreateDocumentTreeRoot(rootKey: "content://tree/first"),
                SyncTestRootFactory.CreateDocumentTreeRoot(rootKey: "content://tree/second"),
            ]);

            Assert.True(state.Items[0].IsDividerVisible);
            Assert.False(state.Items[1].IsDividerVisible);
        }

        [Fact]
        public void ReadyRootShowsItsLastAutomaticSyncResult()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            DateTime completedAtUtc = new(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc);
            CottonAutomaticSyncRootStatusSnapshot status = CottonAutomaticSyncRootStatusSnapshot.Succeeded(
                root.Id,
                completedAtUtc);
            CottonSyncRootListItem item = new(root, automaticStatus: status);

            Assert.Contains("Last synced", item.StatusText, StringComparison.Ordinal);
        }

        [Fact]
        public void FailedRootOffersReadableFailureDetails()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonAutomaticSyncRootStatusSnapshot status = CottonAutomaticSyncRootStatusSnapshot.Failed(
                root.Id,
                new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc),
                CottonAutomaticSyncFailureKind.NetworkUnavailable);
            CottonSyncRootListItem item = new(root, automaticStatus: status);

            Assert.True(item.CanShowFailureDetails);
            Assert.True(item.IsAttentionVisible);
            Assert.Contains("connection", item.FailureDetails, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RootReportsItsCurrentSyncStageAndProgress()
        {
            CottonSyncRootSnapshot root = SyncTestRootFactory.CreateDocumentTreeRoot();
            CottonSyncRootListItem item = new(root);

            item.ApplyProgress(CottonSyncProgressSnapshot.ScanningDevice(root.Id));
            Assert.True(item.IsRunning);
            Assert.False(item.IsProgressDeterminate);
            Assert.Equal("Scanning device…", item.StatusText);

            item.ApplyProgress(CottonSyncProgressSnapshot.ApplyingChanges(root.Id, 2, 4));
            Assert.True(item.IsProgressDeterminate);
            Assert.Equal(0.5, item.ProgressValue);
            Assert.Equal("Syncing 2 of 4 changes…", item.StatusText);

            item.ApplyProgress(CottonSyncProgressSnapshot.UploadingFile(
                root.Id,
                completedItemCount: 0,
                totalItemCount: 1,
                new CottonSyncTransferSnapshot("photo.jpg", 512, 1024, 256)));
            Assert.True(item.IsProgressDeterminate);
            Assert.Equal(0.5, item.ProgressValue);
            Assert.Contains("photo.jpg", item.StatusText, StringComparison.Ordinal);
            Assert.Contains("/s", item.StatusText, StringComparison.Ordinal);

            item.CompleteProgress();
            Assert.False(item.IsRunning);
            Assert.Equal("Ready", item.StatusText);
        }
    }
}
