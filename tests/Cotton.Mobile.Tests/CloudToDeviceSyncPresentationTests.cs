using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudToDeviceSyncPresentationTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid RootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        [Fact]
        public void Account_scope_uses_normalized_username_only()
        {
            bool created = CottonAccountScopeKey.TryCreateFromUsername(" mobile-demo ", out string accountScopeKey);

            Assert.True(created);
            Assert.Equal("user:mobile-demo", accountScopeKey);
        }

        [Fact]
        public void Account_scope_rejects_missing_username_without_fallback()
        {
            bool created = CottonAccountScopeKey.TryCreateFromUsername(" ", out string accountScopeKey);

            Assert.False(created);
            Assert.Equal(string.Empty, accountScopeKey);
        }

        [Fact]
        public void Sync_status_reports_no_roots()
        {
            var summary = new CottonCloudToDeviceSyncRunSummary([]);

            Assert.Equal("No folders are set to sync.", CottonCloudToDeviceSyncStatusText.CreateCompletedStatus(summary));
        }

        [Fact]
        public void Sync_status_reports_up_to_date_run()
        {
            CottonCloudToDeviceSyncRunSummary summary = CreateSummary(
                new CottonCloudToDeviceSyncExecutionResult(
                    downloadedCount: 0,
                    refreshedCount: 0,
                    renamedCount: 0,
                    removedCount: 0,
                    skippedCount: 3,
                    blockedCount: 0));

            Assert.Equal(
                "Sync complete. Everything is up to date.",
                CottonCloudToDeviceSyncStatusText.CreateCompletedStatus(summary));
        }

        [Fact]
        public void Sync_status_reports_changed_blocked_and_skipped_counts()
        {
            CottonSyncRootSnapshot skippedRoot = CreateRoot(
                CottonSyncRootPermissionStatus.Unavailable,
                CottonSyncDirection.CloudToDevice);
            CottonCloudToDeviceSyncRunSummary summary = CreateSummary(
                new CottonCloudToDeviceSyncExecutionResult(
                    downloadedCount: 1,
                    refreshedCount: 2,
                    renamedCount: 3,
                    removedCount: 4,
                    skippedCount: 0,
                    blockedCount: 5),
                CottonCloudToDeviceSyncRootRunResult.SkippedNotReady(skippedRoot));

            Assert.Equal(
                "Sync complete. 1 downloaded, 2 refreshed, 3 renamed, 4 removed, 5 blocked, 1 root skipped.",
                CottonCloudToDeviceSyncStatusText.CreateCompletedStatus(summary));
        }

        [Fact]
        public void Sync_status_copy_is_stable()
        {
            Assert.Equal("Sync to this device", CottonCloudToDeviceSyncStatusText.ActionLabel);
            Assert.Equal("Syncing Projects...", CottonCloudToDeviceSyncStatusText.CreateStartingStatus(" Projects "));
            Assert.Equal("Sync needs a fresh account session.", CottonCloudToDeviceSyncStatusText.AccountUnavailableStatus);
            Assert.Equal("Offline. Sync needs internet.", CottonCloudToDeviceSyncStatusText.OfflineUnavailableStatus);
            Assert.Equal("Sync cancelled.", CottonCloudToDeviceSyncStatusText.CancelledStatus);
            Assert.Equal("Sync failed.", CottonCloudToDeviceSyncStatusText.FailedStatus);
        }

        private static CottonCloudToDeviceSyncRunSummary CreateSummary(
            CottonCloudToDeviceSyncExecutionResult executionResult,
            params CottonCloudToDeviceSyncRootRunResult[] extraResults)
        {
            CottonSyncRootSnapshot root = CreateRoot(
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.CloudToDevice);
            var plan = new CottonCloudToDeviceSyncPlanSnapshot(
                root.Id,
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                []);
            var results = new List<CottonCloudToDeviceSyncRootRunResult>
            {
                CottonCloudToDeviceSyncRootRunResult.Completed(root, plan, executionResult),
            };
            results.AddRange(extraResults);

            return new CottonCloudToDeviceSyncRunSummary(results);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            CottonSyncRootPermissionStatus permissionStatus,
            CottonSyncDirection direction)
        {
            return new CottonSyncRootSnapshot(
                RootId,
                InstanceUri,
                "user:mobile-demo",
                new CottonUploadDestinationSnapshot(
                    FolderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    "app-private-cloud-to-device",
                    "On this device",
                    permissionStatus),
                direction);
        }
    }
}
