using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AppSwitcherPrivacyPolicyTests
    {
        [Fact]
        public void App_switcher_policy_hides_previews_only_when_app_lock_is_enabled_and_available()
        {
            var policy = new CottonAppSwitcherPrivacyPolicy();

            Assert.True(policy.ShouldHidePreviews(
                new CottonAppLockSettings(isEnabled: true),
                CottonAppLockCapabilitySnapshot.Available));
        }

        [Fact]
        public void App_switcher_policy_allows_previews_when_app_lock_is_disabled()
        {
            var policy = new CottonAppSwitcherPrivacyPolicy();

            Assert.False(policy.ShouldHidePreviews(
                CottonAppLockSettings.Disabled,
                CottonAppLockCapabilitySnapshot.Available));
        }

        [Fact]
        public void App_switcher_policy_allows_previews_when_device_unlock_is_unavailable()
        {
            var policy = new CottonAppSwitcherPrivacyPolicy();

            Assert.False(policy.ShouldHidePreviews(
                new CottonAppLockSettings(isEnabled: true),
                CottonAppLockCapabilitySnapshot.Unavailable("Device unlock is unavailable.")));
        }
    }
}
