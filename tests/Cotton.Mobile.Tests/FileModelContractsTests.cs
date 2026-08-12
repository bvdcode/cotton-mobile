using Cotton.Files;
using Cotton.Mobile.Services;
using Cotton.Nodes;
using Xunit;
using static Cotton.Mobile.Tests.FileModelTestData;

namespace Cotton.Mobile.Tests
{
    public class FileModelContractsTests
    {
        [Theory]
        [InlineData("notes.txt", "text/plain; charset=utf-8", CottonFileKind.Text, "TXT", true, false)]
        [InlineData("data.json", "application/json", CottonFileKind.Text, "TXT", true, false)]
        [InlineData("Program.cs", "", CottonFileKind.Text, "TXT", true, false)]
        [InlineData("Dockerfile", "", CottonFileKind.Text, "TXT", true, false)]
        [InlineData("diagram.svg", "", CottonFileKind.Svg, "SVG", false, false)]
        [InlineData("icon.svg", "image/svg+xml; charset=utf-8", CottonFileKind.Svg, "SVG", false, false)]
        [InlineData("photo.webp", "image/webp", CottonFileKind.Image, "IMG", false, true)]
        [InlineData("report.pdf", "", CottonFileKind.Pdf, "PDF", false, false)]
        [InlineData("brief.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", CottonFileKind.Document, "DOC", false, false)]
        [InlineData("notes.rtf", "", CottonFileKind.Document, "DOC", false, false)]
        [InlineData("movie.mp4", "video/mp4", CottonFileKind.Video, "VID", false, false)]
        [InlineData("song.mp3", "audio/mpeg", CottonFileKind.Audio, "AUD", false, false)]
        [InlineData("archive.zip", "application/zip", CottonFileKind.File, "FILE", false, false)]
        public void From_file_classifies_supported_file_kinds(
            string name,
            string contentType,
            CottonFileKind expectedKind,
            string expectedBadge,
            bool expectedText,
            bool expectedImage)
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntryFactory.FromFile(
                CreateFile(name, contentType, sizeBytes: 1536));

            Assert.Equal(CottonFileBrowserEntryType.File, entry.Type);
            Assert.Equal(name, entry.Name);
            Assert.Equal(expectedKind, entry.Kind);
            Assert.Equal(expectedBadge, entry.BadgeText);
            Assert.Equal(
                $"1.5 KB · {CottonFileKindDisplayName.Create(expectedKind)}",
                entry.Details);
            Assert.Equal(UpdatedAt, entry.UpdatedAtUtc);
            Assert.Equal(1536, entry.SizeBytes);
            Assert.Equal(expectedText, entry.IsText);
            Assert.Equal(expectedImage, entry.IsImage);
            Assert.Equal(expectedKind == CottonFileKind.Svg, entry.IsSvg);
            Assert.False(entry.HasLocalCopy);
            Assert.True(entry.Thumbnail.IsPlaceholderVisible);
        }

        [Fact]
        public void From_file_normalizes_content_type_and_preview_hash()
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntryFactory.FromFile(
                CreateFile(
                    " photo.png ",
                    " image/png ",
                    sizeBytes: 100,
                    previewHashEncryptedHex: " abc123 ",
                    eTag: " \"file-revision\" "));

            Assert.Equal("photo.png", entry.Name);
            Assert.Equal("image/png", entry.ContentType);
            Assert.Equal("abc123", entry.PreviewHashEncryptedHex);
            Assert.Equal("\"file-revision\"", entry.ETag);
            Assert.Equal(CottonFileKind.Image, entry.Kind);
        }

        [Fact]
        public void From_file_preserves_an_immutable_metadata_snapshot()
        {
            Dictionary<string, string> sourceMetadata = new(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.UploadOperationId] = "operation-1",
            };
            CottonFileBrowserEntry entry = CottonFileBrowserEntryFactory.FromFile(
                CreateFile("photo.jpg", "image/jpeg", 42, metadata: sourceMetadata));

            sourceMetadata[CottonFileUploadMetadataKeys.UploadOperationId] = "changed";
            CottonFileBrowserEntry selected = entry.WithSelection(true);

            Assert.Equal(
                "operation-1",
                entry.Metadata[CottonFileUploadMetadataKeys.UploadOperationId]);
            Assert.Equal(entry.Metadata, selected.Metadata);
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, string>)entry.Metadata).Add("other", "value"));
        }

        [Fact]
        public void From_node_creates_folder_entry_with_open_action()
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntryFactory.FromNode(
                new NodeDto
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = " Projects ",
                    UpdatedAt = UpdatedAt,
                });

            Assert.True(entry.IsFolder);
            Assert.Equal("Projects", entry.Name);
            Assert.Equal(CottonFileKind.Folder, entry.Kind);
            Assert.Equal("Folder", entry.Details);
            Assert.Equal("Open", entry.ActionLabel);
            Assert.Equal(UpdatedAt, entry.UpdatedAtUtc);
            Assert.True(entry.IsFolderThumbnailVisible);
        }

        [Fact]
        public void Local_file_snapshot_marks_and_clears_entry_without_changing_identity()
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntryFactory.FromFile(CreateFile("notes.txt", "text/plain", 42));
            CottonLocalFileSnapshot localFile = new("notes.txt", 42, UpdatedAt);

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
        public void Selection_marker_preserves_file_identity_and_local_state()
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntryFactory.FromFile(CreateFile("notes.txt", "text/plain", 42));
            CottonLocalFileSnapshot localFile = new("notes.txt", 42, UpdatedAt);

            CottonFileBrowserEntry selected = entry.WithSelection(true).WithLocalFile(localFile);
            CottonFileBrowserEntry cleared = selected.WithSelection(false);

            Assert.Equal(entry.Id, selected.Id);
            Assert.True(selected.IsSelected);
            Assert.True(selected.HasLocalCopy);
            Assert.False(cleared.IsSelected);
            Assert.True(cleared.HasLocalCopy);
        }

        [Fact]
        public void Offline_file_availability_distinguishes_available_stale_and_missing_pins()
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntryFactory.FromFile(CreateFile("notes.txt", "text/plain", 42));
            CottonOfflineFilePinSnapshot pin = CottonOfflineFilePinSnapshot.Create(entry, UpdatedAt.AddMinutes(1));
            CottonLocalFileSnapshot freshLocal = new("notes.txt", 42, UpdatedAt);
            CottonLocalFileSnapshot staleTimeLocal = new("notes.txt", 42, UpdatedAt.AddSeconds(-3));
            CottonLocalFileSnapshot wrongSizeLocal = new("notes.txt", 41, UpdatedAt);

            CottonOfflineFileAvailabilitySnapshot available =
                CottonOfflineFileAvailabilitySnapshot.Create(entry, pin, freshLocal);
            CottonOfflineFileAvailabilitySnapshot staleTime =
                CottonOfflineFileAvailabilitySnapshot.Create(entry, pin, staleTimeLocal);
            CottonOfflineFileAvailabilitySnapshot staleSize =
                CottonOfflineFileAvailabilitySnapshot.Create(entry, pin, wrongSizeLocal);
            CottonOfflineFileAvailabilitySnapshot missing =
                CottonOfflineFileAvailabilitySnapshot.Create(entry, pin, localFile: null);
            CottonOfflineFileAvailabilitySnapshot notPinned =
                CottonOfflineFileAvailabilitySnapshot.Create(entry, pin: null, freshLocal);

            Assert.Equal(CottonOfflineFileAvailabilityStatus.Available, available.Status);
            Assert.True(available.IsAvailable);
            Assert.False(available.NeedsRefresh);
            Assert.Equal("On device", available.StatusText);
            Assert.Equal(CottonOfflineFileAvailabilityStatus.Stale, staleTime.Status);
            Assert.Equal(CottonOfflineFileAvailabilityStatus.Stale, staleSize.Status);
            Assert.True(staleTime.NeedsRefresh);
            Assert.Equal("Offline stale", staleTime.StatusText);
            Assert.Equal("Kept offline, refresh to match the cloud version.", staleTime.DetailsText);
            Assert.Equal(CottonOfflineFileAvailabilityStatus.Missing, missing.Status);
            Assert.True(missing.NeedsRefresh);
            Assert.Equal("Offline missing", missing.StatusText);
            Assert.Equal(CottonOfflineFileAvailabilityStatus.NotPinned, notPinned.Status);
            Assert.False(notPinned.IsPinned);
        }

        [Fact]
        public void File_entry_surfaces_offline_attention_without_overloading_on_device()
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntryFactory.FromFile(CreateFile("notes.txt", "text/plain", 42));
            CottonOfflineFilePinSnapshot pin = CottonOfflineFilePinSnapshot.Create(entry, UpdatedAt);
            CottonOfflineFileAvailabilitySnapshot stale =
                CottonOfflineFileAvailabilitySnapshot.Create(
                    entry,
                    pin,
                    new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt.AddSeconds(-3)));

            CottonFileBrowserEntry staleEntry = entry.WithOfflineAvailability(stale);
            CottonFileBrowserEntry freshEntry = staleEntry.WithLocalFile(new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt));

            Assert.False(staleEntry.HasLocalCopy);
            Assert.True(staleEntry.IsOfflineAttentionVisible);
            Assert.Equal("Offline stale", staleEntry.OfflineAttentionStatus);
            Assert.Equal("42 B · Text · Offline stale", staleEntry.DisplayDetails);
            Assert.True(freshEntry.HasLocalCopy);
            Assert.False(freshEntry.IsOfflineAttentionVisible);
            Assert.Equal("42 B · Text · On device", freshEntry.DisplayDetails);
        }
    }
}
