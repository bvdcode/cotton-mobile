using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncServerRevisionContractTests
    {
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 8, 30, 0, DateTimeKind.Utc);

        [Fact]
        public void File_with_etag_supports_expected_etag_mutation()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");

            CottonSyncServerRevisionSnapshot revision = CottonSyncServerRevisionContract.Create(file);

            Assert.Equal(CottonSyncServerRevisionStatus.FileETag, revision.Status);
            Assert.Equal("\"etag-1\"", revision.ETag);
            Assert.True(revision.HasServerRevisionToken);
            Assert.True(revision.SupportsExpectedETagMutation);
            Assert.Equal("File ETag available", revision.SummaryText);
        }

        [Fact]
        public void File_without_etag_requires_fresh_listing_before_conflict_safe_mutation()
        {
            CottonFileBrowserEntry file = CreateFile(eTag: null);

            CottonSyncServerRevisionSnapshot revision = CottonSyncServerRevisionContract.Create(file);

            Assert.Equal(CottonSyncServerRevisionStatus.FileMissingETag, revision.Status);
            Assert.Null(revision.ETag);
            Assert.False(revision.HasServerRevisionToken);
            Assert.False(revision.SupportsExpectedETagMutation);
        }

        [Fact]
        public void Folder_revision_is_explicitly_unsupported_by_current_sdk_contract()
        {
            CottonFileBrowserEntry folder = CottonFileBrowserEntry.CreateCached(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                CottonFileBrowserEntryType.Folder,
                "Projects",
                "Folder",
                "Folder",
                "Open",
                "Folder",
                UpdatedAt,
                sizeBytes: null,
                contentType: null,
                previewHashEncryptedHex: null,
                eTag: null);

            CottonSyncServerRevisionSnapshot revision = CottonSyncServerRevisionContract.Create(folder);

            Assert.Equal(CottonSyncServerRevisionStatus.FolderUnsupported, revision.Status);
            Assert.Null(revision.ETag);
            Assert.False(revision.HasServerRevisionToken);
            Assert.False(revision.SupportsExpectedETagMutation);
            Assert.Equal("Folder revision unavailable", revision.SummaryText);
        }

        [Fact]
        public void Entry_clones_preserve_file_etag()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");
            var localFile = new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt);

            CottonFileBrowserEntry withThumbnail = file.WithThumbnail(
                CottonFileThumbnailSnapshot.Placeholder("TXT", "cache-key"));
            CottonFileBrowserEntry withLocalFile = withThumbnail.WithLocalFile(localFile);
            CottonFileBrowserEntry withOfflineAvailability = withLocalFile.WithOfflineAvailability(
                CottonOfflineFileAvailabilitySnapshot.NotPinned);
            CottonFileBrowserEntry withoutLocalFile = withOfflineAvailability.WithoutLocalFile();

            Assert.Equal("\"etag-1\"", withThumbnail.ETag);
            Assert.Equal("\"etag-1\"", withLocalFile.ETag);
            Assert.Equal("\"etag-1\"", withOfflineAvailability.ETag);
            Assert.Equal("\"etag-1\"", withoutLocalFile.ETag);
        }

        [Fact]
        public void File_with_blank_etag_is_treated_as_missing()
        {
            CottonFileBrowserEntry file = CreateFile(" ");

            CottonSyncServerRevisionSnapshot revision = CottonSyncServerRevisionContract.Create(file);

            Assert.Equal(CottonSyncServerRevisionStatus.FileMissingETag, revision.Status);
            Assert.Null(revision.ETag);
        }

        private static CottonFileBrowserEntry CreateFile(string? eTag)
        {
            return CottonFileBrowserEntry.CreateCached(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CottonFileBrowserEntryType.File,
                "notes.txt",
                "Text",
                "42 B · Text",
                "More",
                "TXT",
                UpdatedAt,
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag);
        }
    }
}
