using System.Net;
using System.Text;

namespace Cotton.Mobile.Tests
{
    internal class SessionValidationHttpMessageHandler(bool accessTokenIsExpired) : HttpMessageHandler
    {
        public List<string> RequestPaths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string path = request.RequestUri?.PathAndQuery
                ?? throw new InvalidOperationException("Request URI is unavailable.");
            string? authorizationToken = request.Headers.Authorization?.Parameter;
            RequestPaths.Add(path);

            HttpResponseMessage response = path switch
            {
                "/api/v1/auth/me" when accessTokenIsExpired && authorizationToken == "current-access" =>
                    CreateResponse(HttpStatusCode.Unauthorized, "expired"),
                "/api/v1/auth/refresh?refreshToken=current-refresh" =>
                    CreateResponse(
                        HttpStatusCode.OK,
                        "{\"accessToken\":\"rotated-access\",\"refreshToken\":\"rotated-refresh\"}"),
                "/api/v1/auth/me" when authorizationToken is "current-access" or "rotated-access" =>
                    CreateResponse(
                        HttpStatusCode.OK,
                        "{\"username\":\"session-user\",\"role\":0}"),
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
