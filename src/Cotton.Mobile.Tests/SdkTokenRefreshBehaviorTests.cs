using Cotton.Auth;
using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Cotton.Settings;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SdkTokenRefreshBehaviorTests
    {
        [Fact]
        public async Task AuthorizedRequestRefreshesAndPersistsRotatedTokens()
        {
            RotatingTokenHttpMessageHandler handler = new();
            using HttpClient httpClient = new(handler);
            InMemoryCottonTokenStore tokenStore = new();
            await tokenStore.SaveAsync(
                new TokenPairDto
                {
                    AccessToken = "expired-access",
                    RefreshToken = "current-refresh",
                },
                TestContext.Current.CancellationToken);
            await using CottonCloudClient client = new(
                httpClient,
                tokenStore,
                new CottonSdkOptions
                {
                    BaseAddress = new Uri("https://cotton.test"),
                });

            ClientSettingsDto settings = await client.Settings
                .GetAsync(TestContext.Current.CancellationToken);
            TokenPairDto storedTokens = Assert.IsType<TokenPairDto>(
                await tokenStore.GetAsync(TestContext.Current.CancellationToken));

            Assert.Equal(4_194_304, settings.MaxChunkSizeBytes);
            Assert.Equal("rotated-access", storedTokens.AccessToken);
            Assert.Equal("rotated-refresh", storedTokens.RefreshToken);
            Assert.Equal(
                [
                    "/api/v1/settings",
                    "/api/v1/auth/refresh?refreshToken=current-refresh",
                    "/api/v1/settings",
                ],
                handler.RequestPaths);
            Assert.Equal(
                ["expired-access", null, "rotated-access"],
                handler.AuthorizationTokens);
        }
    }
}
