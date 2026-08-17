using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootPathTextTests
    {
        [Fact]
        public void InternalRootNameIsRemovedFromVisiblePath()
        {
            string text = CottonSyncRootPathText.Create("2026", "Default / Pictures / 2026");

            Assert.Equal("2026 → Pictures / 2026", text);
        }

        [Fact]
        public void DirectChildIsNotRepeated()
        {
            string text = CottonSyncRootPathText.Create("Pictures", "Default / Pictures");

            Assert.Equal("Pictures", text);
        }
    }
}
