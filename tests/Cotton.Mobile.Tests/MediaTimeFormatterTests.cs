using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MediaTimeFormatterTests
    {
        [Theory]
        [InlineData(-1, "0:00")]
        [InlineData(0, "0:00")]
        [InlineData(9, "0:09")]
        [InlineData(65, "1:05")]
        [InlineData(3_723, "1:02:03")]
        public void Format_uses_compact_media_time(long totalSeconds, string expected)
        {
            Assert.Equal(expected, CottonMediaTimeFormatter.Format(TimeSpan.FromSeconds(totalSeconds)));
        }
    }
}
