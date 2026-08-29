using System.Net;
using Cotton.Auth;
using Cotton.Mobile.Services;
using Cotton.Sdk.Auth;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RefreshTokenTransportTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev/base/");

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RefreshCredentialNeverAppearsInRequestUri(bool refreshes)
        {
            const string refreshToken = "secret refresh token + query";
            RecordingHttpMessageHandler handler = new(
                HttpStatusCode.OK,
                "{\"accessToken\":\"access\",\"refreshToken\":\"rotated\"}");
            CottonRefreshTokenTransport transport = new(new HttpClient(handler));

            if (refreshes)
            {
                TokenPairDto result = await transport.RefreshAsync(InstanceUri, refreshToken, TestContext.Current.CancellationToken);
                Assert.Equal("rotated", result.RefreshToken);
            }
            else
            {
                await transport.LogoutAsync(InstanceUri, refreshToken, TestContext.Current.CancellationToken);
            }

            HttpRequestMessage request = Assert.IsType<HttpRequestMessage>(handler.Request);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.DoesNotContain(refreshToken, request.RequestUri?.AbsoluteUri, StringComparison.Ordinal);
            Assert.DoesNotContain("refreshToken", request.RequestUri?.Query, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(
                $"refresh_token={refreshToken}",
                Assert.Single(request.Headers.GetValues("Cookie")));
            Assert.Null(request.Content);
        }

        [Fact]
        public async Task RefreshPreservesInstanceBasePath()
        {
            RecordingHttpMessageHandler handler = new(
                HttpStatusCode.OK,
                "{\"accessToken\":\"access\",\"refreshToken\":\"rotated\"}");
            CottonRefreshTokenTransport transport = new(new HttpClient(handler));

            await transport.RefreshAsync(InstanceUri, "refresh", TestContext.Current.CancellationToken);

            Assert.Equal(
                "/base/api/v1/auth/refresh",
                handler.Request?.RequestUri?.AbsolutePath);
        }

        [Fact]
        public void SessionServiceDoesNotCallSdkRefreshOrLogoutMethods()
        {
            string source = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Services/CottonSessionService.cs");

            Assert.DoesNotContain("client.Auth.RefreshAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("client.Auth.LogoutAsync", source, StringComparison.Ordinal);
            Assert.Contains("_refreshTokenTransport", source, StringComparison.Ordinal);
            Assert.Contains(".RefreshAsync(instanceUri", source, StringComparison.Ordinal);
            Assert.Contains(".LogoutAsync(instanceUri", source, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SdkFacingStoreNeverReturnsTheRefreshCredential()
        {
            InMemoryCottonTokenStore innerStore = new();
            CottonAccessTokenStore accessTokenStore = new(innerStore);
            TokenPairDto tokens = new()
            {
                AccessToken = "access",
                RefreshToken = "refresh",
            };
            await accessTokenStore.SaveAsync(tokens, TestContext.Current.CancellationToken);

            TokenPairDto exposed = Assert.IsType<TokenPairDto>(await accessTokenStore.GetAsync(TestContext.Current.CancellationToken));
            TokenPairDto persisted = Assert.IsType<TokenPairDto>(await innerStore.GetAsync(TestContext.Current.CancellationToken));
            Assert.Equal("access", exposed.AccessToken);
            Assert.Empty(exposed.RefreshToken);
            Assert.Equal("refresh", persisted.RefreshToken);
        }
    }
}
