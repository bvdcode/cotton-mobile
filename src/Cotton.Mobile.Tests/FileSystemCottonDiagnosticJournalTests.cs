using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileSystemCottonDiagnosticJournalTests : IDisposable
    {
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(),
            "cotton-diagnostic-journal-tests",
            Guid.NewGuid().ToString("N"));

        [Fact]
        public void WritePersistsStructuredRecordWithoutExceptionDetails()
        {
            DateTime timestamp = new(2026, 8, 22, 12, 30, 0, DateTimeKind.Utc);
            using FileSystemCottonDiagnosticJournal journal = new(
                _directory,
                new FixedTimeProvider(timestamp));

            journal.Write(
                LogLevel.Warning,
                "Cotton.Mobile.Services.CottonSessionService",
                new EventId(2007),
                "Session restore was rejected with status 404.",
                typeof(HttpRequestException));

            string record = Assert.Single(journal.ReadAll());
            Assert.Contains("2026-08-22T12:30:00.0000000+00:00", record, StringComparison.Ordinal);
            Assert.Contains("Warning", record, StringComparison.Ordinal);
            Assert.Contains("\t2007\t", record, StringComparison.Ordinal);
            Assert.Contains("System.Net.Http.HttpRequestException", record, StringComparison.Ordinal);
            Assert.DoesNotContain("stack", record, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void WriteEscapesControlCharacters()
        {
            using FileSystemCottonDiagnosticJournal journal = new(
                _directory,
                TimeProvider.System);

            journal.Write(
                LogLevel.Information,
                "category",
                new EventId(1),
                "first\r\nsecond\tvalue",
                exceptionType: null);

            string record = Assert.Single(journal.ReadAll());
            Assert.DoesNotContain('\r', record);
            Assert.DoesNotContain('\n', record);
            Assert.Contains("first\\r\\nsecond\\tvalue", record, StringComparison.Ordinal);
        }

        [Fact]
        public void WriteRotatesAndBoundsStoredHistory()
        {
            const int recordCount = 400;
            using FileSystemCottonDiagnosticJournal journal = new(
                _directory,
                TimeProvider.System);

            for (int index = 0; index < recordCount; index++)
            {
                journal.Write(
                    LogLevel.Information,
                    "category",
                    new EventId(index),
                    $"record-{index:D3}-" + new string('x', 3000),
                    exceptionType: null);
            }

            IReadOnlyList<string> records = journal.ReadAll();
            Assert.DoesNotContain(records, record => record.Contains("record-000-", StringComparison.Ordinal));
            Assert.Contains(records, record => record.Contains("record-399-", StringComparison.Ordinal));
            Assert.InRange(Directory.GetFiles(_directory).Length, 1, 2);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }
    }
}
