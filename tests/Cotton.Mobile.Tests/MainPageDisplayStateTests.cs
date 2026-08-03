using Cotton.Mobile.ViewModels;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MainPageDisplayStateTests
    {
        [Fact]
        public void Default_instance_url_stays_effective_without_being_field_placeholder()
        {
            var display = new MainPageDisplayState(" https://app.cottoncloud.dev/ ");

            Assert.Equal(string.Empty, display.InstanceUrl);
            Assert.Equal("https://app.cottoncloud.dev/", display.DefaultInstanceUrl);
            Assert.Equal("Custom server URL", display.InstanceUrlPlaceholder);
            Assert.Equal("https://app.cottoncloud.dev/", display.EffectiveInstanceUrl);

            display.InstanceUrl = "https://files.example.test";

            Assert.Equal("https://files.example.test", display.EffectiveInstanceUrl);

            display.InstanceUrl = "   ";

            Assert.Equal("https://app.cottoncloud.dev/", display.EffectiveInstanceUrl);
        }

        [Fact]
        public void Sign_in_state_exposes_only_the_authentication_surface()
        {
            var display = new MainPageDisplayState("https://app.cottoncloud.dev");

            display.ShowSignIn("Sign in again.");

            Assert.True(display.IsSignInVisible);
            Assert.True(display.IsBrandHeaderVisible);
            Assert.True(display.IsLegalFooterVisible);
            Assert.True(display.IsInputEnabled);
            Assert.False(display.IsAuthenticatedVisible);
            Assert.Equal("Sign in again.", display.Status);
        }

        [Fact]
        public void Authorization_state_can_be_cancelled_once()
        {
            var display = new MainPageDisplayState("https://app.cottoncloud.dev");

            display.ShowAuthorizationProgress();

            Assert.True(display.IsAuthorizationProgressVisible);
            Assert.True(display.IsAuthorizationProgressIndicatorRunning);
            Assert.True(display.IsCancelAuthorizationEnabled);

            display.ShowAuthorizationCancelling();

            Assert.False(display.IsAuthorizationProgressIndicatorRunning);
            Assert.False(display.IsCancelAuthorizationEnabled);
            Assert.Equal("Cancelling authorization…", display.AuthorizationProgressMessage);
        }

        [Fact]
        public void Authenticated_state_starts_on_sync_and_exposes_profile_data()
        {
            var display = new MainPageDisplayState("https://app.cottoncloud.dev");
            var profile = new MainPageProfile(
                "Mobile Demo",
                "demo@example.com",
                "app.cottoncloud.dev",
                "user:mobile-demo");

            display.ShowAuthenticated(profile);

            Assert.True(display.IsAuthenticatedVisible);
            Assert.True(display.IsSyncDestinationVisible);
            Assert.False(display.IsProfileDestinationVisible);
            Assert.False(display.IsBrandHeaderVisible);
            Assert.True(display.IsLogoutEnabled);
            Assert.Equal("Mobile Demo", display.ProfileName);
            Assert.Equal("demo@example.com", display.ProfileEmail);
            Assert.Equal("app.cottoncloud.dev", display.ProfileInstance);
        }

        [Fact]
        public void Authenticated_navigation_switches_between_two_stable_destinations()
        {
            var display = new MainPageDisplayState("https://app.cottoncloud.dev");
            display.ShowAuthenticated(new MainPageProfile(
                "Mobile Demo",
                null,
                "app.cottoncloud.dev",
                "user:mobile-demo"));

            display.ShowDestination(AppNavigationDestination.Profile);

            Assert.False(display.IsSyncDestinationVisible);
            Assert.True(display.IsProfileDestinationVisible);

            display.ShowDestination(AppNavigationDestination.Sync);

            Assert.True(display.IsSyncDestinationVisible);
            Assert.False(display.IsProfileDestinationVisible);
        }
    }
}
