using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class DeviceUnlockContractsTests
    {
        [Fact]
        public void Device_unlock_display_exposes_verify_action_when_available()
        {
            CottonDeviceUnlockDisplayState display = CottonDeviceUnlockDisplayState.Create(
                CottonDeviceUnlockAvailabilitySnapshot.Available);

            Assert.Equal("Device unlock", display.Title);
            Assert.Equal("Verify unlock", display.ActionText);
            Assert.Equal("Available", display.StatusText);
            Assert.Equal("Use the device screen lock to verify access.", display.DetailText);
            Assert.True(display.CanVerify);
            Assert.True(display.IsActionVisible);
        }

        [Fact]
        public void Device_unlock_display_hides_verify_action_when_unavailable()
        {
            CottonDeviceUnlockDisplayState display = CottonDeviceUnlockDisplayState.Create(
                CottonDeviceUnlockAvailabilitySnapshot.Unavailable("Set a screen lock first."));

            Assert.Equal("Unavailable", display.StatusText);
            Assert.Equal("Set a screen lock first.", display.DetailText);
            Assert.False(display.CanVerify);
            Assert.False(display.IsActionVisible);
        }

        [Fact]
        public void Device_unlock_display_uses_latest_result_after_prompt()
        {
            CottonDeviceUnlockDisplayState display = CottonDeviceUnlockDisplayState.Create(
                CottonDeviceUnlockAvailabilitySnapshot.Available,
                CottonDeviceUnlockResult.Succeeded);

            Assert.Equal("Verified", display.StatusText);
            Assert.Equal("Device unlock was confirmed.", display.DetailText);
            Assert.True(display.CanVerify);
            Assert.True(display.LastResult?.IsSucceeded);
        }

        [Fact]
        public void Device_unlock_contracts_reject_empty_details()
        {
            Assert.Throws<ArgumentException>(() => CottonDeviceUnlockAvailabilitySnapshot.Unavailable(" "));
            Assert.Throws<ArgumentException>(() => CottonDeviceUnlockResult.Unavailable(" "));
            Assert.Throws<ArgumentException>(() => CottonDeviceUnlockResult.Failed(" "));
        }
    }
}
