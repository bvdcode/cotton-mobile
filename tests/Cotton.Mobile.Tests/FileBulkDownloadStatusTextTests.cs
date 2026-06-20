using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileBulkDownloadStatusTextTests
    {
        [Fact]
        public void Batch_download_copy_uses_file_counts_and_positions()
        {
            Assert.Equal("Downloading file...", CottonFileBulkDownloadStatusText.CreateStartingStatus(1));
            Assert.Equal("Downloading 3 files...", CottonFileBulkDownloadStatusText.CreateStartingStatus(3));
            Assert.Equal(
                "Downloading 2 of 3: report.pdf...",
                CottonFileBulkDownloadStatusText.CreateDownloadingItemStatus(2, 3, " report.pdf "));
            Assert.Equal(
                "Downloading 2 of 3: report.pdf... 75%",
                CottonFileBulkDownloadStatusText.CreateDownloadingItemProgressStatus(2, 3, "report.pdf", 75));
            Assert.Equal("3 files downloaded.", CottonFileBulkDownloadStatusText.CreateCompletedStatus(3));
        }

        [Fact]
        public void Batch_download_cancel_and_failure_copy_preserve_partial_progress()
        {
            Assert.Equal(
                "Download cancelled after 1/3 files.",
                CottonFileBulkDownloadStatusText.CreateCancelledStatus(1, 3));
            Assert.Equal(
                "Download failed after 2/4 files.",
                CottonFileBulkDownloadStatusText.CreateFailedStatus(2, 4));
        }

        [Fact]
        public void Batch_download_copy_clamps_percent_and_uses_file_fallback()
        {
            Assert.Equal(
                "Downloading file... 100%",
                CottonFileBulkDownloadStatusText.CreateDownloadingItemProgressStatus(1, 1, " ", 140));
            Assert.Equal(
                "Downloading file... 0%",
                CottonFileBulkDownloadStatusText.CreateDownloadingItemProgressStatus(1, 1, "file", -10));
        }
    }
}
