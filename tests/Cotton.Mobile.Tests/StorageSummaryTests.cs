using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class StorageSummaryTests
    {
        [Fact]
        public void Storage_summary_includes_transfer_staging_in_total_usage()
        {
            var summary = new CottonStorageSummary(
                new CottonStorageCategorySnapshot("Thumbnails", 100, 1),
                new CottonStorageCategorySnapshot("Folder listings", 200, 2),
                new CottonStorageCategorySnapshot("Downloaded files", 300, 3),
                new CottonStorageCategorySnapshot("Pending uploads", 400, 4),
                CottonOnDeviceStorageSummary.Empty,
                CottonStorageBudgetSummary.Empty,
                CottonCloudStorageQuotaSnapshot.Unavailable);

            Assert.Equal(1000, summary.TotalSizeBytes);
            Assert.Equal(10, summary.TotalFileCount);
            Assert.Equal(400, summary.TransferStaging.SizeBytes);
            Assert.Equal(4, summary.TransferStaging.FileCount);
            Assert.Equal(CottonCloudStorageQuotaStatus.Unavailable, summary.CloudQuota.Status);
        }

        [Fact]
        public void Storage_summary_requires_transfer_staging_category()
        {
            Assert.Throws<ArgumentNullException>(
                () => new CottonStorageSummary(
                    new CottonStorageCategorySnapshot("Thumbnails", 0, 0),
                    new CottonStorageCategorySnapshot("Folder listings", 0, 0),
                    new CottonStorageCategorySnapshot("Downloaded files", 0, 0),
                    null!,
                    CottonOnDeviceStorageSummary.Empty,
                    CottonStorageBudgetSummary.Empty,
                    CottonCloudStorageQuotaSnapshot.Unavailable));
        }

        [Fact]
        public void Storage_summary_requires_cloud_quota_state()
        {
            Assert.Throws<ArgumentNullException>(
                () => new CottonStorageSummary(
                    new CottonStorageCategorySnapshot("Thumbnails", 0, 0),
                    new CottonStorageCategorySnapshot("Folder listings", 0, 0),
                    new CottonStorageCategorySnapshot("Downloaded files", 0, 0),
                    new CottonStorageCategorySnapshot("Pending uploads", 0, 0),
                    CottonOnDeviceStorageSummary.Empty,
                    CottonStorageBudgetSummary.Empty,
                    null!));
        }
    }
}
