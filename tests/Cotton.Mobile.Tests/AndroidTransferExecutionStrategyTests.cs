using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AndroidTransferExecutionStrategyTests
    {
        [Theory]
        [InlineData(CottonAndroidTransferWorkKind.ManualUpload)]
        [InlineData(CottonAndroidTransferWorkKind.ManualDownload)]
        [InlineData(CottonAndroidTransferWorkKind.ShareInboxUpload)]
        public void User_started_long_transfers_use_uidt_on_android_14_plus(
            CottonAndroidTransferWorkKind workKind)
        {
            CottonAndroidTransferExecutionStrategy strategy =
                CottonAndroidTransferExecutionStrategyResolver.Resolve(workKind, androidApiLevel: 34);

            Assert.Equal(workKind, strategy.WorkKind);
            Assert.Equal(CottonAndroidTransferExecutionHost.UserInitiatedDataTransfer, strategy.Host);
            Assert.True(strategy.RequiresUserInitiation);
            Assert.True(strategy.RequiresUserVisibleNotification);
            Assert.True(strategy.SupportsRetry);
            Assert.True(strategy.SupportsNetworkConstraint);
            Assert.False(strategy.SupportsChargingConstraint);
            Assert.False(strategy.SupportsUnmeteredNetworkConstraint);
            Assert.True(strategy.IsBackgroundHost);
        }

        [Theory]
        [InlineData(CottonAndroidTransferWorkKind.ManualUpload)]
        [InlineData(CottonAndroidTransferWorkKind.ManualDownload)]
        [InlineData(CottonAndroidTransferWorkKind.ShareInboxUpload)]
        public void User_started_long_transfers_fall_back_to_foreground_manual_before_android_14(
            CottonAndroidTransferWorkKind workKind)
        {
            CottonAndroidTransferExecutionStrategy strategy =
                CottonAndroidTransferExecutionStrategyResolver.Resolve(workKind, androidApiLevel: 33);

            Assert.Equal(CottonAndroidTransferExecutionHost.ForegroundManual, strategy.Host);
            Assert.True(strategy.RequiresUserInitiation);
            Assert.True(strategy.RequiresUserVisibleNotification);
            Assert.True(strategy.SupportsRetry);
            Assert.False(strategy.SupportsNetworkConstraint);
            Assert.False(strategy.IsBackgroundHost);
        }

        [Fact]
        public void Camera_backup_uses_constrained_persistent_background_work()
        {
            CottonAndroidTransferExecutionStrategy strategy =
                CottonAndroidTransferExecutionStrategyResolver.Resolve(
                    CottonAndroidTransferWorkKind.CameraBackupUpload,
                    androidApiLevel: 36);

            Assert.Equal(CottonAndroidTransferExecutionHost.WorkManagerConstrained, strategy.Host);
            Assert.False(strategy.RequiresUserInitiation);
            Assert.True(strategy.RequiresUserVisibleNotification);
            Assert.True(strategy.SupportsRetry);
            Assert.True(strategy.SupportsNetworkConstraint);
            Assert.True(strategy.SupportsChargingConstraint);
            Assert.True(strategy.SupportsUnmeteredNetworkConstraint);
            Assert.True(strategy.IsBackgroundHost);
        }

        [Fact]
        public void Selected_media_imports_remain_foreground_until_queue_backed()
        {
            CottonAndroidTransferExecutionStrategy strategy =
                CottonAndroidTransferExecutionStrategyResolver.Resolve(
                    CottonAndroidTransferWorkKind.SelectedMediaUpload,
                    androidApiLevel: 36);

            Assert.Equal(CottonAndroidTransferExecutionHost.ForegroundManual, strategy.Host);
            Assert.True(strategy.RequiresUserInitiation);
            Assert.False(strategy.RequiresUserVisibleNotification);
            Assert.False(strategy.SupportsRetry);
            Assert.False(strategy.IsBackgroundHost);
        }

        [Fact]
        public void Strategy_rejects_invalid_android_api_level()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonAndroidTransferExecutionStrategyResolver.Resolve(
                    CottonAndroidTransferWorkKind.ManualUpload,
                    androidApiLevel: 0));
        }
    }
}
