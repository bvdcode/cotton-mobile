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
    }
}
