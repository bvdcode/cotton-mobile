using System.Net;
using System.Text;
using System.Text.Json;
using Cotton.Auth;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RemotePushDeviceTokenServiceTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example");
        private static readonly Guid TokenId = Guid.Parse("22222222-3333-4444-5555-666666666666");
        private static readonly DateTime RegisteredAt =
            new(2026, 6, 19, 22, 15, 0, DateTimeKind.Utc);

        [Fact]
        public async Task RegisterCurrentAsync_puts_authorized_token_payload()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, CreateTokenResponse());
            var service = CreateService(handler, new FakeTokenStore("access", "refresh"));

            CottonRemotePushDeviceTokenSnapshot result = await service.RegisterCurrentAsync(
                InstanceUri,
                CottonRemotePushDeviceTokenRegistrationRequest.ForAndroidFirebase(
                    " fcm-token ",
                    " Pixel 8 ",
                    " 1.2.3 "));

            Assert.Equal(TokenId, result.Id);
            Assert.Equal(CottonRemotePushProviderKind.FirebaseCloudMessaging, result.Provider);
            Assert.Equal(CottonRemotePushMobilePlatform.Android, result.Platform);
            Assert.Equal("session-1", result.SessionId);
            Assert.Equal("Pixel 8", result.DeviceName);
            Assert.Equal("1.2.3", result.AppVersion);
            Assert.Equal(RegisteredAt, result.LastRegisteredAt);
            Assert.Null(result.RevokedAt);

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/api/v1/notifications/device-tokens/current", request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access", request.AuthorizationParameter);
            Assert.Equal("Cotton-Mobile/1.0", request.UserAgent);
            Assert.Equal("Test device", request.DeviceName);

            using JsonDocument document = JsonDocument.Parse(request.Content ?? string.Empty);
            JsonElement root = document.RootElement;
            Assert.Equal(0, root.GetProperty("provider").GetInt32());
            Assert.Equal(0, root.GetProperty("platform").GetInt32());
            Assert.Equal("fcm-token", root.GetProperty("token").GetString());
            Assert.Equal("Pixel 8", root.GetProperty("deviceName").GetString());
            Assert.Equal("1.2.3", root.GetProperty("appVersion").GetString());
        }

        [Fact]
        public async Task RegisterCurrentAsync_refreshes_token_once_after_unauthorized_response()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.Unauthorized, new { error = "expired" });
            handler.EnqueueJson(
                HttpStatusCode.OK,
                new TokenPairDto
                {
                    AccessToken = "new-access",
                    RefreshToken = "new-refresh",
                });
            handler.EnqueueJson(HttpStatusCode.OK, CreateTokenResponse());
            var tokenStore = new FakeTokenStore("old-access", "old refresh");
            var service = CreateService(handler, tokenStore);

            await service.RegisterCurrentAsync(
                InstanceUri,
                CottonRemotePushDeviceTokenRegistrationRequest.ForAndroidFirebase("fcm-token"));

            Assert.Equal("new-access", tokenStore.Tokens?.AccessToken);
            Assert.Equal("new-refresh", tokenStore.Tokens?.RefreshToken);
            Assert.Equal(3, handler.Requests.Count);
            Assert.Equal("old-access", handler.Requests[0].AuthorizationParameter);
            Assert.Null(handler.Requests[1].AuthorizationParameter);
            Assert.Equal("/api/v1/auth/refresh?refreshToken=old%20refresh", handler.Requests[1].Uri.PathAndQuery);
            Assert.Equal("new-access", handler.Requests[2].AuthorizationParameter);
            Assert.Equal("/api/v1/notifications/device-tokens/current", handler.Requests[2].Uri.PathAndQuery);
        }

        [Fact]
        public async Task RevokeCurrentSessionAsync_sends_authorized_delete()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, new { revokedTokens = 2 });
            var service = CreateService(handler, new FakeTokenStore("access", "refresh"));

            CottonRemotePushDeviceTokenRevocationResult result =
                await service.RevokeCurrentSessionAsync(InstanceUri);

            Assert.Equal(2, result.RevokedTokens);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/api/v1/notifications/device-tokens/current-session", request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access", request.AuthorizationParameter);
            Assert.Null(request.Content);
        }

        [Fact]
        public async Task RegisterCurrentAsync_requires_supported_https_instance_uri()
        {
            var service = CreateService(new RecordingHttpMessageHandler(), new FakeTokenStore("access", "refresh"));

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.RegisterCurrentAsync(
                    new Uri("http://cloud.example"),
                    CottonRemotePushDeviceTokenRegistrationRequest.ForAndroidFirebase("fcm-token")));
        }

        [Fact]
        public void RegistrationRequest_normalizes_and_validates_payload()
        {
            string longDeviceName = new('d', CottonRemotePushDeviceTokenRegistrationRequest.DeviceNameMaxLength + 1);
            string longAppVersion = new('v', CottonRemotePushDeviceTokenRegistrationRequest.AppVersionMaxLength + 1);

            var request = CottonRemotePushDeviceTokenRegistrationRequest.ForAndroidFirebase(
                " token ",
                longDeviceName,
                longAppVersion);

            Assert.Equal("token", request.Token);
            Assert.Equal(CottonRemotePushDeviceTokenRegistrationRequest.DeviceNameMaxLength, request.DeviceName!.Length);
            Assert.Equal(CottonRemotePushDeviceTokenRegistrationRequest.AppVersionMaxLength, request.AppVersion!.Length);
            Assert.Null(CottonRemotePushDeviceTokenRegistrationRequest.ForAndroidFirebase("token", " ", " ").DeviceName);
            Assert.Null(CottonRemotePushDeviceTokenRegistrationRequest.ForAndroidFirebase("token", " ", " ").AppVersion);
            Assert.Throws<ArgumentException>(
                () => CottonRemotePushDeviceTokenRegistrationRequest.ForAndroidFirebase(" "));
            Assert.Throws<ArgumentException>(
                () => CottonRemotePushDeviceTokenRegistrationRequest.ForAndroidFirebase(
                    new string('t', CottonRemotePushDeviceTokenRegistrationRequest.TokenMaxLength + 1)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonRemotePushDeviceTokenRegistrationRequest(
                    (CottonRemotePushProviderKind)42,
                    CottonRemotePushMobilePlatform.Android,
                    "token",
                    null,
                    null));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonRemotePushDeviceTokenRegistrationRequest(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    (CottonRemotePushMobilePlatform)42,
                    "token",
                    null,
                    null));
        }

        private static CottonRemotePushDeviceTokenService CreateService(
            RecordingHttpMessageHandler handler,
            ICottonTokenStore tokenStore)
        {
            var httpClient = new HttpClient(handler);
            var apiClient = new CottonAuthenticatedApiClient(
                httpClient,
                tokenStore,
                new CottonAuthenticatedApiHttpOptions("Cotton-Mobile/1.0", "Test device"));
            return new CottonRemotePushDeviceTokenService(apiClient);
        }

        private static object CreateTokenResponse()
        {
            return new
            {
                id = TokenId,
                provider = CottonRemotePushProviderKind.FirebaseCloudMessaging,
                platform = CottonRemotePushMobilePlatform.Android,
                sessionId = "session-1",
                deviceName = "Pixel 8",
                appVersion = "1.2.3",
                lastRegisteredAt = RegisteredAt,
                revokedAt = (DateTime?)null,
            };
        }

        private class FakeTokenStore : ICottonTokenStore
        {
            public FakeTokenStore(string accessToken, string refreshToken)
            {
                Tokens = new TokenPairDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                };
            }

            public TokenPairDto? Tokens { get; set; }

            public Task<TokenPairDto?> GetAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Tokens is null
                    ? null
                    : new TokenPairDto
                    {
                        AccessToken = Tokens.AccessToken,
                        RefreshToken = Tokens.RefreshToken,
                    });
            }

            public Task SaveAsync(TokenPairDto tokens, CancellationToken cancellationToken = default)
            {
                Tokens = new TokenPairDto
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                };
                return Task.CompletedTask;
            }

            public Task ClearAsync(CancellationToken cancellationToken = default)
            {
                Tokens = null;
                return Task.CompletedTask;
            }
        }

        private class RecordingHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses = [];

            public List<RecordedRequest> Requests { get; } = [];

            public void EnqueueJson(HttpStatusCode statusCode, object value)
            {
                string json = JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                _responses.Enqueue(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                if (_responses.Count == 0)
                {
                    throw new InvalidOperationException("No queued HTTP response.");
                }

                string? content = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Requests.Add(new RecordedRequest(
                    request.Method,
                    request.RequestUri ?? throw new InvalidOperationException("Request URI is missing."),
                    request.Headers.Authorization?.Scheme,
                    request.Headers.Authorization?.Parameter,
                    request.Headers.UserAgent.ToString(),
                    request.Headers.TryGetValues(CottonClientHeaders.DeviceName, out IEnumerable<string>? deviceNames)
                        ? deviceNames.SingleOrDefault()
                        : null,
                    content));

                return _responses.Dequeue();
            }
        }

        private class RecordedRequest
        {
            public RecordedRequest(
                HttpMethod method,
                Uri uri,
                string? authorizationScheme,
                string? authorizationParameter,
                string userAgent,
                string? deviceName,
                string? content)
            {
                Method = method;
                Uri = uri;
                AuthorizationScheme = authorizationScheme;
                AuthorizationParameter = authorizationParameter;
                UserAgent = userAgent;
                DeviceName = deviceName;
                Content = content;
            }

            public HttpMethod Method { get; }

            public Uri Uri { get; }

            public string? AuthorizationScheme { get; }

            public string? AuthorizationParameter { get; }

            public string UserAgent { get; }

            public string? DeviceName { get; }

            public string? Content { get; }
        }
    }
}
