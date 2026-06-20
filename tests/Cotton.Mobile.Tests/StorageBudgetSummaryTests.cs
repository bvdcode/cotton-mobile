using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class StorageBudgetSummaryTests
    {
        [Fact]
        public void Budget_summary_distinguishes_evictable_buckets_and_protected_offline_files()
        {
            CottonStorageBudgetSummary summary = CottonStorageBudgetSummary.Create(
                evictableDownloadFileCount: 2,
                evictableDownloadBytes: 512,
                evictableDownloadBudgetBytes: 1024,
                thumbnailCount: 3,
                thumbnailBytes: 1800,
                thumbnailBudgetBytes: 2000,
                folderListingCount: 4,
                folderListingBytes: 2500,
                folderListingBudgetBytes: 2000,
                protectedOfflineFileCount: 5,
                protectedOfflineBytes: 4096);

            Assert.Equal(4812, summary.TotalEvictableBytes);
            Assert.Equal(5024, summary.TotalBudgetBytes);
            Assert.Equal("4.7 KB of 4.9 KB evictable budget used", summary.SummaryText);
            Assert.Equal("5 files kept offline, 4 KB protected", summary.ProtectedOfflineText);
            Assert.True(summary.IsAttentionVisible);

            AssertBucket(
                summary.Buckets[0],
                CottonStorageBudgetBucketKind.EvictableDownloads,
                "Evictable downloads",
                "Opened files not kept offline.",
                "512 B of 1 KB",
                0.5d,
                "2 files",
                CottonStorageBudgetStatus.WithinBudget,
                isAttentionVisible: false);
            AssertBucket(
                summary.Buckets[1],
                CottonStorageBudgetBucketKind.Thumbnails,
                "Thumbnail cache",
                "Regenerated while browsing.",
                "1.8 KB of 2 KB",
                0.9d,
                "3 previews",
                CottonStorageBudgetStatus.NearBudget,
                isAttentionVisible: true);
            AssertBucket(
                summary.Buckets[2],
                CottonStorageBudgetBucketKind.FolderListings,
                "Folder list cache",
                "Saved navigation for offline browsing.",
                "2.4 KB of 2 KB",
                1d,
                "4 lists",
                CottonStorageBudgetStatus.OverBudget,
                isAttentionVisible: true);
        }

        [Fact]
        public void Empty_budget_summary_keeps_zero_cache_copy()
        {
            CottonStorageBudgetSummary summary = CottonStorageBudgetSummary.Empty;

            Assert.Equal("Evictable cache is empty.", summary.SummaryText);
            Assert.Equal(
                "No kept-offline files are protected from automatic cleanup.",
                summary.ProtectedOfflineText);
            Assert.False(summary.IsAttentionVisible);
            Assert.All(summary.Buckets, bucket =>
            {
                Assert.Equal(CottonStorageBudgetStatus.Empty, bucket.Status);
                Assert.Equal("Empty", bucket.StatusText);
                Assert.Equal(0d, bucket.UsageFraction);
                Assert.False(bucket.IsAttentionVisible);
            });
        }

        [Fact]
        public void Budget_buckets_reject_invalid_sizes_and_budgets()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonStorageBudgetBucketSnapshot.CreateEvictableDownloads(-1, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonStorageBudgetBucketSnapshot.CreateThumbnails(1, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonStorageBudgetBucketSnapshot.CreateFolderListings(1, 1, 0));
        }

        [Fact]
        public void Cleanup_policy_copy_keeps_evictable_cache_distinct_from_kept_offline()
        {
            Assert.Equal("Clear downloads and kept-offline files", CottonStorageCleanupPolicyText.ClearDownloadedFilesTitle);
            Assert.Equal("Clear temporary upload files", CottonStorageCleanupPolicyText.ClearTemporaryUploadsTitle);
            Assert.Equal("Free device space", CottonStorageCleanupPolicyText.FreeDeviceSpaceTitle);
            Assert.Equal(
                "Opened downloads and files marked On device, including kept-offline files, will need internet to open again.",
                CottonStorageCleanupPolicyText.ClearDownloadedFilesMessage);
            Assert.Equal(
                "Only completed, cancelled, or abandoned upload files for this account will be removed. Waiting, running, and failed uploads stay in Transfers.",
                CottonStorageCleanupPolicyText.ClearTemporaryUploadsMessage);
            Assert.Equal(
                "Opened downloads not kept offline, cached previews, and saved folder lists will be removed. Kept-offline files and waiting, running, or failed uploads stay on this device.",
                CottonStorageCleanupPolicyText.FreeDeviceSpaceMessage);
            Assert.Equal(
                "Only cached previews will be removed. Offline files stay on this device.",
                CottonStorageCleanupPolicyText.ClearThumbnailsMessage);
            Assert.Equal(
                "Saved folder lists will be removed. Offline files stay on this device.",
                CottonStorageCleanupPolicyText.ClearFolderListingsMessage);
            Assert.Equal(
                "Cached previews, saved folder lists, opened downloads, and kept-offline files will be removed from this device.",
                CottonStorageCleanupPolicyText.ClearAllMessage);
        }

        [Fact]
        public void Temporary_upload_cleanup_status_reports_empty_and_deleted_results()
        {
            Assert.Equal(
                "No temporary upload files to clear.",
                CottonStorageCleanupPolicyText.CreateTemporaryUploadsClearedStatus(
                    CottonTransferStagedFileCleanupResult.Empty));
            Assert.Equal(
                "1 temporary upload file cleared (512 B).",
                CottonStorageCleanupPolicyText.CreateTemporaryUploadsClearedStatus(
                    new CottonTransferStagedFileCleanupResult(1, 512)));
            Assert.Equal(
                "2 temporary upload files cleared (2 KB).",
                CottonStorageCleanupPolicyText.CreateTemporaryUploadsClearedStatus(
                    new CottonTransferStagedFileCleanupResult(2, 2048)));
        }

        [Fact]
        public void Free_device_space_status_reports_empty_and_deleted_results()
        {
            Assert.Equal(
                "No evictable Cotton files to clear.",
                CottonStorageCleanupPolicyText.CreateDeviceSpaceFreedStatus(CottonDeviceSpaceCleanupResult.Empty));
            Assert.Equal(
                "Freed 512 B from 1 Cotton file.",
                CottonStorageCleanupPolicyText.CreateDeviceSpaceFreedStatus(
                    new CottonDeviceSpaceCleanupResult(1, 512)));
            Assert.Equal(
                "Freed 2 KB from 2 Cotton files.",
                CottonStorageCleanupPolicyText.CreateDeviceSpaceFreedStatus(
                    new CottonDeviceSpaceCleanupResult(2, 2048)));
        }

        [Fact]
        public void Temporary_upload_cleanup_result_rejects_negative_values()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CottonTransferStagedFileCleanupResult(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CottonTransferStagedFileCleanupResult(0, -1));
        }

        [Fact]
        public void Free_device_space_result_rejects_negative_values_and_combines_results()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CottonDeviceSpaceCleanupResult(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CottonDeviceSpaceCleanupResult(0, -1));

            CottonDeviceSpaceCleanupResult result = new CottonDeviceSpaceCleanupResult(1, 512)
                .Add(new CottonDeviceSpaceCleanupResult(2, 2048));

            Assert.Equal(3, result.FileCount);
            Assert.Equal(2560, result.SizeBytes);
            Assert.True(result.HasDeletedFiles);
        }

        private static void AssertBucket(
            CottonStorageBudgetBucketSnapshot bucket,
            CottonStorageBudgetBucketKind expectedKind,
            string expectedTitle,
            string expectedDetails,
            string expectedUsage,
            double expectedUsageFraction,
            string expectedCount,
            CottonStorageBudgetStatus expectedStatus,
            bool isAttentionVisible)
        {
            Assert.Equal(expectedKind, bucket.Kind);
            Assert.Equal(expectedTitle, bucket.Title);
            Assert.Equal(expectedDetails, bucket.DetailText);
            Assert.Equal(expectedUsage, bucket.UsageText);
            Assert.Equal(expectedUsageFraction, bucket.UsageFraction, precision: 3);
            Assert.Equal(expectedCount, bucket.CountText);
            Assert.Equal(expectedStatus, bucket.Status);
            Assert.Equal(isAttentionVisible, bucket.IsAttentionVisible);
        }
    }
}
