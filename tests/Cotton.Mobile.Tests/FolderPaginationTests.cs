using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FolderPaginationTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(100, 1)]
        [InlineData(101, 2)]
        public void Page_count_uses_declared_total_count(long totalCount, int expectedPageCount)
        {
            Assert.Equal(expectedPageCount, CottonFolderPagination.CreatePageCount(totalCount));
        }

        [Fact]
        public void Excessive_declared_item_count_is_rejected()
        {
            Assert.Throws<InvalidDataException>(() =>
                CottonFolderPagination.CreatePageCount(CottonFolderPagination.MaximumItemCount + 1L));
        }

        [Fact]
        public void Loaded_item_count_must_match_declared_total_count()
        {
            CottonFolderPagination.EnsureComplete(expectedItemCount: 2, loadedItemCount: 2);

            Assert.Throws<InvalidDataException>(() =>
                CottonFolderPagination.EnsureComplete(expectedItemCount: 2, loadedItemCount: 1));
        }
    }
}
