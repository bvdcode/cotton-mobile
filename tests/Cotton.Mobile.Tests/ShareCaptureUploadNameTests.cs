using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ShareCaptureUploadNameTests
    {
        [Fact]
        public void Create_returns_default_text_upload_name_without_text_display_name()
        {
            CottonShareIntakeItemSnapshot item = CreateTextItem(uploadDisplayName: null);

            string uploadName = CottonShareCaptureUploadName.Create(item);

            Assert.Equal("Shared text.txt", uploadName);
        }

        [Fact]
        public void Create_appends_text_extension_to_renamed_text_share()
        {
            CottonShareIntakeItemSnapshot item = CreateTextItem("Team notes");

            string uploadName = CottonShareCaptureUploadName.Create(item);

            Assert.Equal("Team notes.txt", uploadName);
        }

        [Fact]
        public void TryNormalizeRename_rejects_text_duplicate_after_extension_normalization()
        {
            CottonShareIntakeItemSnapshot item = CreateTextItem(uploadDisplayName: null);

            bool result = CottonShareCaptureUploadName.TryNormalizeRename(
                item,
                "Team notes",
                ["Team notes.txt"],
                out string normalizedName,
                out string uploadName,
                out string errorMessage);

            Assert.False(result);
            Assert.Equal(string.Empty, normalizedName);
            Assert.Equal(string.Empty, uploadName);
            Assert.Equal(CottonShareCaptureUploadName.DuplicateErrorMessage, errorMessage);
        }

        [Fact]
        public void TryNormalizeRename_returns_final_text_upload_name()
        {
            CottonShareIntakeItemSnapshot item = CreateTextItem(uploadDisplayName: null);

            bool result = CottonShareCaptureUploadName.TryNormalizeRename(
                item,
                "Daily notes",
                [],
                out string normalizedName,
                out string uploadName,
                out string errorMessage);

            Assert.True(result);
            Assert.Equal("Daily notes", normalizedName);
            Assert.Equal("Daily notes.txt", uploadName);
            Assert.Equal(string.Empty, errorMessage);
        }

        [Fact]
        public void TryNormalizeRename_keeps_staged_file_upload_name_exact()
        {
            CottonShareIntakeItemSnapshot item = CreateStagedItem();

            bool result = CottonShareCaptureUploadName.TryNormalizeRename(
                item,
                "Photo archive.jpg",
                [],
                out string normalizedName,
                out string uploadName,
                out string errorMessage);

            Assert.True(result);
            Assert.Equal("Photo archive.jpg", normalizedName);
            Assert.Equal("Photo archive.jpg", uploadName);
            Assert.Equal(string.Empty, errorMessage);
        }

        private static CottonShareIntakeItemSnapshot CreateTextItem(string? uploadDisplayName)
        {
            CottonShareIntakeItemSnapshot item = new(
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                CottonShareIntakeItemType.Text,
                "shared text body",
                displayName: null,
                mimeType: "text/plain");
            return uploadDisplayName is null ? item : item.WithUploadDisplayName(uploadDisplayName);
        }

        private static CottonShareIntakeItemSnapshot CreateStagedItem()
        {
            Guid itemId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
            CottonShareStagedContentSnapshot stagedContent = new(
                Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000"),
                itemId,
                "photo.jpg",
                "/tmp/photo.jpg",
                2048);
            return new CottonShareIntakeItemSnapshot(
                    itemId,
                    CottonShareIntakeItemType.Uri,
                    "content://media/photo",
                    "photo.jpg",
                    "image/jpeg")
                .WithStagedContent(stagedContent);
        }
    }
}
