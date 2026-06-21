using System.Net;
using System.Text;
using System.Text.Json;
using Cotton.Auth;
using Cotton.Mobile.Services;
using Cotton.Nodes;
using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TrashBrowserApiClientTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example");
        private static readonly Guid TrashRootId = Guid.Parse("11111111-1111-4111-8111-111111111111");

        [Fact]
        public async Task GetTrashRootAsync_sends_authorized_trash_resolver_request()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, new NodeDto
            {
                Id = TrashRootId,
                Name = "Trash",
            });
            var client = new CottonApiTrashBrowserClient(CreateApiClient(handler));

            NodeDto result = await client.GetTrashRootAsync(InstanceUri);

            Assert.Equal(TrashRootId, result.Id);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v1/layouts/resolver?nodeType=Trash", request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access-token", request.AuthorizationParameter);
        }

        [Fact]
        public async Task GetChildrenAsync_sends_authorized_trash_children_request()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, new NodeContentDto
            {
                Id = TrashRootId,
                TotalCount = 0,
            });
            var client = new CottonApiTrashBrowserClient(CreateApiClient(handler));

            NodeContentDto result = await client.GetChildrenAsync(
                InstanceUri,
                TrashRootId,
                page: 2,
                pageSize: 50);

            Assert.Equal(0, result.TotalCount);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                $"/api/v1/layouts/nodes/{TrashRootId}/children?nodeType=Trash&page=2&pageSize=50&depth=1",
                request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access-token", request.AuthorizationParameter);
        }

        [Fact]
        public async Task GetChildrenAsync_rejects_invalid_request_shape()
        {
            var client = new CottonApiTrashBrowserClient(CreateApiClient(new RecordingHttpMessageHandler()));

            await Assert.ThrowsAsync<ArgumentException>(
                () => client.GetChildrenAsync(InstanceUri, Guid.Empty, page: 1, pageSize: 100));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetChildrenAsync(InstanceUri, TrashRootId, page: 0, pageSize: 100));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetChildrenAsync(InstanceUri, TrashRootId, page: 1, pageSize: 0));
        }

        private static CottonAuthenticatedApiClient CreateApiClient(RecordingHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            return new CottonAuthenticatedApiClient(
                httpClient,
                new FakeTokenStore(),
                new CottonAuthenticatedApiHttpOptions("Cotton-Mobile/1.0", "Test device"));
        }

        private class FakeTokenStore : ICottonTokenStore
        {
            public Task<TokenPairDto?> GetAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<TokenPairDto?>(new TokenPairDto
                {
                    AccessToken = "access-token",
                    RefreshToken = "refresh-token",
                });
            }

            public Task SaveAsync(TokenPairDto tokens, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task ClearAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private class RecordingHttpMessageHandler : HttpMessageHandler
        {
            private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
            private readonly Queue<HttpResponseMessage> _responses = [];

            public List<RecordedRequest> Requests { get; } = [];

            public void EnqueueJson(HttpStatusCode statusCode, object value)
            {
                string json = JsonSerializer.Serialize(value, JsonOptions);
                _responses.Enqueue(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
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
                    request.Headers.Authorization?.Parameter));
                return Task.FromResult(_responses.Dequeue());
            }
        }

        private class RecordedRequest
        {
            public RecordedRequest(
                HttpMethod method,
                Uri uri,
                string? authorizationScheme,
                string? authorizationParameter)
            {
                Method = method;
                Uri = uri;
                AuthorizationScheme = authorizationScheme;
                AuthorizationParameter = authorizationParameter;
            }

            public HttpMethod Method { get; }

            public Uri Uri { get; }

            public string? AuthorizationScheme { get; }

            public string? AuthorizationParameter { get; }
        }
    }
}
