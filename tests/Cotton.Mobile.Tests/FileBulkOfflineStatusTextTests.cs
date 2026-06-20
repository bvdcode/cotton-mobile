using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileBulkOfflineStatusTextTests
    {
        [Fact]
        public void Bulk_offline_status_text_uses_singular_and_plural_copy()
        {
            Assert.Equal("Keeping file offline...", CottonFileBulkOfflineStatusText.CreateStartingStatus(1));
            Assert.Equal("Keeping 3 files offline...", CottonFileBulkOfflineStatusText.CreateStartingStatus(3));
            Assert.Equal(
                "Keeping 2 of 3 offline: report.pdf...",
                CottonFileBulkOfflineStatusText.CreateSavingItemStatus(2, 3, " report.pdf "));
            Assert.Equal(
                "Keeping 2 of 3 offline: report.pdf... 75%",
                CottonFileBulkOfflineStatusText.CreateSavingItemProgressStatus(2, 3, "report.pdf", 75));
            Assert.Equal("1 file available offline.", CottonFileBulkOfflineStatusText.CreateCompletedStatus(1));
            Assert.Equal("3 files available offline.", CottonFileBulkOfflineStatusText.CreateCompletedStatus(3));
        }

        [Fact]
        public void Bulk_offline_status_text_reports_cancelled_and_failed_progress()
        {
            Assert.Equal(
                "Keep offline cancelled after 1/3 files.",
                CottonFileBulkOfflineStatusText.CreateCancelledStatus(1, 3));
            Assert.Equal(
                "Keep offline failed after 2/4 files.",
                CottonFileBulkOfflineStatusText.CreateFailedStatus(2, 4));
            Assert.Equal("Offline. Keep offline needs internet.", CottonFileBulkOfflineStatusText.OfflineUnavailableStatus);
            Assert.Equal("Selection is no longer available.", CottonFileBulkOfflineStatusText.UnavailableStatus);
        }

        [Fact]
        public void Bulk_offline_status_text_rejects_invalid_counts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkOfflineStatusText.CreateStartingStatus(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkOfflineStatusText.CreateSavingItemStatus(0, 1, "file"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkOfflineStatusText.CreateFailedStatus(2, 1));
        }
    }
}
