using System.Net;
using System.Text;

namespace Cotton.Mobile.Tests
{
    internal class RotatingTokenHttpMessageHandler : HttpMessageHandler
    {
        public List<string> RequestPaths { get; } = [];

        public List<string?> AuthorizationTokens { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string path = request.RequestUri?.PathAndQuery
                ?? throw new InvalidOperationException("Request URI is unavailable.");
            string? authorizationToken = request.Headers.Authorization?.Parameter;
            RequestPaths.Add(path);
            AuthorizationTokens.Add(authorizationToken);

            HttpResponseMessage response = path switch
            {
                "/api/v1/settings" when authorizationToken == "expired-access" =>
                    CreateResponse(HttpStatusCode.Unauthorized, "expired"),
                "/api/v1/auth/refresh?refreshToken=current-refresh" =>
                    CreateResponse(
                        HttpStatusCode.OK,
                        "{\"accessToken\":\"rotated-access\",\"refreshToken\":\"rotated-refresh\"}"),
                "/api/v1/settings" when authorizationToken == "rotated-access" =>
                    CreateResponse(
                        HttpStatusCode.OK,
                        "{\"version\":\"1.0.0\",\"maxChunkSizeBytes\":4194304,\"supportedHashAlgorithm\":\"SHA256\"}"),
                _ => CreateResponse(HttpStatusCode.BadRequest, "unexpected request"),
            };
            return Task.FromResult(response);
        }

        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };
        }
    }
}
