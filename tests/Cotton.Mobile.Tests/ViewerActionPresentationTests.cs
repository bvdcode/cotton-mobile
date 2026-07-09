using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ViewerActionPresentationTests
    {
        [Fact]
        public void Viewer_system_actions_do_not_show_transient_busy_status()
        {
            string[] viewModelPaths =
            [
                "src/Cotton.Mobile/ViewModels/ImageViewerViewModel.cs",
                "src/Cotton.Mobile/ViewModels/MediaViewerViewModel.cs",
                "src/Cotton.Mobile/ViewModels/PdfViewerViewModel.cs",
                "src/Cotton.Mobile/ViewModels/TextViewerViewModel.cs",
            ];

            foreach (string viewModelPath in viewModelPaths)
            {
                string content = RepositoryPath.ReadText(viewModelPath);

                Assert.DoesNotContain("\"Opening...\"", content, StringComparison.Ordinal);
                Assert.DoesNotContain("\"Preparing share...\"", content, StringComparison.Ordinal);
                Assert.DoesNotContain("\"Copying...\"", content, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Android_pdf_preview_renders_bounded_page_sample()
        {
            string renderer = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Platforms/Android/AndroidPdfPreviewRenderer.cs");
            string snapshot = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Services/PdfPreviewDocumentSnapshot.cs");

            Assert.Contains("MaxPreviewPageCount = 8", renderer, StringComparison.Ordinal);
            Assert.Contains("int renderedPageCount = Math.Min(renderer.PageCount, MaxPreviewPageCount);", renderer, StringComparison.Ordinal);
            Assert.Contains("new List<PdfPreviewPageSnapshot>(renderedPageCount)", renderer, StringComparison.Ordinal);
            Assert.Contains("index < renderedPageCount", renderer, StringComparison.Ordinal);
            Assert.Contains("Pages.Count > 0 && Pages.Count < TotalPageCount", snapshot, StringComparison.Ordinal);
            Assert.Contains("Showing first {Pages.Count} of {TotalPageCount} pages", snapshot, StringComparison.Ordinal);
        }
    }
}
