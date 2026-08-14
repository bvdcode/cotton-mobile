using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AutomaticSyncStatusTextTests
    {
        private static readonly DateTime AttemptTime =
            new(2026, 8, 14, 18, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void EmptyHistoryHasNoStatusText()
        {
            Assert.Null(CottonAutomaticSyncStatusText.Create([]));
        }

        [Fact]
        public void FailureRemainsVisibleWhenAnotherRootSucceeded()
        {
            string? text = CottonAutomaticSyncStatusText.Create(
                [
                    CreateStatus(
                        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        CottonAutomaticSyncOutcome.Failed),
                    CreateStatus(
                        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                        CottonAutomaticSyncOutcome.Succeeded),
                ]);

            Assert.NotNull(text);
            Assert.Contains("failed", text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SuccessfulRetryClearsFailureText()
        {
            string? text = CottonAutomaticSyncStatusText.Create(
                [
                    CreateStatus(
                        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        CottonAutomaticSyncOutcome.Succeeded),
                ]);

            Assert.NotNull(text);
            Assert.Contains("Last synced", text, StringComparison.Ordinal);
            Assert.DoesNotContain("failed", text, StringComparison.OrdinalIgnoreCase);
        }

        private static CottonAutomaticSyncRootStatusSnapshot CreateStatus(
            Guid rootId,
            CottonAutomaticSyncOutcome outcome)
        {
            return new CottonAutomaticSyncRootStatusSnapshot(rootId, outcome, AttemptTime);
        }
    }
}
