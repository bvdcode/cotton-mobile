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
    }
}
