using Cotton.Mobile.ViewModels;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MainPageDisplayStateTests
    {
        [Fact]
        public void DefaultInstanceUrlStaysEffectiveWithoutBeingFieldPlaceholder()
        {
            MainPageDisplayState display = new(" https://app.cottoncloud.dev/ ");

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
        public void SignInStateExposesOnlyTheAuthenticationSurface()
        {
            MainPageDisplayState display = new("https://app.cottoncloud.dev");
            display.ShowAuthenticated(new MainPageProfile(
                "Mobile Demo",
                "demo@example.com",
                "app.cottoncloud.dev",
                "user:mobile-demo",
                new Uri("https://app.cottoncloud.dev/api/v1/preview/avatar.webp")));

            display.ShowSignIn("Sign in again.");

            Assert.True(display.IsSignInVisible);
            Assert.True(display.IsBrandHeaderVisible);
            Assert.True(display.IsLegalFooterVisible);
            Assert.True(display.IsInputEnabled);
            Assert.False(display.IsAuthenticatedVisible);
            Assert.Equal("Sign in again.", display.Status);
            Assert.Null(display.ProfileAvatarUrl);
        }

        [Fact]
        public void AuthorizationStateCanBeCancelledOnce()
        {
            MainPageDisplayState display = new("https://app.cottoncloud.dev");

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
        public void AuthenticatedStateStartsOnSyncAndExposesProfileData()
        {
            MainPageDisplayState display = new("https://app.cottoncloud.dev");
            Uri avatarUrl = new("https://app.cottoncloud.dev/api/v1/preview/avatar.webp");
            MainPageProfile profile = new(
                "Mobile Demo",
                "demo@example.com",
                "app.cottoncloud.dev",
                "user:mobile-demo",
                avatarUrl);

            display.ShowAuthenticated(profile);

            Assert.True(display.IsAuthenticatedVisible);
            Assert.True(display.IsSyncDestinationVisible);
            Assert.False(display.IsProfileDestinationVisible);
            Assert.False(display.IsBrandHeaderVisible);
            Assert.True(display.IsLogoutEnabled);
            Assert.Equal("Mobile Demo", display.ProfileName);
            Assert.Equal("demo@example.com", display.ProfileEmail);
            Assert.Equal("app.cottoncloud.dev", display.ProfileInstance);
            Assert.Equal(avatarUrl.AbsoluteUri, display.ProfileAvatarUrl);
        }

        [Fact]
        public void AuthenticatedNavigationSwitchesBetweenTwoStableDestinations()
        {
            MainPageDisplayState display = new("https://app.cottoncloud.dev");
            display.ShowAuthenticated(new MainPageProfile(
                "Mobile Demo",
                null,
                "app.cottoncloud.dev",
                "user:mobile-demo",
                avatarUrl: null));

            display.ShowDestination(AppNavigationDestination.Profile);

            Assert.False(display.IsSyncDestinationVisible);
            Assert.True(display.IsProfileDestinationVisible);

            display.ShowDestination(AppNavigationDestination.Sync);

            Assert.True(display.IsSyncDestinationVisible);
            Assert.False(display.IsProfileDestinationVisible);
        }
    }
}
