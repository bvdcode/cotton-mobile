using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CoreAssemblySmokeTests
    {
        [Fact]
        public void FileSizeFormatterIsAvailableFromCoreProject()
        {
            Assert.Equal("0 B", CottonFileSizeFormatter.Format(0));
        }
    }
}
