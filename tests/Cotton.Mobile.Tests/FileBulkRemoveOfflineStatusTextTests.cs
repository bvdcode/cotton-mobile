using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileBulkRemoveOfflineStatusTextTests
    {
        [Fact]
        public void Bulk_remove_offline_status_text_uses_singular_and_plural_copy()
        {
            Assert.Equal("Removing offline file...", CottonFileBulkRemoveOfflineStatusText.CreateStartingStatus(1));
            Assert.Equal("Removing 3 offline files...", CottonFileBulkRemoveOfflineStatusText.CreateStartingStatus(3));
            Assert.Equal(
                "Removing 2 of 3 from this device: report.pdf...",
                CottonFileBulkRemoveOfflineStatusText.CreateRemovingItemStatus(2, 3, " report.pdf "));
            Assert.Equal("1 file removed from this device.", CottonFileBulkRemoveOfflineStatusText.CreateCompletedStatus(1));
            Assert.Equal("3 files removed from this device.", CottonFileBulkRemoveOfflineStatusText.CreateCompletedStatus(3));
        }

        [Fact]
        public void Bulk_remove_offline_status_text_reports_cancelled_and_failed_progress()
        {
            Assert.Equal(
                "Remove offline cancelled after 1/3 files.",
                CottonFileBulkRemoveOfflineStatusText.CreateCancelledStatus(1, 3));
            Assert.Equal(
                "Remove offline failed after 2/4 files.",
                CottonFileBulkRemoveOfflineStatusText.CreateFailedStatus(2, 4));
            Assert.Equal("Selection is no longer available.", CottonFileBulkRemoveOfflineStatusText.UnavailableStatus);
        }

        [Fact]
        public void Bulk_remove_offline_status_text_rejects_invalid_counts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkRemoveOfflineStatusText.CreateStartingStatus(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkRemoveOfflineStatusText.CreateRemovingItemStatus(0, 1, "file"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkRemoveOfflineStatusText.CreateFailedStatus(2, 1));
        }
    }
}
