using System.Net;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AutomaticSyncFailureClassifierTests
    {
        [Theory]
        [InlineData(HttpStatusCode.Unauthorized, CottonAutomaticSyncFailureKind.AuthenticationRequired)]
        [InlineData(HttpStatusCode.Forbidden, CottonAutomaticSyncFailureKind.AuthenticationRequired)]
        [InlineData(HttpStatusCode.NotFound, CottonAutomaticSyncFailureKind.RemoteContentUnavailable)]
        [InlineData(HttpStatusCode.InsufficientStorage, CottonAutomaticSyncFailureKind.InsufficientStorage)]
        [InlineData(HttpStatusCode.GatewayTimeout, CottonAutomaticSyncFailureKind.TimedOut)]
        [InlineData(HttpStatusCode.TooManyRequests, CottonAutomaticSyncFailureKind.ServerUnavailable)]
        [InlineData(HttpStatusCode.ServiceUnavailable, CottonAutomaticSyncFailureKind.ServerUnavailable)]
        [InlineData(HttpStatusCode.BadRequest, CottonAutomaticSyncFailureKind.ServerRejectedRequest)]
        public void ApiStatusMapsToReadableFailureKind(
            HttpStatusCode statusCode,
            CottonAutomaticSyncFailureKind expected)
        {
            CottonApiException exception = new(statusCode, "request failed", string.Empty);

            CottonAutomaticSyncFailureKind actual = CottonAutomaticSyncFailureClassifier.Classify(exception);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void LocalReadFailureIsDistinguishedFromNetworkFailure()
        {
            CottonAutomaticSyncFailureKind local = CottonAutomaticSyncFailureClassifier.Classify(
                new IOException("read failed"));
            CottonAutomaticSyncFailureKind network = CottonAutomaticSyncFailureClassifier.Classify(
                new HttpRequestException("network failed"));

            Assert.Equal(CottonAutomaticSyncFailureKind.LocalReadFailed, local);
            Assert.Equal(CottonAutomaticSyncFailureKind.NetworkUnavailable, network);
        }

        [Theory]
        [InlineData(WebExceptionStatus.NameResolutionFailure)]
        [InlineData(WebExceptionStatus.ProxyNameResolutionFailure)]
        [InlineData(WebExceptionStatus.ConnectFailure)]
        [InlineData(WebExceptionStatus.ConnectionClosed)]
        [InlineData(WebExceptionStatus.KeepAliveFailure)]
        [InlineData(WebExceptionStatus.ReceiveFailure)]
        [InlineData(WebExceptionStatus.SendFailure)]
        public void AndroidNetworkFailuresRemainRetryable(WebExceptionStatus status)
        {
            WebException exception = new("Network unavailable", status);

            CottonAutomaticSyncFailureKind actual = CottonAutomaticSyncFailureClassifier.Classify(exception);

            Assert.Equal(CottonAutomaticSyncFailureKind.NetworkUnavailable, actual);
            Assert.True(CottonAutomaticSyncRetryPolicy.IsRetryable(actual));
        }

        [Fact]
        public void AndroidNetworkTimeoutRemainsRetryable()
        {
            WebException exception = new("Request timed out", WebExceptionStatus.Timeout);

            CottonAutomaticSyncFailureKind actual = CottonAutomaticSyncFailureClassifier.Classify(exception);

            Assert.Equal(CottonAutomaticSyncFailureKind.TimedOut, actual);
            Assert.True(CottonAutomaticSyncRetryPolicy.IsRetryable(actual));
        }

        [Theory]
        [InlineData(WebExceptionStatus.TrustFailure)]
        [InlineData(WebExceptionStatus.SecureChannelFailure)]
        [InlineData(WebExceptionStatus.ProtocolError)]
        [InlineData(WebExceptionStatus.UnknownError)]
        public void OtherWebFailuresDoNotBecomeUnlimitedNetworkRetries(WebExceptionStatus status)
        {
            WebException exception = new("Request failed", status);

            CottonAutomaticSyncFailureKind actual = CottonAutomaticSyncFailureClassifier.Classify(exception);

            Assert.Equal(CottonAutomaticSyncFailureKind.Unexpected, actual);
            Assert.False(CottonAutomaticSyncRetryPolicy.IsRetryable(actual));
        }

        [Theory]
        [InlineData(CottonAutomaticSyncFailureKind.NetworkUnavailable, true)]
        [InlineData(CottonAutomaticSyncFailureKind.TimedOut, true)]
        [InlineData(CottonAutomaticSyncFailureKind.ServerUnavailable, true)]
        [InlineData(CottonAutomaticSyncFailureKind.LocalReadFailed, true)]
        [InlineData(CottonAutomaticSyncFailureKind.AuthenticationRequired, false)]
        [InlineData(CottonAutomaticSyncFailureKind.LocalAccessUnavailable, false)]
        [InlineData(CottonAutomaticSyncFailureKind.SourceChanged, false)]
        [InlineData(CottonAutomaticSyncFailureKind.ServerRejectedRequest, false)]
        [InlineData(CottonAutomaticSyncFailureKind.ActionRequired, false)]
        [InlineData(CottonAutomaticSyncFailureKind.UploadedFileChanged, false)]
        [InlineData(CottonAutomaticSyncFailureKind.PendingUploadChanged, false)]
        [InlineData(CottonAutomaticSyncFailureKind.RemotePathConflict, false)]
        [InlineData(CottonAutomaticSyncFailureKind.RemoteRevisionChanged, false)]
        [InlineData(CottonAutomaticSyncFailureKind.InvalidLocalItemName, false)]
        [InlineData(CottonAutomaticSyncFailureKind.LocalSourceUnavailable, false)]
        [InlineData(CottonAutomaticSyncFailureKind.RemoteContentUnavailable, false)]
        [InlineData(CottonAutomaticSyncFailureKind.InsufficientStorage, false)]
        [InlineData(CottonAutomaticSyncFailureKind.Unexpected, false)]
        public void RetryPolicyDistinguishesTransientFailures(
            CottonAutomaticSyncFailureKind failureKind,
            bool expected)
        {
            Assert.Equal(expected, CottonAutomaticSyncRetryPolicy.IsRetryable(failureKind));
        }
    }
}
