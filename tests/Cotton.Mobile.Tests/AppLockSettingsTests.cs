using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AppLockSettingsTests
    {
        [Fact]
        public void App_lock_display_disables_toggle_when_unlock_is_unavailable()
        {
            CottonAppLockSettingsDisplayState display = CottonAppLockSettingsDisplayState.Create(
                new CottonAppLockSettings(isEnabled: true),
                CottonAppLockCapabilitySnapshot.Unavailable("This version cannot require device unlock."));

            Assert.Equal("App lock", display.Title);
            Assert.False(display.IsEnabled);
            Assert.False(display.CanToggle);
            Assert.Equal("Unavailable", display.StatusText);
            Assert.Equal("This version cannot require device unlock.", display.DetailText);
        }

        [Fact]
        public void App_lock_display_allows_toggle_when_unlock_is_available()
        {
            CottonAppLockSettingsDisplayState display = CottonAppLockSettingsDisplayState.Create(
                CottonAppLockSettings.Disabled,
                CottonAppLockCapabilitySnapshot.Available);

            Assert.False(display.IsEnabled);
            Assert.True(display.CanToggle);
            Assert.Equal("Off", display.StatusText);
            Assert.Equal("Require device unlock after 30 seconds in the background.", display.DetailText);

            CottonAppLockSettingsDisplayState enabled = CottonAppLockSettingsDisplayState.Create(
                display.Settings.WithEnabled(true),
                CottonAppLockCapabilitySnapshot.Available);

            Assert.True(enabled.IsEnabled);
            Assert.Equal("On", enabled.StatusText);
        }

        [Fact]
        public void App_lock_capability_rejects_empty_unavailable_detail()
        {
            Assert.Throws<ArgumentException>(() => CottonAppLockCapabilitySnapshot.Unavailable(" "));
        }
    }
}
