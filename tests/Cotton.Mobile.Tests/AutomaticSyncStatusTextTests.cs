using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AutomaticSyncStatusTextTests
    {
        private static readonly DateTime AttemptTime =
            new(2026, 8, 14, 18, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void FailureStatusContainsAttemptTime()
        {
            CottonAutomaticSyncRootStatusSnapshot status = CottonAutomaticSyncRootStatusSnapshot.Failed(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                AttemptTime,
                CottonAutomaticSyncFailureKind.NetworkUnavailable);

            string text = CottonAutomaticSyncStatusText.Create(status);

            Assert.Contains("failed", text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SuccessfulStatusDoesNotContainFailure()
        {
            CottonAutomaticSyncRootStatusSnapshot status = CottonAutomaticSyncRootStatusSnapshot.Succeeded(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                AttemptTime);

            string text = CottonAutomaticSyncStatusText.Create(status);

            Assert.Contains("Last synced", text, StringComparison.Ordinal);
            Assert.DoesNotContain("failed", text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
