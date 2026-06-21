using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileBulkTrashStatusTextTests
    {
        [Fact]
        public void Bulk_trash_confirmation_copy_describes_selected_items()
        {
            Assert.Equal(
                "1 file will be removed from this folder and can be restored from trash.",
                CottonFileBulkTrashStatusText.CreateConfirmMessage(fileCount: 1, folderCount: 0));
            Assert.Equal(
                "1 folder will be removed from this folder and can be restored from trash.",
                CottonFileBulkTrashStatusText.CreateConfirmMessage(fileCount: 0, folderCount: 1));
            Assert.Equal(
                "2 files and 1 folder will be removed from this folder and can be restored from trash.",
                CottonFileBulkTrashStatusText.CreateConfirmMessage(fileCount: 2, folderCount: 1));
        }

        [Fact]
        public void Bulk_trash_status_text_reports_progress()
        {
            Assert.Equal("Moving item to trash...", CottonFileBulkTrashStatusText.CreateMovingStatus(1));
            Assert.Equal("Moving 3 items to trash...", CottonFileBulkTrashStatusText.CreateMovingStatus(3));
            Assert.Equal(
                "Moving 2 of 3 to trash: Archive...",
                CottonFileBulkTrashStatusText.CreateMovingItemStatus(2, 3, " Archive "));
            Assert.Equal("1 item moved to trash.", CottonFileBulkTrashStatusText.CreateMovedStatus(1));
            Assert.Equal("3 items moved to trash.", CottonFileBulkTrashStatusText.CreateMovedStatus(3));
            Assert.Equal(
                "Move to trash cancelled after 1/3 items.",
                CottonFileBulkTrashStatusText.CreateCancelledStatus(1, 3));
            Assert.Equal(
                "Move to trash failed after 2/4 items.",
                CottonFileBulkTrashStatusText.CreateFailedStatus(2, 4));
            Assert.Equal("Move to trash cancelled.", CottonFileBulkTrashStatusText.CancelledStatus);
            Assert.Equal("Could not move selection to trash.", CottonFileBulkTrashStatusText.FailedStatus);
            Assert.Equal(
                "Refresh this folder before moving selected files to trash.",
                CottonFileBulkTrashStatusText.NeedsRefreshStatus);
            Assert.Equal(
                "Offline. Move to trash needs internet.",
                CottonFileBulkTrashStatusText.OfflineUnavailableStatus);
        }

        [Fact]
        public void Bulk_trash_status_text_rejects_invalid_counts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkTrashStatusText.CreateConfirmMessage(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkTrashStatusText.CreateConfirmMessage(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkTrashStatusText.CreateMovingStatus(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkTrashStatusText.CreateMovingItemStatus(0, 1, "file"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkTrashStatusText.CreateCancelledStatus(2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFileBulkTrashStatusText.CreateFailedStatus(-1, 1));
        }
    }
}
