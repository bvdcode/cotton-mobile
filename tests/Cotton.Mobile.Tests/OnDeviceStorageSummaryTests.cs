using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class OnDeviceStorageSummaryTests
    {
        [Fact]
        public void Summary_creates_distinct_on_device_buckets()
        {
            CottonOnDeviceStorageSummary summary = CottonOnDeviceStorageSummary.Create(
                availableOfflineFileCount: 2,
                availableOfflineFileBytes: 2048,
                staleOfflineFileCount: 1,
                staleOfflineFileBytes: 512,
                missingOfflineFileCount: 1,
                cachedFolderListingCount: 3,
                cachedFolderListingBytes: 1536,
                thumbnailCount: 4,
                thumbnailBytes: 4096);

            Assert.False(summary.IsEmpty);
            Assert.Equal(8192, summary.TotalStoredSizeBytes);
            Assert.Equal(11, summary.TotalItemCount);
            Assert.Equal("8 KB stored on this device", summary.SummaryText);

            AssertBucket(
                summary.Buckets[0],
                CottonOnDeviceStorageBucketKind.OfflineAvailable,
                "Offline files",
                "Ready on this device.",
                "2 KB",
                "2 files",
                isAttentionVisible: false);
            AssertBucket(
                summary.Buckets[1],
                CottonOnDeviceStorageBucketKind.OfflineStale,
                "Needs refresh",
                "Kept offline but older than cloud.",
                "512 B",
                "1 file",
                isAttentionVisible: true);
            AssertBucket(
                summary.Buckets[2],
                CottonOnDeviceStorageBucketKind.OfflineMissing,
                "Missing offline files",
                "Kept offline but not stored here.",
                "0 B",
                "1 file",
                isAttentionVisible: true);
            AssertBucket(
                summary.Buckets[3],
                CottonOnDeviceStorageBucketKind.CachedFolderListings,
                "Folder lists",
                "Saved folder navigation.",
                "1.5 KB",
                "3 lists",
                isAttentionVisible: false);
            AssertBucket(
                summary.Buckets[4],
                CottonOnDeviceStorageBucketKind.Thumbnails,
                "Thumbnails",
                "Saved file previews.",
                "4 KB",
                "4 previews",
                isAttentionVisible: false);
        }

        [Fact]
        public void Empty_summary_keeps_honest_empty_copy_and_zero_buckets()
        {
            CottonOnDeviceStorageSummary summary = CottonOnDeviceStorageSummary.Empty;

            Assert.True(summary.IsEmpty);
            Assert.Equal(0, summary.TotalStoredSizeBytes);
            Assert.Equal(0, summary.TotalItemCount);
            Assert.Equal("No offline files or cached previews on this device.", summary.SummaryText);
            Assert.Equal(5, summary.Buckets.Count);
            Assert.All(summary.Buckets, bucket =>
            {
                Assert.Equal("0 B", bucket.SizeText);
                Assert.Equal(0, bucket.ItemCount);
                Assert.False(bucket.IsAttentionVisible);
            });
        }

        [Fact]
        public void Buckets_reject_negative_counts_and_sizes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonOnDeviceStorageBucketSnapshot.CreateAvailableOfflineFiles(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonOnDeviceStorageBucketSnapshot.CreateThumbnails(1, -1));
        }

        private static void AssertBucket(
            CottonOnDeviceStorageBucketSnapshot bucket,
            CottonOnDeviceStorageBucketKind expectedKind,
            string expectedTitle,
            string expectedDetails,
            string expectedSize,
            string expectedCount,
            bool isAttentionVisible)
        {
            Assert.Equal(expectedKind, bucket.Kind);
            Assert.Equal(expectedTitle, bucket.Title);
            Assert.Equal(expectedDetails, bucket.DetailText);
            Assert.Equal(expectedSize, bucket.SizeText);
            Assert.Equal(expectedCount, bucket.CountText);
            Assert.Equal(isAttentionVisible, bucket.IsAttentionVisible);
        }
    }
}
