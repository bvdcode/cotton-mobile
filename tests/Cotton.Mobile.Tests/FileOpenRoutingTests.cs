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
        [InlineData("Program.cs", "", 256, CottonFilePreviewKind.Text, "text/plain")]
        [InlineData("build.gradle", "", 256, CottonFilePreviewKind.Text, "text/plain")]
        [InlineData("script.py", "", 256, CottonFilePreviewKind.Text, "text/x-python")]
        [InlineData("diagram.svg", "", 1024, CottonFilePreviewKind.Text, "image/svg+xml")]
        [InlineData("inline-svg", "image/svg+xml", 1024, CottonFilePreviewKind.Text, "image/svg+xml")]
        [InlineData("photo.webp", "image/webp", 8_192, CottonFilePreviewKind.Image, "image/webp")]
        [InlineData("report.pdf", "", 16_384, CottonFilePreviewKind.Pdf, "application/pdf")]
        [InlineData("inline-pdf", "application/pdf", 16_384, CottonFilePreviewKind.Pdf, "application/pdf")]
        [InlineData("song.mp3", "audio/mpeg", 4_096, CottonFilePreviewKind.Audio, "audio/mpeg")]
        [InlineData("voice.m4a", "", 4_096, CottonFilePreviewKind.Audio, "audio/mp4")]
        [InlineData("movie.mp4", "", 16_384, CottonFilePreviewKind.Video, "video/mp4")]
        [InlineData("clip.webm", "video/webm; codecs=vp9", 16_384, CottonFilePreviewKind.Video, "video/webm")]
        public void KnownInAppPreviewTypesRouteToOpen(
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
        public void TextRouteUsesAvailableLocalSizeWhenPresent(
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
        [InlineData("brief.docx", "", CottonSystemFileOpenKind.Document, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "No document app can open this file.")]
        [InlineData("slides.pptx", "", CottonSystemFileOpenKind.Document, "application/vnd.openxmlformats-officedocument.presentationml.presentation", "No document app can open this file.")]
        [InlineData("archive.zip", "", CottonSystemFileOpenKind.Archive, "application/zip", "No archive app can open this file.")]
        [InlineData("unknown.bin", "", CottonSystemFileOpenKind.File, null, "No app can open this file type.")]
        public void NonPreviewTypesRouteToSystemOpen(
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
        public void OversizedSvgRoutesToSystemOpenWithSvgCopy()
        {
            CottonFileOpenRoute route = CottonFileOpenRouter.CreateRoute(
                CreateEntry(
                    "large.svg",
                    contentType: "",
                    sizeBytes: CottonFileOpenRouter.MaxTextPreviewBytes + 1));

            Assert.Equal(CottonFileOpenTarget.SystemApp, route.Target);
            Assert.True(route.OpensWithSystemApp);
            Assert.Equal(CottonSystemFileOpenKind.Svg, route.SystemKind);
            Assert.Equal("image/svg+xml", route.ContentType);
            Assert.Equal("No SVG app can open this file.", route.UnavailableStatus);
        }

        [Fact]
        public void RequiredContentTypeFallsBackForUnknownSystemFiles()
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
        public void FileOpenRouteRejectsFolderEntries()
        {
            CottonFileBrowserEntry folder = CottonFileBrowserEntryFactory.CreateFolder(
                Guid.NewGuid(),
                "Folder",
                UpdatedAt);

            Assert.Throws<ArgumentException>(() => CottonFileOpenRouter.CreateRoute(folder));
        }

        private static CottonFileBrowserEntry CreateEntry(
            string name,
            string contentType,
            long sizeBytes)
        {
            return CottonFileBrowserEntryFactory.FromFile(
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
