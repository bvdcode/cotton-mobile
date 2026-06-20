using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FolderBulkOfflineStatusTextTests
    {
        [Fact]
        public void Folder_bulk_offline_status_text_uses_singular_and_plural_copy()
        {
            Assert.Equal(
                "Checking folder for offline use...",
                CottonFolderBulkOfflineStatusText.CreateStartingStatus(1));
            Assert.Equal(
                "Checking 3 folders for offline use...",
                CottonFolderBulkOfflineStatusText.CreateStartingStatus(3));
            Assert.Equal(
                "Checking 2 of 3 folders: Projects...",
                CottonFolderBulkOfflineStatusText.CreateCheckingFolderStatus(2, 3, " Projects "));
        }

        [Fact]
        public void Folder_bulk_offline_status_text_reports_terminal_policy_states()
        {
            Assert.Equal(
                "Selection is no longer available.",
                CottonFolderBulkOfflineStatusText.UnavailableStatus);
            Assert.Equal(
                "Select only files or only folders for this action.",
                CottonFolderBulkOfflineStatusText.MixedSelectionUnavailableStatus);
        }

        [Fact]
        public void Folder_bulk_offline_status_text_rejects_invalid_counts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFolderBulkOfflineStatusText.CreateStartingStatus(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFolderBulkOfflineStatusText.CreateCheckingFolderStatus(0, 1, "Projects"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonFolderBulkOfflineStatusText.CreateCheckingFolderStatus(2, 1, "Projects"));
        }
    }
}
