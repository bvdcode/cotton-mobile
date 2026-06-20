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
    public class ActivityFeedServiceTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example");
        private static readonly Guid FirstNotificationId =
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid SecondNotificationId =
            Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly DateTime CreatedAt =
            new(2026, 6, 20, 4, 45, 0, DateTimeKind.Utc);
        private static readonly DateTime ReadAt =
            new(2026, 6, 20, 5, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetPageAsync_reads_authorized_notifications_feed()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(
                HttpStatusCode.OK,
                new object[]
                {
                    new
                    {
                        id = FirstNotificationId,
                        title = " Shared file downloaded ",
                        content = " Report.pdf was downloaded. ",
                        createdAt = CreatedAt,
                        readAt = (DateTime?)null,
                        priority = CottonActivityFeedPriority.Medium,
                        metadata = new Dictionary<string, string>
                        {
                            ["fileName"] = "Report.pdf",
                        },
                    },
                    new
                    {
                        id = SecondNotificationId,
                        title = "Security",
                        content = (string?)null,
                        createdAt = CreatedAt.AddMinutes(-5),
                        readAt = (DateTime?)ReadAt,
                        priority = CottonActivityFeedPriority.High,
                        metadata = (Dictionary<string, string>?)null,
                    },
                });
            var service = CreateService(handler, new FakeTokenStore("access", "refresh"));

            CottonActivityFeedPageSnapshot page = await service.GetPageAsync(
                InstanceUri,
                new CottonActivityFeedQuery(page: 2, pageSize: 3));

            Assert.Equal(2, page.Query.Page);
            Assert.Equal(3, page.Query.PageSize);
            Assert.False(page.IsEmpty);
            Assert.False(page.MayHaveMore);

            CottonActivityFeedItemSnapshot first = page.Items[0];
            Assert.Equal(FirstNotificationId, first.Id);
            Assert.Equal("Shared file downloaded", first.Title);
            Assert.Equal("Report.pdf was downloaded.", first.Content);
            Assert.Equal(CreatedAt, first.CreatedAt);
            Assert.Null(first.ReadAt);
            Assert.True(first.IsUnread);
            Assert.Equal(CottonActivityFeedPriority.Medium, first.Priority);
            Assert.Equal("Report.pdf", first.Metadata["fileName"]);

            CottonActivityFeedItemSnapshot second = page.Items[1];
            Assert.Equal(SecondNotificationId, second.Id);
            Assert.Equal("Security", second.Title);
            Assert.Null(second.Content);
            Assert.Equal(ReadAt, second.ReadAt);
            Assert.False(second.IsUnread);
            Assert.Equal(CottonActivityFeedPriority.High, second.Priority);
            Assert.Empty(second.Metadata);

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v1/notifications?page=2&pageSize=3", request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access", request.AuthorizationParameter);
            Assert.Equal("Cotton-Mobile/1.0", request.UserAgent);
            Assert.Equal("Test device", request.DeviceName);
            Assert.Null(request.Content);
        }

        [Fact]
        public async Task GetPageAsync_refreshes_token_once_after_unauthorized_response()
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
            handler.EnqueueJson(HttpStatusCode.OK, Array.Empty<object>());
            var tokenStore = new FakeTokenStore("old-access", "old refresh");
            var service = CreateService(handler, tokenStore);

            CottonActivityFeedPageSnapshot page = await service.GetPageAsync(
                InstanceUri,
                new CottonActivityFeedQuery());

            Assert.True(page.IsEmpty);
            Assert.Equal("new-access", tokenStore.Tokens?.AccessToken);
            Assert.Equal("new-refresh", tokenStore.Tokens?.RefreshToken);
            Assert.Equal(3, handler.Requests.Count);
            Assert.Equal("old-access", handler.Requests[0].AuthorizationParameter);
            Assert.Null(handler.Requests[1].AuthorizationParameter);
            Assert.Equal("/api/v1/auth/refresh?refreshToken=old%20refresh", handler.Requests[1].Uri.PathAndQuery);
            Assert.Equal("new-access", handler.Requests[2].AuthorizationParameter);
            Assert.Equal("/api/v1/notifications?page=1&pageSize=20", handler.Requests[2].Uri.PathAndQuery);
        }

        [Fact]
        public async Task GetPageAsync_requires_supported_https_instance_uri()
        {
            var service = CreateService(new RecordingHttpMessageHandler(), new FakeTokenStore("access", "refresh"));

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GetPageAsync(
                    new Uri("http://cloud.example"),
                    new CottonActivityFeedQuery()));
        }

        [Fact]
        public void Query_validates_positive_paging()
        {
            var query = new CottonActivityFeedQuery(page: 5, pageSize: 10);

            Assert.Equal("page=5&pageSize=10", query.ToQueryString());
            Assert.Throws<ArgumentOutOfRangeException>(() => new CottonActivityFeedQuery(page: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CottonActivityFeedQuery(pageSize: 0));
        }

        [Fact]
        public void Page_snapshot_marks_full_pages_as_maybe_more()
        {
            CottonActivityFeedItemSnapshot item = CreateItem();

            var full = new CottonActivityFeedPageSnapshot(
                new CottonActivityFeedQuery(pageSize: 1),
                [item]);
            var partial = new CottonActivityFeedPageSnapshot(
                new CottonActivityFeedQuery(pageSize: 2),
                [item]);

            Assert.True(full.MayHaveMore);
            Assert.False(partial.MayHaveMore);
        }

        [Fact]
        public void Item_snapshot_normalizes_and_copies_metadata()
        {
            var metadata = new Dictionary<string, string>
            {
                ["kind"] = "security",
            };

            CottonActivityFeedItemSnapshot item = new(
                FirstNotificationId,
                " Security ",
                " ",
                CreatedAt,
                null,
                CottonActivityFeedPriority.High,
                metadata);
            metadata["kind"] = "changed";

            Assert.Equal("Security", item.Title);
            Assert.Null(item.Content);
            Assert.Equal("security", item.Metadata["kind"]);
            Assert.True(item.IsUnread);
            Assert.Throws<ArgumentException>(
                () => new CottonActivityFeedItemSnapshot(
                    Guid.Empty,
                    "Title",
                    null,
                    CreatedAt,
                    null,
                    CottonActivityFeedPriority.Low,
                    null));
            Assert.Throws<ArgumentException>(
                () => new CottonActivityFeedItemSnapshot(
                    FirstNotificationId,
                    " ",
                    null,
                    CreatedAt,
                    null,
                    CottonActivityFeedPriority.Low,
                    null));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonActivityFeedItemSnapshot(
                    FirstNotificationId,
                    "Title",
                    null,
                    CreatedAt,
                    null,
                    (CottonActivityFeedPriority)99,
                    null));
        }

        private static CottonActivityFeedItemSnapshot CreateItem()
        {
            return new CottonActivityFeedItemSnapshot(
                FirstNotificationId,
                "Activity",
                null,
                CreatedAt,
                null,
                CottonActivityFeedPriority.Low,
                null);
        }

        private static CottonActivityFeedService CreateService(
            RecordingHttpMessageHandler handler,
            ICottonTokenStore tokenStore)
        {
            var httpClient = new HttpClient(handler);
            var apiClient = new CottonAuthenticatedApiClient(
                httpClient,
                tokenStore,
                new CottonAuthenticatedApiHttpOptions("Cotton-Mobile/1.0", "Test device"));
            return new CottonActivityFeedService(apiClient);
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

        private record RecordedRequest(
            HttpMethod Method,
            Uri Uri,
            string? AuthorizationScheme,
            string? AuthorizationParameter,
            string UserAgent,
            string? DeviceName,
            string? Content);
    }
}
