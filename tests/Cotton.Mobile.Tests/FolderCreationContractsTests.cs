using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FolderCreationContractsTests
    {
        [Fact]
        public void Add_action_sheet_keeps_primary_cloud_actions_ordered()
        {
            Assert.Equal(
                [
                    CottonFileAddActionSheet.NewFolderAction,
                    CottonFileAddActionSheet.UploadFileAction,
                    CottonFileAddActionSheet.ScanDocumentAction,
                    CottonFileAddActionSheet.UploadPhotoAction,
                    CottonFileAddActionSheet.UploadVideoAction,
                ],
                CottonFileAddActionSheet.CreateActions());
        }

        [Fact]
        public void Add_action_sheet_hides_scan_when_document_scanner_is_unavailable()
        {
            Assert.Equal(
                [
                    CottonFileAddActionSheet.NewFolderAction,
                    CottonFileAddActionSheet.UploadFileAction,
                    CottonFileAddActionSheet.UploadPhotoAction,
                    CottonFileAddActionSheet.UploadVideoAction,
                ],
                CottonFileAddActionSheet.CreateActions(canScanDocument: false));
        }

        [Fact]
        public void Add_action_sheet_title_names_current_folder()
        {
            var folder = new CottonFolderHandle(Guid.NewGuid(), " Camera ");

            Assert.Equal("Add to Camera", CottonFileAddActionSheet.CreateTitle(folder));
        }

        [Fact]
        public void Folder_name_validator_normalizes_safe_name()
        {
            bool valid = CottonFolderNameValidator.TryNormalize(
                " Camera Uploads ",
                ["Photos", "Videos"],
                out string normalizedName,
                out string errorMessage);

            Assert.True(valid);
            Assert.Equal("Camera Uploads", normalizedName);
            Assert.Equal(string.Empty, errorMessage);
        }

        [Theory]
        [InlineData(null, "Enter a folder name.")]
        [InlineData("", "Enter a folder name.")]
        [InlineData("  ", "Enter a folder name.")]
        [InlineData(".", "Use a folder name, not a path.")]
        [InlineData("..", "Use a folder name, not a path.")]
        [InlineData("Camera/Uploads", "Folder names cannot contain path separators or reserved characters.")]
        [InlineData("Camera\\Uploads", "Folder names cannot contain path separators or reserved characters.")]
        [InlineData("Camera:Uploads", "Folder names cannot contain path separators or reserved characters.")]
        public void Folder_name_validator_rejects_unsafe_names(string? value, string expectedError)
        {
            bool valid = CottonFolderNameValidator.TryNormalize(
                value,
                [],
                out string normalizedName,
                out string errorMessage);

            Assert.False(valid);
            Assert.Equal(string.Empty, normalizedName);
            Assert.Equal(expectedError, errorMessage);
        }

        [Fact]
        public void Folder_name_validator_rejects_existing_item_name()
        {
            bool valid = CottonFolderNameValidator.TryNormalize(
                "photos",
                [" Photos "],
                out string normalizedName,
                out string errorMessage);

            Assert.False(valid);
            Assert.Equal(string.Empty, normalizedName);
            Assert.Equal(CottonFolderCreationStatusText.DuplicateStatus, errorMessage);
        }

        [Fact]
        public void Folder_creation_status_text_names_action_result()
        {
            Assert.Equal("Creating Camera...", CottonFolderCreationStatusText.CreateCreatingStatus(" Camera "));
            Assert.Equal("Created folder Camera.", CottonFolderCreationStatusText.CreateCreatedStatus(" Camera "));
            Assert.Equal("Offline. New folder needs internet.", CottonFolderCreationStatusText.OfflineStatus);
        }
    }
}
