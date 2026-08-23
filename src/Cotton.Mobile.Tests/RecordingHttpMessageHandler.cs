using System.Net;

namespace Cotton.Mobile.Tests
{
    internal class RecordingHttpMessageHandler(
        HttpStatusCode statusCode,
        string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            HttpResponseMessage response = new(statusCode)
            {
                Content = new StringContent(responseBody),
            };
            return Task.FromResult(response);
        }
    }
}
