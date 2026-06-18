using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CoreAssemblySmokeTests
    {
        [Fact]
        public void File_size_formatter_is_available_from_core_project()
        {
            Assert.Equal("0 B", CottonFileSizeFormatter.Format(0));
        }
    }
}
