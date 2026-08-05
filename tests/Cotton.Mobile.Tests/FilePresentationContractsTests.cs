using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.FileModelTestData;

namespace Cotton.Mobile.Tests
{
    public class FilePresentationContractsTests
    {
        [Fact]
        public void Thumbnail_snapshots_expose_stable_display_flags()
        {
            CottonFileThumbnailSnapshot shortPlaceholder = CottonFileThumbnailSnapshot.Placeholder("JS", "cache-key-short");
            CottonFileThumbnailSnapshot placeholder = CottonFileThumbnailSnapshot.Placeholder("PDF", "cache-key");
            CottonFileThumbnailSnapshot fourCharacterPlaceholder = CottonFileThumbnailSnapshot.Placeholder("DOCX", "cache-key-docx");
            CottonFileThumbnailSnapshot longPlaceholder = CottonFileThumbnailSnapshot.Placeholder("ARCHIVE", "cache-key-archive");
            CottonFileThumbnailSnapshot loading = CottonFileThumbnailSnapshot.Loading("IMG", "cache-key");
            CottonFileThumbnailSnapshot ready = CottonFileThumbnailSnapshot.Ready("IMG", "file:///tmp/preview.webp", "cache-key");
            CottonFileThumbnailSnapshot failed = CottonFileThumbnailSnapshot.Failed("TXT", "cache-key");

            Assert.Equal(28d, shortPlaceholder.PlaceholderFontSize);
            Assert.True(placeholder.IsPlaceholderVisible);
            Assert.False(placeholder.HasImage);
            Assert.False(placeholder.IsLoading);
            Assert.Equal(14d, placeholder.PlaceholderFontSize);
            Assert.Equal(12d, fourCharacterPlaceholder.PlaceholderFontSize);
            Assert.Equal(11d, longPlaceholder.PlaceholderFontSize);
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
    }
}
