using Cotton.Files;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileVersionHistoryPresentationTests
    {
        private static readonly Guid FileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid CurrentManifestId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid OriginalManifestId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

        [Fact]
        public void File_version_item_formats_current_version_metadata()
        {
            FileVersionDto version = CreateVersion(
                versionId: CurrentManifestId,
                manifestId: CurrentManifestId,
                versionNumber: 3,
                isCurrent: true,
                isOriginal: false,
                canDelete: false,
                updatedAtUtc: new DateTime(2026, 6, 20, 12, 45, 0, DateTimeKind.Utc),
                contentType: "image/png; charset=utf-8",
                sizeBytes: 1536);

            CottonFileVersionItemSnapshot item = CottonFileVersionItemSnapshot.Create(version, Utc);

            Assert.Equal(CurrentManifestId, item.VersionId);
            Assert.Equal(FileId, item.NodeFileId);
            Assert.Equal(CurrentManifestId, item.FileManifestId);
            Assert.Equal("photo.png", item.Name);
            Assert.Equal("Image", item.KindText);
            Assert.Equal("1.5 KB", item.SizeText);
            Assert.Equal("image/png", item.ContentTypeText);
            Assert.Equal("Current", item.VersionText);
            Assert.Equal("2026-06-20 12:45", item.UpdatedText);
            Assert.Equal("1.5 KB · Image · Updated 2026-06-20 12:45", item.DetailText);
            Assert.False(item.CanDelete);
        }

        [Fact]
        public void File_version_item_formats_original_version()
        {
            FileVersionDto version = CreateVersion(
                versionId: OriginalManifestId,
                manifestId: OriginalManifestId,
                versionNumber: 1,
                isCurrent: false,
                isOriginal: true,
                canDelete: true,
                updatedAtUtc: new DateTime(2026, 6, 18, 9, 0, 0, DateTimeKind.Utc),
                contentType: "text/plain",
                sizeBytes: 64);

            CottonFileVersionItemSnapshot item = CottonFileVersionItemSnapshot.Create(version, Utc);

            Assert.Equal("Text", item.KindText);
            Assert.Equal("64 B", item.SizeText);
            Assert.Equal("Version 1 · Original", item.VersionText);
            Assert.True(item.CanDelete);
        }

        [Fact]
        public void File_version_list_sorts_current_then_newest_versions()
        {
            FileVersionDto original = CreateVersion(
                versionId: OriginalManifestId,
                manifestId: OriginalManifestId,
                versionNumber: 1,
                isCurrent: false,
                isOriginal: true,
                canDelete: true,
                updatedAtUtc: new DateTime(2026, 6, 18, 9, 0, 0, DateTimeKind.Utc));
            FileVersionDto middle = CreateVersion(
                versionId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                manifestId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                versionNumber: 2,
                isCurrent: false,
                isOriginal: false,
                canDelete: true,
                updatedAtUtc: new DateTime(2026, 6, 19, 9, 0, 0, DateTimeKind.Utc));
            FileVersionDto current = CreateVersion(
                versionId: CurrentManifestId,
                manifestId: CurrentManifestId,
                versionNumber: 3,
                isCurrent: true,
                isOriginal: false,
                canDelete: false,
                updatedAtUtc: new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc));

            CottonFileVersionListSnapshot list = CottonFileVersionListSnapshot.Create(
                "photo.png",
                [original, current, middle],
                Utc);

            Assert.Equal(CottonFileVersionListStatus.Ready, list.Status);
            Assert.True(list.HasItems);
            Assert.Equal("3 versions for photo.png.", list.SummaryText);
            Assert.Equal(
                [3, 2, 1],
                list.Items.Select(item => item.VersionNumber).ToArray());
        }

        [Fact]
        public void File_version_list_keeps_empty_state_explicit()
        {
            CottonFileVersionListSnapshot list = CottonFileVersionListSnapshot.Create(
                "photo.png",
                [],
                Utc);

            Assert.Equal(CottonFileVersionListStatus.Empty, list.Status);
            Assert.False(list.HasItems);
            Assert.Equal("No versions found for photo.png.", list.SummaryText);
            Assert.Equal("No versions found.", list.EmptyText);
        }

        [Fact]
        public void File_version_status_text_covers_loading_and_failures()
        {
            Assert.Equal(
                "Loading versions for photo.png...",
                CottonFileVersionStatusText.CreateLoadingStatus(" photo.png "));
            Assert.Equal("1 version found.", CottonFileVersionStatusText.CreateLoadedStatus(1));
            Assert.Equal("2 versions found.", CottonFileVersionStatusText.CreateLoadedStatus(2));
            Assert.Equal("Version history cancelled.", CottonFileVersionStatusText.CancelledStatus);
            Assert.Equal("Could not load versions.", CottonFileVersionStatusText.FailedStatus);
            Assert.Equal(
                "Offline. Version history needs internet.",
                CottonFileVersionStatusText.OfflineUnavailableStatus);
        }

        [Fact]
        public void File_version_item_rejects_invalid_identity()
        {
            FileVersionDto missingManifest = CreateVersion(
                versionId: Guid.NewGuid(),
                manifestId: Guid.Empty,
                versionNumber: 1,
                isCurrent: true,
                isOriginal: true,
                canDelete: false,
                updatedAtUtc: DateTime.UtcNow);

            Assert.Throws<ArgumentException>(() =>
                CottonFileVersionItemSnapshot.Create(missingManifest, Utc));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CottonFileVersionStatusText.CreateLoadedStatus(-1));
        }

        [Fact]
        public void File_version_item_hides_default_timestamps()
        {
            FileVersionDto version = CreateVersion(
                versionId: Guid.NewGuid(),
                manifestId: CurrentManifestId,
                versionNumber: 1,
                isCurrent: true,
                isOriginal: true,
                canDelete: false,
                updatedAtUtc: DateTime.UtcNow,
                sizeBytes: 4096);
            version.CreatedAt = default;
            version.UpdatedAt = default;

            CottonFileVersionItemSnapshot item = CottonFileVersionItemSnapshot.Create(version, Utc);

            Assert.Equal("Unknown", item.CreatedText);
            Assert.Equal("Unknown", item.UpdatedText);
            Assert.Equal("4 KB · Image", item.DetailText);
            Assert.DoesNotContain("0001", item.DetailText);
        }

        private static FileVersionDto CreateVersion(
            Guid versionId,
            Guid manifestId,
            int versionNumber,
            bool isCurrent,
            bool isOriginal,
            bool canDelete,
            DateTime updatedAtUtc,
            string contentType = "image/png",
            long sizeBytes = 1024)
        {
            return new FileVersionDto
            {
                Id = versionId,
                NodeFileId = FileId,
                FileManifestId = manifestId,
                Name = "photo.png",
                ContentType = contentType,
                SizeBytes = sizeBytes,
                VersionNumber = versionNumber,
                IsCurrent = isCurrent,
                IsOriginal = isOriginal,
                CanDelete = canDelete,
                CreatedAt = updatedAtUtc.AddDays(-1),
                UpdatedAt = updatedAtUtc,
            };
        }
    }
}
