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
    public class CloudStorageQuotaServiceTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example");

        [Fact]
        public async Task GetCurrentAsync_sends_authorized_quota_request()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, new
            {
                usedBytes = 1024,
                quotaBytes = 4096,
                availableBytes = 3072,
            });
            var service = CreateService(handler, new FakeTokenStore("access", "refresh"));

            CottonCloudStorageQuotaSnapshot result = await service.GetCurrentAsync(InstanceUri);

            Assert.Equal(CottonCloudStorageQuotaStatus.WithinLimit, result.Status);
            Assert.Equal(1024, result.UsedBytes);
            Assert.Equal(4096, result.LimitBytes);
            Assert.Equal("1 KB of 4 KB used", result.SummaryText);
            Assert.Equal("3 KB available", result.DetailText);

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v1/users/me/storage-quota", request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access", request.AuthorizationParameter);
            Assert.Equal("Cotton-Mobile/1.0", request.UserAgent);
            Assert.Equal("Test device", request.DeviceName);
            Assert.Null(request.Content);
        }

        [Fact]
        public async Task GetCurrentAsync_maps_missing_quota_limit_to_unlimited_account()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, new
            {
                usedBytes = 2048,
                quotaBytes = (long?)null,
                availableBytes = (long?)null,
            });
            var service = CreateService(handler, new FakeTokenStore("access", "refresh"));

            CottonCloudStorageQuotaSnapshot result = await service.GetCurrentAsync(InstanceUri);

            Assert.Equal(CottonCloudStorageQuotaStatus.Unlimited, result.Status);
            Assert.Equal(2048, result.UsedBytes);
            Assert.Null(result.LimitBytes);
            Assert.Equal("2 KB used", result.SummaryText);
            Assert.Equal("No account quota reported.", result.DetailText);
            Assert.False(result.IsProgressVisible);
        }

        [Fact]
        public async Task GetCurrentAsync_refreshes_token_once_after_unauthorized_response()
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
            handler.EnqueueJson(HttpStatusCode.OK, new
            {
                usedBytes = 1024,
                quotaBytes = 4096,
                availableBytes = 3072,
            });
            var tokenStore = new FakeTokenStore("old-access", "old refresh");
            var service = CreateService(handler, tokenStore);

            CottonCloudStorageQuotaSnapshot result = await service.GetCurrentAsync(InstanceUri);

            Assert.Equal(CottonCloudStorageQuotaStatus.WithinLimit, result.Status);
            Assert.Equal("new-access", tokenStore.Tokens?.AccessToken);
            Assert.Equal("new-refresh", tokenStore.Tokens?.RefreshToken);
            Assert.Equal(3, handler.Requests.Count);
            Assert.Equal("old-access", handler.Requests[0].AuthorizationParameter);
            Assert.Null(handler.Requests[1].AuthorizationParameter);
            Assert.Equal("/api/v1/auth/refresh?refreshToken=old%20refresh", handler.Requests[1].Uri.PathAndQuery);
            Assert.Equal("new-access", handler.Requests[2].AuthorizationParameter);
            Assert.Equal("/api/v1/users/me/storage-quota", handler.Requests[2].Uri.PathAndQuery);
        }

        [Fact]
        public async Task GetCurrentAsync_requires_supported_https_instance_uri()
        {
            var service = CreateService(new RecordingHttpMessageHandler(), new FakeTokenStore("access", "refresh"));

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GetCurrentAsync(new Uri("http://cloud.example")));
        }

        private static CottonCloudStorageQuotaService CreateService(
            RecordingHttpMessageHandler handler,
            ICottonTokenStore tokenStore)
        {
            var httpClient = new HttpClient(handler);
            var apiClient = new CottonAuthenticatedApiClient(
                httpClient,
                tokenStore,
                new CottonAuthenticatedApiHttpOptions("Cotton-Mobile/1.0", "Test device"));
            return new CottonCloudStorageQuotaService(apiClient);
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
