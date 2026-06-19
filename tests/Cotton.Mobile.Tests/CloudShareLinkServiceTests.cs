using System.Net;
using System.Text;
using System.Text.Json;
using Cotton;
using Cotton.Auth;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudShareLinkServiceTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example");
        private static readonly Guid FileId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid FolderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        [Fact]
        public async Task CreateAsync_creates_file_link_with_authorized_request()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, "/api/v1/files/download?token=file-token");
            var tokenStore = new FakeTokenStore("access", "refresh");
            var service = CreateService(handler, tokenStore);

            CottonCloudShareLinkSnapshot result = await service.CreateAsync(
                InstanceUri,
                CottonCloudShareLinkRequest.ForFile(FileId));

            Assert.Equal("https://cloud.example/s/file-token", result.ShareUrl);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                $"/api/v1/files/{FileId}/download-link?expireAfterMinutes=1440",
                request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access", request.AuthorizationParameter);
            Assert.Equal("Cotton-Mobile/1.0", request.UserAgent);
            Assert.Equal("Test device", request.DeviceName);
        }

        [Fact]
        public async Task CreateAsync_creates_folder_link_with_custom_lifetime()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, "/s/folder-token");
            var service = CreateService(handler, new FakeTokenStore("access", "refresh"));

            CottonCloudShareLinkSnapshot result = await service.CreateAsync(
                InstanceUri,
                CottonCloudShareLinkRequest.ForFolder(FolderId, expireAfterMinutes: 60));

            Assert.Equal(CottonCloudShareLinkTargetKind.Folder, result.TargetKind);
            Assert.Equal("https://cloud.example/s/folder-token", result.ShareUrl);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                $"/api/v1/layouts/nodes/{FolderId}/share-link?expireAfterMinutes=60",
                request.Uri.PathAndQuery);
        }

        [Fact]
        public async Task CreateAsync_refreshes_token_once_after_unauthorized_response()
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
            handler.EnqueueJson(HttpStatusCode.OK, "/s/refreshed-token");
            var tokenStore = new FakeTokenStore("old-access", "old refresh");
            var service = CreateService(handler, tokenStore);

            CottonCloudShareLinkSnapshot result = await service.CreateAsync(
                InstanceUri,
                CottonCloudShareLinkRequest.ForFile(FileId));

            Assert.Equal("https://cloud.example/s/refreshed-token", result.ShareUrl);
            Assert.Equal("new-access", tokenStore.Tokens?.AccessToken);
            Assert.Equal("new-refresh", tokenStore.Tokens?.RefreshToken);
            Assert.Equal(3, handler.Requests.Count);
            Assert.Equal("old-access", handler.Requests[0].AuthorizationParameter);
            Assert.Null(handler.Requests[1].AuthorizationParameter);
            Assert.Equal("/api/v1/auth/refresh?refreshToken=old%20refresh", handler.Requests[1].Uri.PathAndQuery);
            Assert.Equal("new-access", handler.Requests[2].AuthorizationParameter);
        }

        [Fact]
        public async Task CreateAsync_uses_newer_token_when_another_request_already_refreshed()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.Unauthorized, new { error = "expired" });
            handler.EnqueueJson(HttpStatusCode.OK, "/s/refreshed-token");
            var tokenStore = new FakeTokenStore("old-access", "refresh");
            var service = CreateService(handler, tokenStore);
            handler.AfterFirstRequest = () => tokenStore.Tokens = new TokenPairDto
            {
                AccessToken = "new-access",
                RefreshToken = "new-refresh",
            };

            CottonCloudShareLinkSnapshot result = await service.CreateAsync(
                InstanceUri,
                CottonCloudShareLinkRequest.ForFile(FileId));

            Assert.Equal("https://cloud.example/s/refreshed-token", result.ShareUrl);
            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal("old-access", handler.Requests[0].AuthorizationParameter);
            Assert.Equal("new-access", handler.Requests[1].AuthorizationParameter);
        }

        [Fact]
        public async Task CreateAsync_clears_tokens_when_refresh_fails()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.Unauthorized, new { error = "expired" });
            handler.EnqueueJson(HttpStatusCode.Forbidden, new { error = "revoked" });
            handler.EnqueueJson(HttpStatusCode.Unauthorized, new { error = "expired" });
            var tokenStore = new FakeTokenStore("old-access", "old-refresh");
            var service = CreateService(handler, tokenStore);

            CottonApiException exception = await Assert.ThrowsAsync<CottonApiException>(
                () => service.CreateAsync(InstanceUri, CottonCloudShareLinkRequest.ForFile(FileId)));

            Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
            Assert.Null(tokenStore.Tokens);
            Assert.Equal(3, handler.Requests.Count);
        }

        [Fact]
        public async Task CreateAsync_surfaces_not_found_without_refresh()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.NotFound, new { error = "missing" });
            var service = CreateService(handler, new FakeTokenStore("access", "refresh"));

            CottonApiException exception = await Assert.ThrowsAsync<CottonApiException>(
                () => service.CreateAsync(InstanceUri, CottonCloudShareLinkRequest.ForFile(FileId)));

            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task CreateAsync_requires_supported_https_instance_uri()
        {
            var service = CreateService(new RecordingHttpMessageHandler(), new FakeTokenStore("access", "refresh"));

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateAsync(
                    new Uri("http://cloud.example"),
                    CottonCloudShareLinkRequest.ForFile(FileId)));
        }

        [Fact]
        public async Task CreateAsync_truncates_device_name_header()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, "/s/file-token");
            var tokenStore = new FakeTokenStore("access", "refresh");
            var longDeviceName = new string('d', CottonClientHeaders.DeviceNameMaxLength + 1);
            using var httpClient = new HttpClient(handler);
            var service = new CottonCloudShareLinkService(
                httpClient,
                tokenStore,
                new CottonCloudShareLinkHttpOptions("Cotton-Mobile/1.0", longDeviceName));

            await service.CreateAsync(InstanceUri, CottonCloudShareLinkRequest.ForFile(FileId));

            Assert.Equal(CottonClientHeaders.DeviceNameMaxLength, Assert.Single(handler.Requests).DeviceName!.Length);
        }

        private static CottonCloudShareLinkService CreateService(
            RecordingHttpMessageHandler handler,
            ICottonTokenStore tokenStore)
        {
            var httpClient = new HttpClient(handler);
            return new CottonCloudShareLinkService(
                httpClient,
                tokenStore,
                new CottonCloudShareLinkHttpOptions("Cotton-Mobile/1.0", "Test device"));
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
            private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = [];
            private int _requestCount;

            public Action? AfterFirstRequest { get; set; }

            public List<RecordedRequest> Requests { get; } = [];

            public void EnqueueJson(HttpStatusCode statusCode, object value, Action? afterRequest = null)
            {
                _responses.Enqueue(_ =>
                {
                    afterRequest?.Invoke();
                    string json = JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    return new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json"),
                    };
                });
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                if (_responses.Count == 0)
                {
                    throw new InvalidOperationException("No queued HTTP response.");
                }

                Requests.Add(new RecordedRequest(
                    request.Method,
                    request.RequestUri ?? throw new InvalidOperationException("Request URI is missing."),
                    request.Headers.Authorization?.Scheme,
                    request.Headers.Authorization?.Parameter,
                    request.Headers.UserAgent.ToString(),
                    request.Headers.TryGetValues(CottonClientHeaders.DeviceName, out IEnumerable<string>? deviceNames)
                        ? deviceNames.SingleOrDefault()
                        : null));
                _requestCount++;
                if (_requestCount == 1)
                {
                    AfterFirstRequest?.Invoke();
                }

                return Task.FromResult(_responses.Dequeue().Invoke(request));
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
                string? deviceName)
            {
                Method = method;
                Uri = uri;
                AuthorizationScheme = authorizationScheme;
                AuthorizationParameter = authorizationParameter;
                UserAgent = userAgent;
                DeviceName = deviceName;
            }

            public HttpMethod Method { get; }

            public Uri Uri { get; }

            public string? AuthorizationScheme { get; }

            public string? AuthorizationParameter { get; }

            public string UserAgent { get; }

            public string? DeviceName { get; }
        }
    }
}
