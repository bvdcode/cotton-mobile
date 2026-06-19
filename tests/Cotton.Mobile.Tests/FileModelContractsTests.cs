using Cotton.Files;
using Cotton.Mobile.Services;
using Cotton.Nodes;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileModelContractsTests
    {
        private static readonly DateTime UpdatedAt = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData("notes.txt", "text/plain; charset=utf-8", "Text", "TXT", true, false)]
        [InlineData("data.json", "application/json", "Text", "TXT", true, false)]
        [InlineData("diagram.svg", "", "Text", "TXT", true, false)]
        [InlineData("photo.webp", "image/webp", "Image", "IMG", false, true)]
        [InlineData("report.pdf", "", "PDF", "PDF", false, false)]
        [InlineData("brief.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Document", "DOC", false, false)]
        [InlineData("notes.rtf", "", "Document", "DOC", false, false)]
        [InlineData("movie.mp4", "video/mp4", "Video", "VID", false, false)]
        [InlineData("song.mp3", "audio/mpeg", "Audio", "AUD", false, false)]
        [InlineData("archive.zip", "application/zip", "File", "FILE", false, false)]
        public void From_file_classifies_supported_file_kinds(
            string name,
            string contentType,
            string expectedKind,
            string expectedBadge,
            bool expectedText,
            bool expectedImage)
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntry.FromFile(
                CreateFile(name, contentType, sizeBytes: 1536));

            Assert.Equal(CottonFileBrowserEntryType.File, entry.Type);
            Assert.Equal(name, entry.Name);
            Assert.Equal(expectedKind, entry.Kind);
            Assert.Equal(expectedBadge, entry.BadgeText);
            Assert.Equal($"1.5 KB · {expectedKind}", entry.Details);
            Assert.Equal(UpdatedAt, entry.UpdatedAtUtc);
            Assert.Equal(1536, entry.SizeBytes);
            Assert.Equal(expectedText, entry.IsText);
            Assert.Equal(expectedImage, entry.IsImage);
            Assert.False(entry.HasLocalCopy);
            Assert.True(entry.Thumbnail.IsPlaceholderVisible);
        }

        [Fact]
        public void From_file_normalizes_content_type_and_preview_hash()
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntry.FromFile(
                CreateFile(" photo.png ", " image/png ", sizeBytes: 100, previewHashEncryptedHex: " abc123 "));

            Assert.Equal("photo.png", entry.Name);
            Assert.Equal("image/png", entry.ContentType);
            Assert.Equal("abc123", entry.PreviewHashEncryptedHex);
            Assert.Equal("Image", entry.Kind);
        }

        [Fact]
        public void From_node_creates_folder_entry_with_open_action()
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntry.FromNode(
                new NodeDto
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = " Projects ",
                    UpdatedAt = UpdatedAt,
                });

            Assert.True(entry.IsFolder);
            Assert.Equal("Projects", entry.Name);
            Assert.Equal("Folder", entry.Kind);
            Assert.Equal("Folder", entry.Details);
            Assert.Equal("Open", entry.ActionLabel);
            Assert.Equal(UpdatedAt, entry.UpdatedAtUtc);
            Assert.True(entry.IsFolderThumbnailVisible);
        }

        [Fact]
        public void Local_file_snapshot_marks_and_clears_entry_without_changing_identity()
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntry.FromFile(CreateFile("notes.txt", "text/plain", 42));
            var localFile = new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt);

            CottonFileBrowserEntry marked = entry.WithLocalFile(localFile);
            CottonFileBrowserEntry cleared = marked.WithoutLocalFile();

            Assert.Equal(entry.Id, marked.Id);
            Assert.True(marked.HasLocalCopy);
            Assert.Equal("42 B · Text · On device", marked.DisplayDetails);
            Assert.Same(localFile, marked.LocalFile);
            Assert.False(cleared.HasLocalCopy);
            Assert.Equal(entry.Id, cleared.Id);
            Assert.Equal("42 B · Text", cleared.DisplayDetails);
        }

        [Fact]
        public void Thumbnail_snapshots_expose_stable_display_flags()
        {
            CottonFileThumbnailSnapshot placeholder = CottonFileThumbnailSnapshot.Placeholder("PDF", "cache-key");
            CottonFileThumbnailSnapshot loading = CottonFileThumbnailSnapshot.Loading("IMG", "cache-key");
            CottonFileThumbnailSnapshot ready = CottonFileThumbnailSnapshot.Ready("IMG", "file:///tmp/preview.webp", "cache-key");
            CottonFileThumbnailSnapshot failed = CottonFileThumbnailSnapshot.Failed("TXT", "cache-key");

            Assert.True(placeholder.IsPlaceholderVisible);
            Assert.False(placeholder.HasImage);
            Assert.False(placeholder.IsLoading);
            Assert.Equal(10d, placeholder.PlaceholderFontSize);

            Assert.True(loading.IsLoading);
            Assert.False(loading.IsPlaceholderVisible);

            Assert.True(ready.HasImage);
            Assert.False(ready.IsPlaceholderVisible);
            Assert.Equal("file:///tmp/preview.webp", ready.Source);

            Assert.True(failed.IsPlaceholderVisible);
            Assert.Equal("TXT", failed.PlaceholderText);
        }

        [Fact]
        public void Thumbnail_ready_requires_source_and_cache_key()
        {
            Assert.Throws<ArgumentException>(() => CottonFileThumbnailSnapshot.Ready("IMG", "", "cache-key"));
            Assert.Throws<ArgumentException>(() => CottonFileThumbnailSnapshot.Placeholder("IMG", ""));
        }

        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(1023, "1023 B")]
        [InlineData(1024, "1 KB")]
        [InlineData(1536, "1.5 KB")]
        [InlineData(1048576, "1 MB")]
        [InlineData(1610612736, "1.5 GB")]
        public void File_size_formatter_uses_binary_units(long sizeBytes, string expected)
        {
            Assert.Equal(expected, CottonFileSizeFormatter.Format(sizeBytes));
        }

        [Fact]
        public void Local_file_freshness_normalizes_time_and_allows_small_timestamp_drift()
        {
            DateTime remote = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);
            DateTime localWithinTolerance = new(2026, 6, 18, 11, 59, 59, DateTimeKind.Unspecified);
            DateTime localTooOld = new(2026, 6, 18, 11, 59, 57, DateTimeKind.Utc);

            Assert.True(CottonLocalFileFreshness.IsFresh(localWithinTolerance, remote));
            Assert.False(CottonLocalFileFreshness.IsFresh(localTooOld, remote));
            Assert.Equal(DateTimeKind.Utc, CottonLocalFileFreshness.NormalizeUtc(localWithinTolerance).Kind);
        }

        [Fact]
        public void Local_file_snapshot_requires_file_name()
        {
            Assert.Throws<ArgumentException>(() => new CottonLocalFileSnapshot(" ", 1, UpdatedAt));
        }

        private static NodeFileManifestDto CreateFile(
            string name,
            string contentType,
            long sizeBytes,
            string? previewHashEncryptedHex = null)
        {
            return new NodeFileManifestDto
            {
                Id = Guid.NewGuid(),
                Name = name,
                ContentType = contentType,
                SizeBytes = sizeBytes,
                PreviewHashEncryptedHex = previewHashEncryptedHex ?? string.Empty,
                UpdatedAt = UpdatedAt,
            };
        }
    }
}
