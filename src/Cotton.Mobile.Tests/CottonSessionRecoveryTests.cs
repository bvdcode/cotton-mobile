using System.Net;
using System.Net.Http.Json;
using Cotton.Auth;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CottonSessionRecoveryTests
    {
        [Fact]
        public async Task CanceledRestoreRetainsReceivedTokensAndNextRestoreAuthenticatesWithoutSignIn()
        {
            CancellationToken testCancellation = TestContext.Current.CancellationToken;
            using SessionRecoveryTestContext context = new();
            await context.InitializeAsync(testCancellation);
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(testCancellation);
            CancelingSessionTokenStore tokens = new(context.Tokens, cancellation);
            SessionValidationTestClientFactory clients = new(context.HttpClient, tokens);
            CottonSessionService service = new(
                clients, new CottonSessionValidator(clients), context.Instances, tokens,
                context.Authorization, context.Cursor, context.Authorization,
                context.Notifications, NullLogger<CottonSessionService>.Instance);
            context.Handler.RefreshFailure = () =>
            {
                if (context.Handler.RefreshCount == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new TokenPairDto
                        {
                            AccessToken = "renewed-access",
                            RefreshToken = "renewed-refresh",
                        }),
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(
                        "{\"status\":404,\"code\":\"not_found\",\"instance\":\"/api/v1/auth/refresh\"}"),
                };
            };

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RestoreAsync(cancellation.Token));
            CottonSessionResult restored = await service.RestoreAsync(testCancellation);

            Assert.True(restored.IsAuthenticated);
            Assert.Equal("renewed-refresh", (await tokens.GetAsync(testCancellation))?.RefreshToken);
            Assert.Equal(SessionRecoveryTestContext.Instance, context.Instances.Instance);
            Assert.NotNull(context.Cursor.Cursor);
            Assert.Equal(0, context.Authorization.ClearCount);
            Assert.Equal(1, context.Notifications.ClearCount);
            Assert.Equal(1, context.Handler.RefreshCount);
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound, "404 page not found")]
        [InlineData(HttpStatusCode.ServiceUnavailable, "maintenance")]
        [InlineData(HttpStatusCode.Unauthorized, "gateway unauthorized")]
        [InlineData(HttpStatusCode.Forbidden, "gateway forbidden")]
        [InlineData(HttpStatusCode.NotFound, "{\"status\":404,\"title\":\"Not Found\"}")]
        public async Task RepeatedRefreshOutagePreservesSessionThenUploadsWithoutSignIn(HttpStatusCode status, string body)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            using SessionRecoveryTestContext context = new();
            await context.InitializeAsync(cancellationToken);
            context.Handler.RefreshFailure = () => new HttpResponseMessage(status) { Content = new StringContent(body) };
            for (int attempt = 0; attempt < 5; attempt++)
            {
                CottonTokenRefreshException failure = await Assert.ThrowsAsync<CottonTokenRefreshException>(
                    () => context.CreateService().RestoreAsync(cancellationToken));
                Assert.True(CottonAutomaticSyncRetryPolicy.IsRetryable(CottonAutomaticSyncFailureClassifier.Classify(failure)));
                Assert.Equal("current-refresh", (await context.Tokens.GetAsync(cancellationToken))?.RefreshToken);
                Assert.Equal(SessionRecoveryTestContext.Instance, context.Instances.Instance);
                Assert.NotNull(context.Cursor.Cursor);
                Assert.Equal(0, context.Authorization.ClearCount);
                Assert.Equal(0, context.Notifications.ClearCount);
            }

            context.Handler.RefreshFailure = null;
            CottonSessionResult restored = await context.CreateService().RestoreAsync(cancellationToken);
            Assert.True(restored.IsAuthenticated);
            Assert.Equal(1, context.Notifications.ClearCount);
            Assert.Equal("renewed-refresh", (await context.Tokens.GetAsync(cancellationToken))?.RefreshToken);
            Assert.Equal(6, context.Handler.RefreshCount);

            byte[] bytes = [1, 2, 3, 4];
            CottonFileUploadSource source = new(
                new CottonFileUploadSourceSnapshot("photo.jpg", "image/jpeg", bytes.Length,
                    new Dictionary<string, string> { [CottonFileUploadMetadataKeys.UploadOperationId] = "recovered-upload" },
                    CottonContentHash.ComputeSha256(bytes)),
                _ => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
            CottonFileUploadService uploads = new(context.Clients);
            CottonFileBrowserEntry uploaded = await uploads.UploadAsync(
                SessionRecoveryTestContext.Instance, new CottonFolderHandle(Guid.NewGuid(), "Photos"),
                source, cancellationToken: cancellationToken);

            Assert.Equal(bytes, Assert.Single(context.Uploads.Chunks).Value);
            Assert.Single(context.Uploads.PublishedFiles);
            Assert.Equal(CottonContentHash.ComputeSha256(bytes), uploaded.ContentHash);
            Assert.Equal("renewed-access", context.Handler.AccessTokens[^1]);
        }

        [Fact]
        public async Task ConfirmedRefreshRejectionExpiresSessionButPreservesServer()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            using SessionRecoveryTestContext context = new();
            await context.InitializeAsync(cancellationToken);
            context.Handler.RefreshFailure = () => new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"status\":404,\"code\":\"not_found\",\"instance\":\"/api/v1/auth/refresh\"}"),
            };

            CottonSessionResult result = await context.CreateService().RestoreAsync(cancellationToken);

            Assert.Equal(CottonSessionResultStatus.SessionExpired, result.Status);
            Assert.Null(await context.Tokens.GetAsync(cancellationToken));
            Assert.Equal(SessionRecoveryTestContext.Instance, context.Instances.Instance);
            Assert.Null(context.Cursor.Cursor);
            Assert.Equal(1, context.Authorization.ClearCount);
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task ProfileHttpFailureDoesNotDeleteSession(HttpStatusCode status)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            using SessionRecoveryTestContext context = new();
            await context.InitializeAsync(cancellationToken);
            context.Handler.ProfileFailure = status;

            await Assert.ThrowsAsync<CottonApiException>(() => context.CreateService().RestoreAsync(cancellationToken));

            Assert.Equal("current-refresh", (await context.Tokens.GetAsync(cancellationToken))?.RefreshToken);
            Assert.NotNull(context.Cursor.Cursor);
            Assert.Equal(0, context.Authorization.ClearCount);
        }
    }
}
