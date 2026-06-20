using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileDetailsDisplayStateTests
    {
        private static readonly DateTime UpdatedAt = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);
        private static readonly TimeZoneInfo DisplayTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "CottonDetailsDisplay",
            TimeSpan.FromHours(3),
            "Cotton details display",
            "Cotton details display");

        [Fact]
        public void Create_formats_file_identity_and_message()
        {
            CottonFileBrowserEntry file = CreateFile("notes.txt", "Text", 42, "text/plain", UpdatedAt);

            CottonFileDetailsDisplayState details =
                CottonFileDetailsDisplayState.Create(file, localFile: null, DisplayTimeZone);

            Assert.Equal("notes.txt", details.Title);
            Assert.Equal("Text", details.KindText);
            Assert.Equal("42 B", details.SizeText);
            Assert.Equal("2026-06-18 15:00", details.UpdatedText);
            Assert.Equal("text/plain", details.ContentTypeText);
            Assert.Equal("Not saved", details.OnDeviceText);
            Assert.Equal(
                string.Join(
                    Environment.NewLine,
                    "Type: Text",
                    "Size: 42 B",
                    "Updated: 2026-06-18 15:00",
                    "Saved on this device: Not saved"),
                details.Message);
        }

        [Fact]
        public void Create_uses_unknown_for_missing_size_and_content_type()
        {
            CottonFileBrowserEntry file = CreateFile("mystery.bin", "File", null, null, UpdatedAt);

            CottonFileDetailsDisplayState details =
                CottonFileDetailsDisplayState.Create(file, localFile: null, TimeZoneInfo.Utc);

            Assert.Equal("Unknown", details.SizeText);
            Assert.Equal("Unknown", details.ContentTypeText);
            Assert.Contains("Size: Unknown", details.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("Content type:", details.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Create_marks_fresh_local_copy_as_on_device()
        {
            CottonFileBrowserEntry file = CreateFile("notes.txt", "Text", 42, "text/plain", UpdatedAt);
            var localFile = new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt.AddSeconds(-1));

            CottonFileDetailsDisplayState details =
                CottonFileDetailsDisplayState.Create(file, localFile, TimeZoneInfo.Utc);

            Assert.Equal("Saved (42 B)", details.OnDeviceText);
            Assert.Contains("Saved on this device: Saved (42 B)", details.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Create_marks_stale_local_copy_as_needing_refresh()
        {
            CottonFileBrowserEntry file = CreateFile("notes.txt", "Text", 42, "text/plain", UpdatedAt);
            var localFile = new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt.AddSeconds(-3));

            CottonFileDetailsDisplayState details =
                CottonFileDetailsDisplayState.Create(file, localFile, TimeZoneInfo.Utc);

            Assert.Equal("Needs refresh (42 B)", details.OnDeviceText);
        }

        [Fact]
        public void Create_marks_wrong_size_local_copy_as_needing_refresh()
        {
            CottonFileBrowserEntry file = CreateFile("notes.txt", "Text", 42, "text/plain", UpdatedAt);
            var localFile = new CottonLocalFileSnapshot("notes.txt", 41, UpdatedAt);

            CottonFileDetailsDisplayState details =
                CottonFileDetailsDisplayState.Create(file, localFile, TimeZoneInfo.Utc);

            Assert.Equal("Needs refresh (41 B)", details.OnDeviceText);
        }

        [Fact]
        public void Create_treats_unspecified_updated_time_as_utc()
        {
            DateTime unspecifiedUpdatedAt = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Unspecified);
            CottonFileBrowserEntry file = CreateFile("notes.txt", "Text", 42, "text/plain", unspecifiedUpdatedAt);

            CottonFileDetailsDisplayState details =
                CottonFileDetailsDisplayState.Create(file, localFile: null, DisplayTimeZone);

            Assert.Equal("2026-06-18 15:00", details.UpdatedText);
        }

        [Fact]
        public void Create_requires_file_and_display_time_zone()
        {
            CottonFileBrowserEntry file = CreateFile("notes.txt", "Text", 42, "text/plain", UpdatedAt);

            Assert.Throws<ArgumentNullException>(
                () => CottonFileDetailsDisplayState.Create(null!, localFile: null, TimeZoneInfo.Utc));
            Assert.Throws<ArgumentNullException>(
                () => CottonFileDetailsDisplayState.Create(file, localFile: null, displayTimeZone: null!));
        }

        private static CottonFileBrowserEntry CreateFile(
            string name,
            string kind,
            long? sizeBytes,
            string? contentType,
            DateTime updatedAtUtc)
        {
            string details = sizeBytes.HasValue
                ? $"{CottonFileSizeFormatter.Format(sizeBytes.Value)} · {kind}"
                : kind;

            return CottonFileBrowserEntry.CreateCached(
                Guid.NewGuid(),
                CottonFileBrowserEntryType.File,
                name,
                kind,
                details,
                "More",
                kind.ToUpperInvariant(),
                updatedAtUtc,
                sizeBytes,
                contentType,
                previewHashEncryptedHex: null,
                eTag: null);
        }
    }
}
