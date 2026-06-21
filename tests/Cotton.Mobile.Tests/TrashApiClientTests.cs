using System.Net;
using System.Text;
using System.Text.Json;
using Cotton.Auth;
using Cotton.Files;
using Cotton.Mobile.Services;
using Cotton.Sdk.Auth;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TrashApiClientTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example");
        private static readonly Guid FileId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid FolderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        [Fact]
        public async Task MoveFileToTrashAsync_sends_authorized_delete_with_expected_etag()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueStatus(HttpStatusCode.NoContent);
            var client = new CottonApiFileTrashClient(CreateApiClient(handler));

            await client.MoveFileToTrashAsync(InstanceUri, FileId, " etag value/1 ");

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal(
                $"/api/v1/files/{FileId}?skipTrash=false&expectedETag=etag%20value%2F1",
                request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access-token", request.AuthorizationParameter);
        }

        [Fact]
        public async Task MoveFolderToTrashAsync_sends_authorized_node_delete()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueStatus(HttpStatusCode.NoContent);
            var client = new CottonApiFolderTrashClient(CreateApiClient(handler));

            await client.MoveFolderToTrashAsync(InstanceUri, FolderId);

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal($"/api/v1/layouts/nodes/{FolderId}?skipTrash=false", request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access-token", request.AuthorizationParameter);
        }

        [Fact]
        public async Task RestoreFileAsync_posts_authorized_restore_request()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, new RestoreOutcomeDto
            {
                Status = RestoreStatus.Restored,
            });
            var client = new CottonApiTrashRestoreClient(CreateApiClient(handler));
            var request = new RestoreItemRequestDto
            {
                CreateMissingParents = true,
            };

            RestoreOutcomeDto result = await client.RestoreFileAsync(InstanceUri, FileId, request);

            Assert.Equal(RestoreStatus.Restored, result.Status);
            RecordedRequest recorded = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, recorded.Method);
            Assert.Equal($"/api/v1/files/{FileId}/restore", recorded.Uri.PathAndQuery);
            Assert.Equal("Bearer", recorded.AuthorizationScheme);
            Assert.Equal("access-token", recorded.AuthorizationParameter);
            using JsonDocument body = JsonDocument.Parse(recorded.Body);
            Assert.True(body.RootElement.GetProperty("createMissingParents").GetBoolean());
        }

        [Fact]
        public async Task RestoreFolderAsync_posts_authorized_node_restore_request()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, new RestoreOutcomeDto
            {
                Status = RestoreStatus.Restored,
            });
            var client = new CottonApiTrashRestoreClient(CreateApiClient(handler));
            var request = new RestoreItemRequestDto
            {
                Overwrite = true,
            };

            RestoreOutcomeDto result = await client.RestoreFolderAsync(InstanceUri, FolderId, request);

            Assert.Equal(RestoreStatus.Restored, result.Status);
            RecordedRequest recorded = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, recorded.Method);
            Assert.Equal($"/api/v1/layouts/nodes/{FolderId}/restore", recorded.Uri.PathAndQuery);
            Assert.Equal("Bearer", recorded.AuthorizationScheme);
            Assert.Equal("access-token", recorded.AuthorizationParameter);
            using JsonDocument body = JsonDocument.Parse(recorded.Body);
            Assert.True(body.RootElement.GetProperty("overwrite").GetBoolean());
        }

        [Fact]
        public async Task DeleteFileForeverAsync_sends_authorized_permanent_delete_with_if_match()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueStatus(HttpStatusCode.NoContent);
            var client = new CottonApiTrashPermanentDeleteClient(CreateApiClient(handler));

            await client.DeleteFileForeverAsync(InstanceUri, FileId, " \"etag-current\" ");

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal($"/api/v1/files/{FileId}?skipTrash=true", request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access-token", request.AuthorizationParameter);
            Assert.Equal("\"etag-current\"", request.Headers["If-Match"]);
        }

        [Fact]
        public async Task DeleteFolderForeverAsync_sends_authorized_permanent_node_delete()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueStatus(HttpStatusCode.NoContent);
            var client = new CottonApiTrashPermanentDeleteClient(CreateApiClient(handler));

            await client.DeleteFolderForeverAsync(InstanceUri, FolderId);

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal($"/api/v1/layouts/nodes/{FolderId}?skipTrash=true", request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access-token", request.AuthorizationParameter);
            Assert.False(request.Headers.ContainsKey("If-Match"));
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

            public void EnqueueStatus(HttpStatusCode statusCode)
            {
                _responses.Enqueue(new HttpResponseMessage(statusCode));
            }

            public void EnqueueJson(HttpStatusCode statusCode, object value)
            {
                string json = JsonSerializer.Serialize(value, JsonOptions);
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

                string body = request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Requests.Add(new RecordedRequest(
                    request.Method,
                    request.RequestUri ?? throw new InvalidOperationException("Request URI is missing."),
                    request.Headers.Authorization?.Scheme,
                    request.Headers.Authorization?.Parameter,
                    request.Headers.ToDictionary(
                        header => header.Key,
                        header => string.Join(",", header.Value),
                        StringComparer.OrdinalIgnoreCase),
                    body));
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
                IReadOnlyDictionary<string, string> headers,
                string body)
            {
                Method = method;
                Uri = uri;
                AuthorizationScheme = authorizationScheme;
                AuthorizationParameter = authorizationParameter;
                Headers = headers;
                Body = body;
            }

            public HttpMethod Method { get; }

            public Uri Uri { get; }

            public string? AuthorizationScheme { get; }

            public string? AuthorizationParameter { get; }

            public IReadOnlyDictionary<string, string> Headers { get; }

            public string Body { get; }
        }
    }
}
