using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RecentFilesPresentationTests
    {
        [Fact]
        public void Recent_file_open_does_not_show_transient_open_status()
        {
            string content = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/RecentFilesViewModel.cs");

            Assert.DoesNotContain("Status = $\"Opening {file.Name}...\";", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Status = $\"Opened {file.Name}.\";", content, StringComparison.Ordinal);
            Assert.Contains("Status = $\"Downloading {file.Name}...\";", content, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_file_open_defers_transient_open_status()
        {
            string content = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/MainPageFileBrowserController.cs");

            Assert.DoesNotContain("BeginFileAction($\"Opening {file.Name}...\")", content, StringComparison.Ordinal);
            Assert.Contains("BeginDeferredFileAction(", content, StringComparison.Ordinal);
            Assert.Contains("showStatusPanelWhenLoading: false", content, StringComparison.Ordinal);
            Assert.Contains("showStatusTextWhenLoading: false", content, StringComparison.Ordinal);
            Assert.Contains("blockBrowserChromeWhilePending: false", content, StringComparison.Ordinal);
            Assert.Contains("showLoadingAfterDelay: false", content, StringComparison.Ordinal);
            Assert.Contains("showStatusPanel: false", content, StringComparison.Ordinal);
            Assert.Contains("showStatusText: false", content, StringComparison.Ordinal);
            Assert.Contains("ShowFileActionPending", content, StringComparison.Ordinal);
            Assert.Contains("if (!showStatusText || file.SizeBytes is not > 0)", content, StringComparison.Ordinal);
            Assert.Contains("&& !_display.IsFilesLoading", content, StringComparison.Ordinal);
            Assert.Contains("&& !_display.IsFileBrowserChromeEnabled", content, StringComparison.Ordinal);
            Assert.Contains("DeferredFileActionLoadingDelay = TimeSpan.FromMilliseconds(1200)", content, StringComparison.Ordinal);
            Assert.Contains("CancellationToken cancellationToken = fileActionCancellation.Token;", content, StringComparison.Ordinal);
            Assert.Contains("Task.Delay(DeferredFileActionLoadingDelay, cancellationToken)", content, StringComparison.Ordinal);
            Assert.Contains(
                "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
                content,
                StringComparison.Ordinal);
            Assert.Contains("_fileActionCancellation = null;\n            cancellation.Cancel();", content, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_file_share_defers_transient_prepare_status()
        {
            string content = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/MainPageFileBrowserController.cs");

            Assert.DoesNotContain("BeginFileAction($\"Preparing {file.Name}...\")", content, StringComparison.Ordinal);
            Assert.Contains("BeginDeferredFileAction($\"Preparing {file.Name}...\")", content, StringComparison.Ordinal);
        }
    }
}
