using System.Net;
using System.Net.Http.Json;
using Cotton.Auth;

namespace Cotton.Mobile.Tests
{
    internal class SessionRecoveryHttpHandler(HttpMessageHandler uploadHandler) : DelegatingHandler(uploadHandler)
    {
        public Func<HttpResponseMessage>? RefreshFailure { get; set; }
        public HttpStatusCode? ProfileFailure { get; set; }
        public int RefreshCount { get; private set; }
        public List<string?> AccessTokens { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? path = request.RequestUri?.AbsolutePath;
            if (path == "/api/v1/auth/refresh")
            {
                RefreshCount++;
                return Task.FromResult(RefreshFailure?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new TokenPairDto
                    {
                        AccessToken = "renewed-access",
                        RefreshToken = "renewed-refresh",
                    }),
                });
            }

            AccessTokens.Add(request.Headers.Authorization?.Parameter);
            if (path == "/api/v1/auth/me" && ProfileFailure.HasValue)
            {
                return Task.FromResult(new HttpResponseMessage(ProfileFailure.Value));
            }

            if (request.Headers.Authorization?.Parameter != "renewed-access")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            if (path == "/api/v1/auth/me")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new UserDto { Id = Guid.NewGuid(), Username = "account" }),
                });
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
