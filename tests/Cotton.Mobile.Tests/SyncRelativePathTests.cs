using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRelativePathTests
    {
        [Fact]
        public void Create_file_path_preserves_nested_segments()
        {
            string path = CottonSyncRelativePath.CreateFilePath("Projects / Reports", " Q2.pdf ");

            Assert.Equal("Projects/Reports/Q2.pdf", path);
            Assert.Equal("Q2.pdf", CottonSyncRelativePath.GetFileName(path));
        }

        [Theory]
        [InlineData("../report.pdf")]
        [InlineData("Projects//report.pdf")]
        [InlineData("/Projects/report.pdf")]
        [InlineData("Projects/report?.pdf")]
        public void Normalize_file_path_rejects_invalid_segments(string value)
        {
            Assert.Throws<ArgumentException>(() =>
                CottonSyncRelativePath.NormalizeFilePath(value, nameof(value)));
        }
    }
}
