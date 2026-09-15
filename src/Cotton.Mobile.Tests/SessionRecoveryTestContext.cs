using Cotton.Auth;
using Cotton.Mobile.Services;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cotton.Mobile.Tests
{
    internal class SessionRecoveryTestContext : IDisposable
    {
        public static readonly Uri Instance = new("https://cotton.test");
        public UploadHttpMessageHandler Uploads { get; } = new();
        public SessionRecoveryHttpHandler Handler { get; }
        public HttpClient HttpClient { get; }
        public InMemoryCottonTokenStore Tokens { get; } = new();
        public SessionRecoveryInstanceStore Instances { get; } = new(Instance);
        public SessionRecoveryNotifications Notifications { get; } = new();
        public SessionRecoveryAuthorization Authorization { get; } = new();
        public InMemoryCottonNotificationCursorStore Cursor { get; } = new(new(DateTime.UtcNow, Guid.NewGuid()));
        public SessionValidationTestClientFactory Clients { get; }

        public SessionRecoveryTestContext()
        {
            Handler = new(Uploads);
            HttpClient = new(Handler);
            Clients = new(HttpClient, Tokens);
        }

        public Task InitializeAsync(CancellationToken cancellationToken) => Tokens.SaveAsync(
            new TokenPairDto { AccessToken = "expired-access", RefreshToken = "current-refresh" }, cancellationToken);

        public CottonSessionService CreateService() => new(
            Clients, new CottonSessionValidator(Clients), Instances, Tokens,
            Authorization, Cursor, Authorization, Notifications, NullLogger<CottonSessionService>.Instance);

        public void Dispose() => HttpClient.Dispose();
    }
}
