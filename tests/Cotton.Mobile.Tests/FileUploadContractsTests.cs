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
            CottonFileUploadSettings settings = new CottonFileUploadSettings(CottonFileUploadSettings.MinimumChunkSizeBytes, algorithm);

            Assert.Equal(CottonFileUploadSettings.MinimumChunkSizeBytes, settings.MaxChunkSizeBytes);
            Assert.Equal(CottonFileUploadSettings.SupportedSha256Algorithm, settings.SupportedHashAlgorithm);
        }

        [Fact]
        public void Upload_settings_reject_invalid_server_contracts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CottonFileUploadSettings(CottonFileUploadSettings.MinimumChunkSizeBytes - 1, "SHA256"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CottonFileUploadSettings(CottonFileUploadSettings.MaximumChunkSizeBytes + 1L, "SHA256"));
            Assert.Throws<NotSupportedException>(() =>
                new CottonFileUploadSettings(CottonFileUploadSettings.MinimumChunkSizeBytes, "SHA1"));
        }

        [Fact]
        public void Upload_source_snapshot_normalizes_user_selected_file_metadata()
        {
            CottonFileUploadSourceSnapshot snapshot = new CottonFileUploadSourceSnapshot(
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
            CottonFileUploadSourceSnapshot snapshot = new CottonFileUploadSourceSnapshot(" ", " ", null);

            Assert.Equal(CottonFileUploadSourceSnapshot.DefaultFileName, snapshot.Name);
            Assert.Equal(CottonFileUploadSourceSnapshot.DefaultContentType, snapshot.ContentType);
            Assert.Null(snapshot.SizeBytes);
            Assert.Empty(snapshot.Metadata);
        }

        [Fact]
        public void Upload_source_snapshot_normalizes_optional_metadata()
        {
            CottonFileUploadSourceSnapshot snapshot = new CottonFileUploadSourceSnapshot(
                "photo.jpg",
                "image/jpeg",
                200,
                new Dictionary<string, string>
                {
                    [$" {CottonFileUploadMetadataKeys.Source} "] = " picked-photo ",
                    [CottonFileUploadMetadataKeys.OriginalLastModifiedUtc] = " 2026-06-19T10:00:00.0000000Z ",
                    [" "] = "ignored",
                    ["ignored"] = " ",
                });

            Assert.Equal("picked-photo", snapshot.Metadata[CottonFileUploadMetadataKeys.Source]);
            Assert.Equal(
                "2026-06-19T10:00:00.0000000Z",
                snapshot.Metadata[CottonFileUploadMetadataKeys.OriginalLastModifiedUtc]);
            Assert.DoesNotContain("ignored", snapshot.Metadata.Keys);
        }

        [Fact]
        public void Upload_source_snapshot_keeps_metadata_when_renamed()
        {
            CottonFileUploadSourceSnapshot snapshot = new CottonFileUploadSourceSnapshot(
                "photo.jpg",
                "image/jpeg",
                200,
                new Dictionary<string, string>
                {
                    [CottonFileUploadMetadataKeys.Source] = "picked-photo",
                });

            CottonFileUploadSourceSnapshot renamed = snapshot.WithName("photo (1).jpg");

            Assert.Equal("photo (1).jpg", renamed.Name);
            Assert.Equal("picked-photo", renamed.Metadata[CottonFileUploadMetadataKeys.Source]);
        }

        [Fact]
        public void Upload_source_snapshot_rejects_negative_size()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CottonFileUploadSourceSnapshot("notes.txt", "text/plain", -1));
        }

        [Fact]
        public void Upload_hash_uses_lowercase_sha256_hex()
        {
            string hash = CottonContentHash.ComputeSha256("abc"u8);

            Assert.Equal(
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                hash);
        }

        [Fact]
        public async Task Upload_hash_streaming_uses_lowercase_sha256_hex()
        {
            await using MemoryStream content = new MemoryStream("abc"u8.ToArray());

            string hash = await CottonContentHash.ComputeSha256Async(content);

            Assert.Equal(
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                hash);
        }

        [Fact]
        public void Sync_working_file_names_are_reserved_for_temporary_state()
        {
            string temporary = CottonSyncWorkingFileName.CreateTemporary("report.pdf");
            string backup = CottonSyncWorkingFileName.CreateBackup("report.pdf");

            Assert.True(CottonSyncWorkingFileName.IsWorkingFile(temporary));
            Assert.True(CottonSyncWorkingFileName.IsWorkingFile(backup));
            Assert.False(CottonSyncWorkingFileName.IsWorkingFile("report.pdf"));
            Assert.False(CottonSyncWorkingFileName.IsWorkingFile("notes.cotton-sync-tmp"));
            Assert.False(CottonSyncWorkingFileName.IsWorkingFile("notes.cotton-sync-backup"));
            Assert.False(CottonSyncWorkingFileName.IsWorkingFile(
                "notes.not-a-generated-identifier.cotton-sync-tmp"));
        }

        [Fact]
        public void Upload_destination_snapshot_normalizes_folder_copy()
        {
            Guid folderId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            CottonUploadDestinationSnapshot destination = new CottonUploadDestinationSnapshot(
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
