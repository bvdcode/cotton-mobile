using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileUploadContractsTests
    {
        [Theory]
        [InlineData("SHA256")]
        [InlineData(" sha256 ")]
        public void UploadSettingsAcceptSupportedSha256Algorithm(string algorithm)
        {
            CottonFileUploadSettings settings = new(CottonFileUploadSettings.MinimumChunkSizeBytes, algorithm);

            Assert.Equal(CottonFileUploadSettings.MinimumChunkSizeBytes, settings.MaxChunkSizeBytes);
            Assert.Equal(CottonFileUploadSettings.SupportedSha256Algorithm, settings.SupportedHashAlgorithm);
        }

        [Fact]
        public void UploadSettingsRejectInvalidServerContracts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CottonFileUploadSettings(CottonFileUploadSettings.MinimumChunkSizeBytes - 1, "SHA256"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CottonFileUploadSettings(CottonFileUploadSettings.MaximumChunkSizeBytes + 1L, "SHA256"));
            Assert.Throws<NotSupportedException>(() =>
                new CottonFileUploadSettings(CottonFileUploadSettings.MinimumChunkSizeBytes, "SHA1"));
        }

        [Fact]
        public void UploadSourceSnapshotNormalizesUserSelectedFileMetadata()
        {
            CottonFileUploadSourceSnapshot snapshot = new(
                " /tmp/report.pdf ",
                " application/pdf ",
                1536);

            Assert.Equal("report.pdf", snapshot.Name);
            Assert.Equal("application/pdf", snapshot.ContentType);
            Assert.Equal(1536, snapshot.SizeBytes);
        }

        [Fact]
        public void UploadSourceSnapshotUsesExplicitSafeDefaults()
        {
            CottonFileUploadSourceSnapshot snapshot = new(" ", " ", null);

            Assert.Equal(CottonFileUploadSourceSnapshot.DefaultFileName, snapshot.Name);
            Assert.Equal(CottonFileUploadSourceSnapshot.DefaultContentType, snapshot.ContentType);
            Assert.Null(snapshot.SizeBytes);
            Assert.Empty(snapshot.Metadata);
        }

        [Fact]
        public void UploadSourceSnapshotNormalizesOptionalMetadata()
        {
            CottonFileUploadSourceSnapshot snapshot = new(
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
        public void UploadSourceSnapshotKeepsMetadataWhenRenamed()
        {
            CottonFileUploadSourceSnapshot snapshot = new(
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
        public void UploadSourceSnapshotRejectsNegativeSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CottonFileUploadSourceSnapshot("notes.txt", "text/plain", -1));
        }

        [Fact]
        public void UploadHashUsesLowercaseSha256Hex()
        {
            string hash = CottonContentHash.ComputeSha256("abc"u8);

            Assert.Equal(
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                hash);
        }

        [Fact]
        public async Task UploadHashStreamingUsesLowercaseSha256Hex()
        {
            await using MemoryStream content = new("abc"u8.ToArray());

            string hash = await CottonContentHash.ComputeSha256Async(content);

            Assert.Equal(
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                hash);
        }

        [Fact]
        public void SyncWorkingFileNamesAreReservedForTemporaryState()
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
        public void UploadDestinationSnapshotNormalizesFolderCopy()
        {
            Guid folderId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            CottonUploadDestinationSnapshot destination = new(
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
