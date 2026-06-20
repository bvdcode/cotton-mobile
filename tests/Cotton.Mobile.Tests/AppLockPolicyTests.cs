using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AppLockPolicyTests
    {
        [Fact]
        public void App_lock_policy_does_not_lock_when_setting_is_disabled()
        {
            var policy = new CottonAppLockPolicy(TimeSpan.FromSeconds(30));
            DateTimeOffset backgroundedAt = DateTimeOffset.Parse("2026-06-20T06:00:00Z");

            bool shouldLock = policy.ShouldLock(
                CottonAppLockSettings.Disabled,
                CottonAppLockCapabilitySnapshot.Available,
                new CottonAppLockRuntimeState(backgroundedAt, null),
                backgroundedAt.AddMinutes(5));

            Assert.False(shouldLock);
        }

        [Fact]
        public void App_lock_policy_waits_for_background_timeout()
        {
            var policy = new CottonAppLockPolicy(TimeSpan.FromSeconds(30));
            DateTimeOffset backgroundedAt = DateTimeOffset.Parse("2026-06-20T06:00:00Z");

            bool shouldLock = policy.ShouldLock(
                new CottonAppLockSettings(isEnabled: true),
                CottonAppLockCapabilitySnapshot.Available,
                new CottonAppLockRuntimeState(backgroundedAt, null),
                backgroundedAt.AddSeconds(29));

            Assert.False(shouldLock);
        }

        [Fact]
        public void App_lock_policy_locks_after_background_timeout()
        {
            var policy = new CottonAppLockPolicy(TimeSpan.FromSeconds(30));
            DateTimeOffset backgroundedAt = DateTimeOffset.Parse("2026-06-20T06:00:00Z");

            bool shouldLock = policy.ShouldLock(
                new CottonAppLockSettings(isEnabled: true),
                CottonAppLockCapabilitySnapshot.Available,
                new CottonAppLockRuntimeState(backgroundedAt, null),
                backgroundedAt.AddSeconds(30));

            Assert.True(shouldLock);
        }

        [Fact]
        public void App_lock_policy_uses_successful_unlock_after_background()
        {
            var policy = new CottonAppLockPolicy(TimeSpan.FromSeconds(30));
            DateTimeOffset backgroundedAt = DateTimeOffset.Parse("2026-06-20T06:00:00Z");

            bool shouldLock = policy.ShouldLock(
                new CottonAppLockSettings(isEnabled: true),
                CottonAppLockCapabilitySnapshot.Available,
                new CottonAppLockRuntimeState(backgroundedAt, backgroundedAt.AddSeconds(35)),
                backgroundedAt.AddMinutes(5));

            Assert.False(shouldLock);
        }

        [Fact]
        public void App_lock_policy_rejects_negative_timeout()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CottonAppLockPolicy(TimeSpan.FromSeconds(-1)));
        }
    }
}
