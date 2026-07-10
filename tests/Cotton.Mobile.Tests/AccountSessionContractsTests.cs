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
    public class AccountSessionContractsTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example");
        private static readonly DateTime CurrentLastSeen =
            new(2026, 6, 20, 6, 5, 0, DateTimeKind.Utc);
        private static readonly DateTime OlderLastSeen =
            new(2026, 6, 19, 23, 30, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetActiveSessionsAsync_sends_authorized_get_and_maps_response()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(
                HttpStatusCode.OK,
                new[]
                {
                    CreateSessionResponse(
                        "session-current",
                        " Cotton Mobile ",
                        68,
                        true,
                        CurrentLastSeen,
                        TimeSpan.FromHours(2),
                        2),
                });
            var service = CreateService(handler, new FakeTokenStore("access", "refresh"));

            IReadOnlyList<CottonAccountSessionSnapshot> result =
                await service.GetActiveSessionsAsync(InstanceUri);

            CottonAccountSessionSnapshot session = Assert.Single(result);
            Assert.Equal("session-current", session.SessionId);
            Assert.Equal("Cotton Mobile", session.Device);
            Assert.Equal("203.0.113.10", session.IpAddress);
            Assert.Equal("Cotton Mobile Android", session.UserAgent);
            Assert.Equal(68, session.AuthType);
            Assert.Equal("US", session.Country);
            Assert.Equal("CA", session.Region);
            Assert.Equal("Mountain View", session.City);
            Assert.True(session.IsCurrentSession);
            Assert.Equal(TimeSpan.FromHours(2), session.TotalSessionDuration);
            Assert.Equal(CurrentLastSeen, session.LastSeenAt);

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v1/auth/sessions", request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access", request.AuthorizationParameter);
            Assert.Equal("Cotton-Mobile/1.0", request.UserAgent);
            Assert.Equal("Test device", request.DeviceName);
            Assert.Null(request.Content);
        }

        [Fact]
        public async Task GetActiveSessionsAsync_refreshes_token_once_after_unauthorized_response()
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

            IReadOnlyList<CottonAccountSessionSnapshot> result =
                await service.GetActiveSessionsAsync(InstanceUri);

            Assert.Empty(result);
            Assert.Equal("new-access", tokenStore.Tokens?.AccessToken);
            Assert.Equal("new-refresh", tokenStore.Tokens?.RefreshToken);
            Assert.Equal(3, handler.Requests.Count);
            Assert.Equal("old-access", handler.Requests[0].AuthorizationParameter);
            Assert.Null(handler.Requests[1].AuthorizationParameter);
            Assert.Equal("/api/v1/auth/refresh?refreshToken=old%20refresh", handler.Requests[1].Uri.PathAndQuery);
            Assert.Equal("new-access", handler.Requests[2].AuthorizationParameter);
            Assert.Equal("/api/v1/auth/sessions", handler.Requests[2].Uri.PathAndQuery);
        }

        [Fact]
        public async Task RevokeSessionAsync_sends_authorized_delete_with_escaped_session_id()
        {
            var handler = new RecordingHttpMessageHandler();
            handler.EnqueueJson(HttpStatusCode.OK, new { });
            var service = CreateService(handler, new FakeTokenStore("access", "refresh"));

            await service.RevokeSessionAsync(InstanceUri, " session current/mobile?id=1 ");

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal(
                "/api/v1/auth/sessions/session%20current%2Fmobile%3Fid%3D1",
                request.Uri.PathAndQuery);
            Assert.Equal("Bearer", request.AuthorizationScheme);
            Assert.Equal("access", request.AuthorizationParameter);
            Assert.Equal("Cotton-Mobile/1.0", request.UserAgent);
            Assert.Equal("Test device", request.DeviceName);
            Assert.Null(request.Content);
        }

        [Fact]
        public async Task RevokeSessionAsync_refreshes_token_once_after_unauthorized_response()
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
            handler.EnqueueJson(HttpStatusCode.OK, new { });
            var tokenStore = new FakeTokenStore("old-access", "old refresh");
            var service = CreateService(handler, tokenStore);

            await service.RevokeSessionAsync(InstanceUri, "session-current");

            Assert.Equal("new-access", tokenStore.Tokens?.AccessToken);
            Assert.Equal("new-refresh", tokenStore.Tokens?.RefreshToken);
            Assert.Equal(3, handler.Requests.Count);
            Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
            Assert.Equal("old-access", handler.Requests[0].AuthorizationParameter);
            Assert.Null(handler.Requests[1].AuthorizationParameter);
            Assert.Equal("/api/v1/auth/refresh?refreshToken=old%20refresh", handler.Requests[1].Uri.PathAndQuery);
            Assert.Equal(HttpMethod.Delete, handler.Requests[2].Method);
            Assert.Equal("new-access", handler.Requests[2].AuthorizationParameter);
            Assert.Equal("/api/v1/auth/sessions/session-current", handler.Requests[2].Uri.PathAndQuery);
        }

        [Fact]
        public async Task RevokeSessionAsync_rejects_blank_session_id()
        {
            var service = CreateService(new RecordingHttpMessageHandler(), new FakeTokenStore("access", "refresh"));

            await Assert.ThrowsAsync<ArgumentException>(() => service.RevokeSessionAsync(InstanceUri, " "));
        }

        [Fact]
        public void DisplayState_sorts_current_session_first_and_formats_security_details()
        {
            CottonAccountSessionListDisplayState display = CottonAccountSessionListDisplayState.Create(
            [
                CreateSession(
                    "session-older",
                    "Laptop",
                    authType: 1,
                    isCurrentSession: false,
                    lastSeenAt: CurrentLastSeen.AddMinutes(10),
                    duration: TimeSpan.FromDays(2),
                    refreshTokenCount: 4),
                CreateSession(
                    "session-current",
                    "Cotton Mobile",
                    authType: 68,
                    isCurrentSession: true,
                    lastSeenAt: OlderLastSeen,
                    duration: TimeSpan.FromHours(2),
                    refreshTokenCount: 2),
            ]);

            Assert.Equal("Devices and sessions", display.Title);
            Assert.Equal("2 active", display.StatusText);
            Assert.True(display.HasItems);
            Assert.Equal("session-current", display.CurrentSessionId);
            Assert.True(display.CanRevokeCurrentSession);
            Assert.Equal("Revoke current session", display.CurrentSessionRevokeActionText);
            Assert.Empty(display.DetailText);

            CottonAccountSessionListItem current = display.Items[0];
            Assert.Equal("Cotton Mobile", current.Title);
            Assert.Equal("Current", current.BadgeText);
            Assert.Equal("Mountain View, CA, US - 203.0.113.10", current.DetailText);
            Assert.Equal("Passkey - Last seen Jun 19, 2026 23:30 UTC", current.AccessText);
            Assert.Equal("2 hours active - 2 tokens", current.DurationText);

            CottonAccountSessionListItem other = display.Items[1];
            Assert.Equal("Laptop", other.Title);
            Assert.Equal("Active", other.BadgeText);
            Assert.Equal("Password - Last seen Jun 20, 2026 06:15 UTC", other.AccessText);
            Assert.Equal("2 days active - 4 tokens", other.DurationText);
        }

        [Fact]
        public void DisplayState_handles_empty_and_unavailable_states()
        {
            CottonAccountSessionListDisplayState empty = CottonAccountSessionListDisplayState.Create(
                Array.Empty<CottonAccountSessionSnapshot>());

            Assert.Equal("No active sessions", empty.StatusText);
            Assert.False(empty.HasItems);
            Assert.Null(empty.CurrentSessionId);
            Assert.False(empty.CanRevokeCurrentSession);
            Assert.Empty(empty.DetailText);

            CottonAccountSessionListDisplayState unavailable =
                CottonAccountSessionListDisplayState.Unavailable("Could not load signed-in devices.");

            Assert.Equal("Unavailable", unavailable.StatusText);
            Assert.Null(unavailable.CurrentSessionId);
            Assert.False(unavailable.CanRevokeCurrentSession);
            Assert.Equal("Could not load signed-in devices.", unavailable.DetailText);
        }

        [Fact]
        public void AuthTypeText_formats_confirmed_backend_values()
        {
            Assert.Equal("Password", CottonAccountSessionAuthTypeText.Format(1));
            Assert.Equal("Passkey", CottonAccountSessionAuthTypeText.Format(68));
            Assert.Equal("Security key", CottonAccountSessionAuthTypeText.Format(72));
            Assert.Equal("Auth type 999", CottonAccountSessionAuthTypeText.Format(999));
        }

        private static CottonAccountSessionService CreateService(
            RecordingHttpMessageHandler handler,
            ICottonTokenStore tokenStore)
        {
            var httpClient = new HttpClient(handler);
            var apiClient = new CottonAuthenticatedApiClient(
                httpClient,
                tokenStore,
                new CottonAuthenticatedApiHttpOptions("Cotton-Mobile/1.0", "Test device"));
            return new CottonAccountSessionService(apiClient);
        }

        private static CottonAccountSessionSnapshot CreateSession(
            string sessionId,
            string device,
            int authType,
            bool isCurrentSession,
            DateTime lastSeenAt,
            TimeSpan duration,
            int refreshTokenCount)
        {
            return new CottonAccountSessionSnapshot(
                sessionId,
                device,
                "203.0.113.10",
                "Cotton Mobile Android",
                authType,
                "US",
                "CA",
                "Mountain View",
                refreshTokenCount,
                duration,
                isCurrentSession,
                lastSeenAt);
        }

        private static object CreateSessionResponse(
            string sessionId,
            string device,
            int authType,
            bool isCurrentSession,
            DateTime lastSeenAt,
            TimeSpan duration,
            int refreshTokenCount)
        {
            return new
            {
                sessionId,
                device,
                ipAddress = "203.0.113.10",
                userAgent = "Cotton Mobile Android",
                authType,
                country = "US",
                region = "CA",
                city = "Mountain View",
                refreshTokenCount,
                totalSessionDuration = duration,
                isCurrentSession,
                lastSeenAt,
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
