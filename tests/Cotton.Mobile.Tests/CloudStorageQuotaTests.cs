using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudStorageQuotaTests
    {
        [Fact]
        public void Cloud_quota_formats_known_account_limit()
        {
            CottonCloudStorageQuotaSnapshot quota = CottonCloudStorageQuotaSnapshot.Create(
                usedBytes: 1024,
                limitBytes: 4096);

            Assert.Equal("Account storage", quota.Title);
            Assert.Equal(CottonCloudStorageQuotaStatus.WithinLimit, quota.Status);
            Assert.Equal(1024, quota.UsedBytes);
            Assert.Equal(4096, quota.LimitBytes);
            Assert.Equal("1 KB of 4 KB used", quota.SummaryText);
            Assert.Equal("3 KB available", quota.DetailText);
            Assert.Equal(0.25d, quota.UsageFraction, precision: 3);
            Assert.True(quota.IsProgressVisible);
            Assert.False(quota.IsAttentionVisible);
        }

        [Fact]
        public void Cloud_quota_marks_near_and_over_limit_attention()
        {
            CottonCloudStorageQuotaSnapshot near = CottonCloudStorageQuotaSnapshot.Create(
                usedBytes: 3687,
                limitBytes: 4096);
            CottonCloudStorageQuotaSnapshot over = CottonCloudStorageQuotaSnapshot.Create(
                usedBytes: 5120,
                limitBytes: 4096);

            Assert.Equal(CottonCloudStorageQuotaStatus.NearLimit, near.Status);
            Assert.True(near.IsAttentionVisible);
            Assert.Equal(CottonCloudStorageQuotaStatus.OverLimit, over.Status);
            Assert.Equal("5 KB of 4 KB used", over.SummaryText);
            Assert.Equal("1 KB over account quota", over.DetailText);
            Assert.Equal(1d, over.UsageFraction, precision: 3);
            Assert.True(over.IsAttentionVisible);
        }

        [Fact]
        public void Cloud_quota_keeps_unlimited_and_unavailable_states_explicit()
        {
            CottonCloudStorageQuotaSnapshot unlimited = CottonCloudStorageQuotaSnapshot.Create(
                usedBytes: 2048,
                limitBytes: null);

            Assert.Equal(CottonCloudStorageQuotaStatus.Unlimited, unlimited.Status);
            Assert.Equal("2 KB used", unlimited.SummaryText);
            Assert.Equal("No account quota reported.", unlimited.DetailText);
            Assert.False(unlimited.IsProgressVisible);
            Assert.Equal(CottonCloudStorageQuotaStatus.Unavailable, CottonCloudStorageQuotaSnapshot.Unavailable.Status);
            Assert.Equal("Account storage unavailable.", CottonCloudStorageQuotaSnapshot.Unavailable.SummaryText);
            Assert.Equal("This server does not report account quota.", CottonCloudStorageQuotaSnapshot.Unavailable.DetailText);
            Assert.False(CottonCloudStorageQuotaSnapshot.Unavailable.IsProgressVisible);
        }

        [Fact]
        public void Cloud_quota_rejects_negative_values()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonCloudStorageQuotaSnapshot.Create(-1, 1024));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonCloudStorageQuotaSnapshot.Create(0, -1));
        }
    }
}
