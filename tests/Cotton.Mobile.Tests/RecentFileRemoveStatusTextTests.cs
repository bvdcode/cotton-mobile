using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RecentFileRemoveStatusTextTests
    {
        [Fact]
        public void Removing_status_includes_trimmed_file_name()
        {
            Assert.Equal(
                "Removing report.pdf...",
                CottonRecentFileRemoveStatusText.CreateRemovingStatus(" report.pdf "));
        }

        [Fact]
        public void Removed_status_includes_trimmed_file_name()
        {
            Assert.Equal(
                "Removed report.pdf from Recent files.",
                CottonRecentFileRemoveStatusText.CreateRemovedStatus(" report.pdf "));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Status_rejects_blank_file_name(string fileName)
        {
            Assert.Throws<ArgumentException>(() =>
                CottonRecentFileRemoveStatusText.CreateRemovingStatus(fileName));
            Assert.Throws<ArgumentException>(() =>
                CottonRecentFileRemoveStatusText.CreateRemovedStatus(fileName));
        }

        [Fact]
        public void Stable_failure_statuses_are_explicit()
        {
            Assert.Equal(
                "Recent file was already removed.",
                CottonRecentFileRemoveStatusText.AlreadyRemovedStatus);
            Assert.Equal(
                "Could not remove recent file.",
                CottonRecentFileRemoveStatusText.FailedStatus);
        }
    }
}
