using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CottonSyncIgnoredFileNameTests
    {
        [Theory]
        [InlineData(".temp")]
        [InlineData(".temp1")]
        [InlineData(".temporary-upload")]
        [InlineData(".TEMP-camera")]
        public void IsIgnoredMatchesTemporaryFilePrefix(string fileName)
        {
            Assert.True(CottonSyncIgnoredFileName.IsIgnored(fileName));
        }

        [Theory]
        [InlineData("photo.temp")]
        [InlineData("temp-photo.jpg")]
        [InlineData("photo.jpg")]
        [InlineData("")]
        public void IsIgnoredKeepsUnrelatedFileNames(string fileName)
        {
            Assert.False(CottonSyncIgnoredFileName.IsIgnored(fileName));
        }

        [Fact]
        public void IsIgnoredRetainsGeneratedWorkingFileProtection()
        {
            string workingFileName = CottonSyncWorkingFileName.CreateTemporary("photo.jpg");

            Assert.True(CottonSyncIgnoredFileName.IsIgnored(workingFileName));
        }
    }
}
