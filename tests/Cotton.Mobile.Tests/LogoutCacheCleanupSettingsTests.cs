using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class LogoutCacheCleanupSettingsTests
    {
        [Fact]
        public void Logout_cache_cleanup_defaults_to_enabled()
        {
            CottonLogoutCacheCleanupDisplayState display = CottonLogoutCacheCleanupDisplayState.Create(
                CottonLogoutCacheCleanupSettings.Default);

            Assert.True(display.IsEnabled);
            Assert.Equal("Clear cache on logout", display.Title);
            Assert.Equal("On", display.StatusText);
            Assert.Equal(
                "Remove cached previews, saved folder lists, and downloaded files when you log out.",
                display.DetailText);
        }

        [Fact]
        public void Logout_cache_cleanup_can_be_disabled()
        {
            CottonLogoutCacheCleanupDisplayState display = CottonLogoutCacheCleanupDisplayState.Create(
                CottonLogoutCacheCleanupSettings.Default.WithClearCachedFilesOnLogout(false));

            Assert.False(display.IsEnabled);
            Assert.Equal("Off", display.StatusText);
            Assert.Equal("Keep local cached files on this device when you log out.", display.DetailText);
        }
    }
}
