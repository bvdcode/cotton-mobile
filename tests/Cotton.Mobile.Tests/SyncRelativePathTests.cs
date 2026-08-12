using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRelativePathTests
    {
        [Fact]
        public void CreateFilePathPreservesNestedSegments()
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
        public void NormalizeFilePathRejectsInvalidSegments(string value)
        {
            Assert.Throws<ArgumentException>(() =>
                CottonSyncRelativePath.NormalizeFilePath(value, nameof(value)));
        }
    }
}
