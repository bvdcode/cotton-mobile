using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileUploadContractsTests
    {
        [Theory]
        [InlineData("SHA256")]
        [InlineData(" sha256 ")]
        public void Upload_settings_accept_supported_sha256_algorithm(string algorithm)
        {
            var settings = new CottonFileUploadSettings(1024, algorithm);

            Assert.Equal(1024, settings.MaxChunkSizeBytes);
            Assert.Equal(CottonFileUploadSettings.SupportedSha256Algorithm, settings.SupportedHashAlgorithm);
        }

        [Fact]
        public void Upload_settings_reject_invalid_server_contracts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CottonFileUploadSettings(0, "SHA256"));
            Assert.Throws<NotSupportedException>(() => new CottonFileUploadSettings(1024, "SHA1"));
        }

        [Fact]
        public void Upload_source_snapshot_normalizes_user_selected_file_metadata()
        {
            var snapshot = new CottonFileUploadSourceSnapshot(
                " /tmp/report.pdf ",
                " application/pdf ",
                1536);

            Assert.Equal("report.pdf", snapshot.Name);
            Assert.Equal("application/pdf", snapshot.ContentType);
            Assert.Equal(1536, snapshot.SizeBytes);
        }

        [Fact]
        public void Upload_source_snapshot_uses_explicit_safe_defaults()
        {
            var snapshot = new CottonFileUploadSourceSnapshot(" ", " ", null);

            Assert.Equal(CottonFileUploadSourceSnapshot.DefaultFileName, snapshot.Name);
            Assert.Equal(CottonFileUploadSourceSnapshot.DefaultContentType, snapshot.ContentType);
            Assert.Null(snapshot.SizeBytes);
            Assert.Empty(snapshot.Metadata);
        }

        [Fact]
        public void Upload_source_snapshot_normalizes_optional_metadata()
        {
            var snapshot = new CottonFileUploadSourceSnapshot(
                "photo.jpg",
                "image/jpeg",
                200,
                new Dictionary<string, string>
                {
                    [" cottonMobileSource "] = " picked-photo ",
                    ["cottonMobileOriginalLastModifiedUtc"] = " 2026-06-19T10:00:00.0000000Z ",
                    [" "] = "ignored",
                    ["ignored"] = " ",
                });

            Assert.Equal("picked-photo", snapshot.Metadata["cottonMobileSource"]);
            Assert.Equal("2026-06-19T10:00:00.0000000Z", snapshot.Metadata["cottonMobileOriginalLastModifiedUtc"]);
            Assert.DoesNotContain("ignored", snapshot.Metadata.Keys);
        }

        [Fact]
        public void Upload_source_snapshot_keeps_metadata_when_renamed()
        {
            var snapshot = new CottonFileUploadSourceSnapshot(
                "photo.jpg",
                "image/jpeg",
                200,
                new Dictionary<string, string>
                {
                    ["cottonMobileSource"] = "picked-photo",
                });

            CottonFileUploadSourceSnapshot renamed = snapshot.WithName("photo (1).jpg");

            Assert.Equal("photo (1).jpg", renamed.Name);
            Assert.Equal("picked-photo", renamed.Metadata["cottonMobileSource"]);
        }

        [Fact]
        public void Upload_source_snapshot_rejects_negative_size()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CottonFileUploadSourceSnapshot("notes.txt", "text/plain", -1));
        }

        [Fact]
        public void Upload_progress_reports_percent_for_known_size()
        {
            var source = new CottonFileUploadSourceSnapshot("notes.txt", "text/plain", 200);

            var progress = new CottonFileUploadProgressSnapshot(source, 125);

            Assert.Equal(62, progress.Percent);
            Assert.Equal("Uploading notes.txt... 62%", progress.StatusText);
        }

        [Fact]
        public void Upload_progress_reports_uploaded_bytes_without_known_size()
        {
            var source = new CottonFileUploadSourceSnapshot("photo.jpg", "image/jpeg", null);

            var progress = new CottonFileUploadProgressSnapshot(source, 1536);

            Assert.Null(progress.Percent);
            Assert.Equal("Uploading photo.jpg... 1.5 KB", progress.StatusText);
        }

        [Fact]
        public void Upload_progress_rejects_negative_uploaded_bytes()
        {
            var source = new CottonFileUploadSourceSnapshot("notes.txt", "text/plain", 200);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CottonFileUploadProgressSnapshot(source, -1));
        }

        [Fact]
        public void Upload_hash_uses_lowercase_sha256_hex()
        {
            string hash = CottonFileUploadHash.CreateSha256Hex("abc"u8);

            Assert.Equal(
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                hash);
        }

        [Fact]
        public void Upload_name_resolver_keeps_available_name()
        {
            string name = CottonFileUploadNameResolver.ResolveUniqueName(
                "photo.jpg",
                ["notes.txt"]);

            Assert.Equal("photo.jpg", name);
        }

        [Fact]
        public void Upload_name_resolver_adds_suffix_for_case_insensitive_duplicates()
        {
            string name = CottonFileUploadNameResolver.ResolveUniqueName(
                "photo.jpg",
                ["PHOTO.jpg", "photo (1).jpg"]);

            Assert.Equal("photo (2).jpg", name);
        }

        [Fact]
        public void Upload_name_resolver_handles_names_without_extension()
        {
            string name = CottonFileUploadNameResolver.ResolveUniqueName(
                "Screenshot",
                ["screenshot"]);

            Assert.Equal("Screenshot (1)", name);
        }

        [Fact]
        public void Upload_destination_snapshot_normalizes_folder_copy()
        {
            Guid folderId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var destination = new CottonUploadDestinationSnapshot(
                folderId,
                " Camera Uploads ",
                " Files / Camera Uploads ");

            CottonFolderHandle folder = destination.ToFolderHandle();

            Assert.Equal(folderId, destination.FolderId);
            Assert.Equal("Camera Uploads", destination.FolderName);
            Assert.Equal("Files / Camera Uploads", destination.Path);
            Assert.Equal(folderId, folder.Id);
            Assert.Equal("Camera Uploads", folder.Name);
        }
    }
}
