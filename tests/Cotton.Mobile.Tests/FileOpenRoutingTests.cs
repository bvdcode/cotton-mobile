using Cotton.Files;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileOpenRoutingTests
    {
        private static readonly DateTime UpdatedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData("notes.txt", "text/plain; charset=utf-8", 42, CottonFilePreviewKind.Text, "text/plain")]
        [InlineData("data.json", "", 512, CottonFilePreviewKind.Text, "application/json")]
        [InlineData("diagram.svg", "", 1024, CottonFilePreviewKind.Text, "image/svg+xml")]
        [InlineData("photo.webp", "image/webp", 8_192, CottonFilePreviewKind.Image, "image/webp")]
        public void Known_in_app_preview_types_route_to_open(
            string name,
            string contentType,
            long sizeBytes,
            CottonFilePreviewKind expectedPreviewKind,
            string expectedContentType)
        {
            CottonFileOpenRoute route = CottonFileOpenRouter.CreateRoute(
                CreateEntry(name, contentType, sizeBytes));

            Assert.Equal(CottonFileOpenTarget.InAppPreview, route.Target);
            Assert.True(route.CanPreviewInApp);
            Assert.False(route.OpensWithSystemApp);
            Assert.Equal(expectedPreviewKind, route.PreviewKind);
            Assert.Equal(CottonSystemFileOpenKind.None, route.SystemKind);
            Assert.Equal("Open", route.ActionLabel);
            Assert.Equal(expectedContentType, route.ContentType);
        }

        [Theory]
        [InlineData("large.txt", "text/plain", 524_289)]
        [InlineData("local-large.md", "", 64)]
        public void Text_route_uses_available_local_size_when_present(
            string name,
            string contentType,
            long availableSizeBytes)
        {
            CottonFileOpenRoute route = CottonFileOpenRouter.CreateRoute(
                CreateEntry(name, contentType, sizeBytes: 2_000_000),
                availableSizeBytes);

            Assert.Equal(
                availableSizeBytes <= CottonFileOpenRouter.MaxTextPreviewBytes
                    ? CottonFileOpenTarget.InAppPreview
                    : CottonFileOpenTarget.SystemApp,
                route.Target);
        }

        [Theory]
        [InlineData("report.pdf", "", CottonSystemFileOpenKind.Pdf, "application/pdf", "No PDF app can open this file.")]
        [InlineData("song.mp3", "audio/mpeg", CottonSystemFileOpenKind.Audio, "audio/mpeg", "No audio app can open this file.")]
        [InlineData("movie.mp4", "", CottonSystemFileOpenKind.Video, "video/mp4", "No video app can open this file.")]
        [InlineData("brief.docx", "", CottonSystemFileOpenKind.Document, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "No document app can open this file.")]
        [InlineData("slides.pptx", "", CottonSystemFileOpenKind.Document, "application/vnd.openxmlformats-officedocument.presentationml.presentation", "No document app can open this file.")]
        [InlineData("archive.zip", "", CottonSystemFileOpenKind.Archive, "application/zip", "No archive app can open this file.")]
        [InlineData("unknown.bin", "", CottonSystemFileOpenKind.File, null, "No app can open this file type.")]
        public void Non_preview_types_route_to_system_open(
            string name,
            string contentType,
            CottonSystemFileOpenKind expectedSystemKind,
            string? expectedContentType,
            string expectedUnavailableStatus)
        {
            CottonFileOpenRoute route = CottonFileOpenRouter.CreateRoute(
                CreateEntry(name, contentType, sizeBytes: 1024));

            Assert.Equal(CottonFileOpenTarget.SystemApp, route.Target);
            Assert.False(route.CanPreviewInApp);
            Assert.True(route.OpensWithSystemApp);
            Assert.Equal(CottonFilePreviewKind.None, route.PreviewKind);
            Assert.Equal(expectedSystemKind, route.SystemKind);
            Assert.Equal("Open with system app", route.ActionLabel);
            Assert.Equal(expectedUnavailableStatus, route.UnavailableStatus);
            Assert.Equal(expectedContentType, route.ContentType);
        }

        [Fact]
        public void Required_content_type_falls_back_for_unknown_system_files()
        {
            Assert.Equal(
                "application/octet-stream",
                CottonFileOpenRouter.ResolveRequiredContentType("unknown.bin", null));
            Assert.Equal(
                "application/pdf",
                CottonFileOpenRouter.ResolveRequiredContentType("REPORT.PDF", null));
            Assert.Equal(
                "video/mp4",
                CottonFileOpenRouter.ResolveRequiredContentType("movie", " video/mp4; codecs=avc1 "));
        }

        [Fact]
        public void File_open_route_rejects_folder_entries()
        {
            CottonFileBrowserEntry folder = CottonFileBrowserEntry.CreateCached(
                Guid.NewGuid(),
                CottonFileBrowserEntryType.Folder,
                "Folder",
                "Folder",
                "Folder",
                "Open",
                "Folder",
                UpdatedAt,
                null,
                null,
                null);

            Assert.Throws<ArgumentException>(() => CottonFileOpenRouter.CreateRoute(folder));
        }

        private static CottonFileBrowserEntry CreateEntry(
            string name,
            string contentType,
            long sizeBytes)
        {
            return CottonFileBrowserEntry.FromFile(
                new NodeFileManifestDto
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    ContentType = contentType,
                    SizeBytes = sizeBytes,
                    UpdatedAt = UpdatedAt,
                });
        }
    }
}
