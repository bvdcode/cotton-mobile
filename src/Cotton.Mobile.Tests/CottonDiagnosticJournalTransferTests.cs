using Cotton.Mobile.Services;
using System.Globalization;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CottonDiagnosticJournalTransferTests
    {
        [Fact]
        public async Task TransferPreservesLongUnicodeRecords()
        {
            string[] records = [new string('Ж', 32000), "second\trecord\\ntext"];
            using StringWriter writer = new(CultureInfo.InvariantCulture);
            await CottonDiagnosticJournalTransfer.WriteAsync(writer, records);
            string export = writer.ToString();

            CottonDiagnosticJournalTransfer.Validate(export);

            using StringReader reader = new(export);
            Assert.Equal("BEGIN records=2", await reader.ReadLineAsync(TestContext.Current.CancellationToken));
            Assert.Equal(records[0], await reader.ReadLineAsync(TestContext.Current.CancellationToken));
            Assert.Equal(records[1], await reader.ReadLineAsync(TestContext.Current.CancellationToken));
            Assert.Equal("END", await reader.ReadLineAsync(TestContext.Current.CancellationToken));
            Assert.Null(await reader.ReadLineAsync(TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData("BEGIN records=2\nfirst\nEND\n")]
        [InlineData("BEGIN records=1\nfirst\n")]
        [InlineData("BEGIN records=0\nEND\nunexpected")]
        [InlineData("Error reading content provider")]
        public void TransferRejectsIncompleteExports(string export)
        {
            Assert.Throws<InvalidDataException>(() => CottonDiagnosticJournalTransfer.Validate(export));
        }
    }
}
