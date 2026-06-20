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
        [InlineData("Program.cs", "", "Text", "TXT", true, false)]
        [InlineData("Dockerfile", "", "Text", "TXT", true, false)]
        [InlineData("diagram.svg", "", "SVG", "SVG", false, false)]
        [InlineData("icon.svg", "image/svg+xml; charset=utf-8", "SVG", "SVG", false, false)]
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
            Assert.Equal(expectedKind == "SVG", entry.IsSvg);
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
        public void Offline_file_availability_distinguishes_available_stale_and_missing_pins()
        {
            CottonFileBrowserEntry entry = CottonFileBrowserEntry.FromFile(CreateFile("notes.txt", "text/plain", 42));
            CottonOfflineFilePinSnapshot pin = CottonOfflineFilePinSnapshot.Create(entry, UpdatedAt.AddMinutes(1));
            var freshLocal = new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt);
            var staleTimeLocal = new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt.AddSeconds(-3));
            var wrongSizeLocal = new CottonLocalFileSnapshot("notes.txt", 41, UpdatedAt);

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
            CottonFileBrowserEntry entry = CottonFileBrowserEntry.FromFile(CreateFile("notes.txt", "text/plain", 42));
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

        [Theory]
        [InlineData(".env", null, true)]
        [InlineData(".env.production", null, true)]
        [InlineData("service-account.pem", null, true)]
        [InlineData("vault.kdbx", null, true)]
        [InlineData("access-token.txt", null, true)]
        [InlineData("tokenizer.cs", null, false)]
        [InlineData("photo.jpg", "image/jpeg", false)]
        [InlineData("bundle", "application/x-pkcs12; charset=binary", true)]
        public void Sensitive_file_cache_policy_identifies_secret_material(
            string fileName,
            string? contentType,
            bool expectedSensitive)
        {
            Assert.Equal(
                expectedSensitive,
                CottonSensitiveFileCachePolicy.IsSensitiveFile(fileName, contentType));
        }

        [Fact]
        public void Sensitive_file_cache_policy_blocks_unpinned_reusable_local_copy()
        {
            CottonFileBrowserEntry sensitiveEntry = CottonFileBrowserEntry.FromFile(
                CreateFile("private-key.pem", "application/x-pem-file", 42));
            CottonFileBrowserEntry normalEntry = CottonFileBrowserEntry.FromFile(
                CreateFile("notes.txt", "text/plain", 42));

            Assert.False(CottonSensitiveFileCachePolicy.CanReuseUnpinnedLocalCopy(sensitiveEntry));
            Assert.True(CottonSensitiveFileCachePolicy.CanReuseUnpinnedLocalCopy(normalEntry));
        }

        [Fact]
        public void Local_file_snapshot_requires_file_name()
        {
            Assert.Throws<ArgumentException>(() => new CottonLocalFileSnapshot(" ", 1, UpdatedAt));
        }

        [Fact]
        public void Offline_file_status_text_keeps_user_copy_explicit()
        {
            Assert.Equal(
                "Keeping notes.txt offline...",
                CottonOfflineFileStatusText.CreateStartingStatus(" notes.txt "));
            Assert.Equal(
                "notes.txt is available offline.",
                CottonOfflineFileStatusText.CreateAvailableStatus("notes.txt"));
            Assert.Equal(
                "notes.txt removed from this device.",
                CottonOfflineFileStatusText.CreateRemovedStatus("notes.txt"));
            Assert.Equal(
                "notes.txt is not on this device.",
                CottonOfflineFileStatusText.CreateNotOnDeviceStatus("notes.txt"));
            Assert.Equal(
                "Offline. Keep offline needs internet.",
                CottonOfflineFileStatusText.OfflineUnavailableStatus);
            Assert.Equal("Keep offline cancelled.", CottonOfflineFileStatusText.CancelledStatus);
            Assert.Equal("Keep offline failed.", CottonOfflineFileStatusText.FailedStatus);
            Assert.Equal(
                "Refreshing notes.txt offline...",
                CottonOfflineFileStatusText.CreateRefreshingStatus("notes.txt"));
            Assert.Equal(
                "notes.txt offline copy refreshed.",
                CottonOfflineFileStatusText.CreateRefreshedStatus("notes.txt"));
            Assert.Equal("Offline. Refresh offline needs internet.", CottonOfflineFileStatusText.RefreshOfflineUnavailableStatus);
            Assert.Equal("Refresh offline cancelled.", CottonOfflineFileStatusText.RefreshCancelledStatus);
            Assert.Equal("Refresh offline failed.", CottonOfflineFileStatusText.RefreshFailedStatus);
            Assert.Equal("Remove offline cancelled.", CottonOfflineFileStatusText.RemoveCancelledStatus);
            Assert.Equal("Remove offline failed.", CottonOfflineFileStatusText.RemoveFailedStatus);
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
