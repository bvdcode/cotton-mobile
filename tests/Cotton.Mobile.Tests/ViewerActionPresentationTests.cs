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

            string repositoryRoot = FindRepositoryRoot();
            foreach (string viewModelPath in viewModelPaths)
            {
                string content = File.ReadAllText(Path.Combine(repositoryRoot, viewModelPath));

                Assert.DoesNotContain("\"Opening...\"", content, StringComparison.Ordinal);
                Assert.DoesNotContain("\"Preparing share...\"", content, StringComparison.Ordinal);
            }
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "README.md"))
                    && Directory.Exists(Path.Combine(current.FullName, "src")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root was not found.");
        }
    }
}
