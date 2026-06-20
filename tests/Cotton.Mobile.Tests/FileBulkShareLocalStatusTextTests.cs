using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileBulkShareLocalStatusTextTests
    {
        [Fact]
        public void Bulk_share_local_status_text_uses_singular_and_plural_copy()
        {
            Assert.Equal("Preparing file...", CottonFileBulkShareLocalStatusText.CreatePreparingStatus(1));
            Assert.Equal("Preparing 2 files...", CottonFileBulkShareLocalStatusText.CreatePreparingStatus(2));
            Assert.Equal("Sharing file...", CottonFileBulkShareLocalStatusText.CreateSharingStatus(1));
            Assert.Equal("Sharing 3 files...", CottonFileBulkShareLocalStatusText.CreateSharingStatus(3));
        }

        [Fact]
        public void Bulk_share_local_status_text_reports_terminal_states()
        {
            Assert.Equal("Selection is no longer available.", CottonFileBulkShareLocalStatusText.UnavailableStatus);
            Assert.Equal("Download files before sharing them.", CottonFileBulkShareLocalStatusText.LocalFilesUnavailableStatus);
            Assert.Equal("Share cancelled.", CottonFileBulkShareLocalStatusText.CancelledStatus);
            Assert.Equal("Share failed.", CottonFileBulkShareLocalStatusText.FailedStatus);
        }

        [Fact]
        public void Bulk_share_local_status_text_rejects_invalid_counts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkShareLocalStatusText.CreatePreparingStatus(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkShareLocalStatusText.CreateSharingStatus(-1));
        }
    }
}
