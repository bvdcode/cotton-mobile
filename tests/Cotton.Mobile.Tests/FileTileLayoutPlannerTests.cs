using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileTileLayoutPlannerTests
    {
        [Fact]
        public void Initial_metrics_match_stable_file_tile_defaults()
        {
            CottonFileTileLayoutMetrics metrics = CottonFileTileLayoutPlanner.InitialMetrics;

            Assert.Equal(150, metrics.SlotWidth);
            Assert.Equal(72, metrics.PreviewHeight);
            Assert.Equal(62, metrics.FolderIconSize);
            Assert.Equal(146, metrics.TileHeight);
            Assert.Equal(2, metrics.ColumnCount);
        }

        [Theory]
        [InlineData(260, 259, 159, 92, 227, 1)]
        [InlineData(360, 179, 110, 74, 178, 2)]
        [InlineData(720, 359, 221, 92, 289, 2)]
        public void Calculate_keeps_tile_metrics_stable_across_phone_widths(
            double contentWidth,
            double expectedSlotWidth,
            double expectedPreviewHeight,
            double expectedFolderIconSize,
            double expectedTileHeight,
            int expectedColumnCount)
        {
            CottonFileTileLayoutMetrics metrics = CottonFileTileLayoutPlanner.Calculate(contentWidth);

            Assert.Equal(expectedSlotWidth, metrics.SlotWidth);
            Assert.Equal(expectedPreviewHeight, metrics.PreviewHeight);
            Assert.Equal(expectedFolderIconSize, metrics.FolderIconSize);
            Assert.Equal(expectedTileHeight, metrics.TileHeight);
            Assert.Equal(expectedColumnCount, metrics.ColumnCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void Calculate_rejects_invalid_content_width(double contentWidth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonFileTileLayoutPlanner.Calculate(contentWidth));
        }
    }
}
