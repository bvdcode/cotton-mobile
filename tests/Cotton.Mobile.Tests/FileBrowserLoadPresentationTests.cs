using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileBrowserLoadPresentationTests
    {
        [Fact]
        public void File_browser_does_not_wait_for_local_file_markers_before_first_render()
        {
            string controller = RepositoryPath.ReadText(
                "src/Cotton.Mobile/ViewModels/MainPageFileBrowserController.cs");

            Assert.DoesNotContain("ApplyLocalFilesAsync", controller, StringComparison.Ordinal);
            Assert.Equal(
                4,
                CountOccurrences(controller, "RefreshLocalFileStateAfterFirstRender(instanceUri);"));
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int startIndex = 0;
            while (startIndex < text.Length)
            {
                int matchIndex = text.IndexOf(value, startIndex, StringComparison.Ordinal);
                if (matchIndex < 0)
                {
                    return count;
                }

                count++;
                startIndex = matchIndex + value.Length;
            }

            return count;
        }
    }
}
