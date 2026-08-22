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
        [InlineData(HttpStatusCode.GatewayTimeout, CottonAutomaticSyncFailureKind.TimedOut)]
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
    }
}
