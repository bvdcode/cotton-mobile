using Cotton.Auth;
using Cotton.Mobile.Services;
using Cotton.Sdk.Auth;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CottonSessionValidatorTests
    {
        [Fact]
        public async Task ValidAccessTokenIsValidatedWithoutRefresh()
        {
            using SessionValidationTestContext context = await CreateContextAsync(accessTokenIsExpired: false);

            UserDto user = await context.Validator.ValidateAsync(
                new Uri("https://cotton.test"),
                TestContext.Current.CancellationToken);
            TokenPairDto tokens = Assert.IsType<TokenPairDto>(
                await context.TokenStore.GetAsync(TestContext.Current.CancellationToken));

            Assert.Equal("session-user", user.Username);
            Assert.Equal(["/api/v1/auth/me"], context.Handler.RequestPaths);
            Assert.Equal("current-access", tokens.AccessToken);
            Assert.Equal("current-refresh", tokens.RefreshToken);
        }

        [Fact]
        public async Task ExpiredAccessTokenIsRefreshedByAuthorizedRequest()
        {
            using SessionValidationTestContext context = await CreateContextAsync(accessTokenIsExpired: true);

            UserDto user = await context.Validator.ValidateAsync(
                new Uri("https://cotton.test"),
                TestContext.Current.CancellationToken);
            TokenPairDto tokens = Assert.IsType<TokenPairDto>(
                await context.TokenStore.GetAsync(TestContext.Current.CancellationToken));

            Assert.Equal("session-user", user.Username);
            Assert.Equal(
                [
                    "/api/v1/auth/me",
                    "/api/v1/auth/refresh?refreshToken=current-refresh",
                    "/api/v1/auth/me",
                ],
                context.Handler.RequestPaths);
            Assert.Equal("rotated-access", tokens.AccessToken);
            Assert.Equal("rotated-refresh", tokens.RefreshToken);
        }

        private static async Task<SessionValidationTestContext> CreateContextAsync(
            bool accessTokenIsExpired)
        {
            SessionValidationHttpMessageHandler handler = new(accessTokenIsExpired);
            HttpClient httpClient = new(handler);
            InMemoryCottonTokenStore tokenStore = new();
            await tokenStore.SaveAsync(
                new TokenPairDto
                {
                    AccessToken = "current-access",
                    RefreshToken = "current-refresh",
                },
                TestContext.Current.CancellationToken);
            CottonSessionValidator validator = new(
                new SessionValidationTestClientFactory(httpClient, tokenStore));
            return new SessionValidationTestContext(handler, httpClient, tokenStore, validator);
        }
    }
}
