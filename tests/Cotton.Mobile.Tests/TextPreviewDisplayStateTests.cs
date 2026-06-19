using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TextPreviewDisplayStateTests
    {
        [Theory]
        [InlineData("", 0)]
        [InlineData("one", 1)]
        [InlineData("one\ntwo", 2)]
        [InlineData("one\ntwo\n", 3)]
        [InlineData("one\r\ntwo", 2)]
        [InlineData("one\rtwo", 2)]
        public void Count_lines_handles_common_text_newlines(string content, int expectedLineCount)
        {
            Assert.Equal(expectedLineCount, CottonTextPreviewDisplayState.CountLines(content));
        }

        [Theory]
        [InlineData(" Text ", 42, "hello", "Text · 42 B · 1 line")]
        [InlineData("", 1536, "one\ntwo", "Text · 1.5 KB · 2 lines")]
        [InlineData("Config", -1, "", "Config · 0 B · 0 lines")]
        public void Create_formats_details_with_kind_size_and_line_count(
            string kind,
            long sizeBytes,
            string content,
            string expectedDetails)
        {
            CottonTextPreviewDisplayState display =
                CottonTextPreviewDisplayState.Create(kind, sizeBytes, content);

            Assert.Equal(expectedDetails, display.DetailsText);
        }
    }
}
