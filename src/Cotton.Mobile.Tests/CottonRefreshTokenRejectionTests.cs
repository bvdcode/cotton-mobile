using System.Net;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CottonRefreshTokenRejectionTests
    {
        [Theory]
        [InlineData("{\"status\":404,\"code\":\"not_found\",\"instance\":\"/api/v1/auth/refresh\"}", true)]
        [InlineData("{\"status\":404,\"code\":\"not_found\",\"instance\":\"/api/v1/files\"}", false)]
        [InlineData("{\"status\":404,\"title\":\"Not Found\"}", false)]
        [InlineData("{\"status\":\"404\",\"code\":\"not_found\",\"instance\":\"/api/v1/auth/refresh\"}", false)]
        [InlineData("{broken", false)]
        [InlineData("404 page not found", false)]
        [InlineData("null", false)]
        [InlineData("[]", false)]
        public void OnlyConfirmedRefreshRejectionRequiresSignIn(string body, bool expected)
        {
            CottonTokenRefreshException failure = new(new CottonApiException(HttpStatusCode.NotFound, body, "refresh failed"));
            Assert.Equal(expected, CottonRefreshTokenRejection.IsConfirmed(failure));
            Assert.Equal(expected, CottonAutomaticSyncFailureClassifier.Classify(failure) == CottonAutomaticSyncFailureKind.AuthenticationRequired);
        }
    }
}
