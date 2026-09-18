using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CottonDiagnosticLoggerProviderTests : IDisposable
    {
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(),
            "cotton-diagnostic-provider-tests",
            Guid.NewGuid().ToString("N"));

        [Fact]
        public void LoggerPersistsOnlyAllowedCategoriesWithoutExceptionMessage()
        {
            using FileSystemCottonDiagnosticJournal journal = new(
                _directory,
                TimeProvider.System);
            using CottonDiagnosticLoggerProvider provider = new(journal);
            ILogger allowed = provider.CreateLogger("Cotton.Mobile.Services.CottonSessionService");
            ILogger excluded = provider.CreateLogger("Cotton.Mobile.Services.UnrelatedService");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                ThrowDiagnosticException);
            allowed.Log(
                LogLevel.Warning,
                new EventId(1),
                "Safe diagnostic message 404.",
                exception,
                static (message, _) => message);
            excluded.Log(
                LogLevel.Error,
                new EventId(2),
                "Excluded message.",
                exception: null,
                static (message, _) => message);

            string record = Assert.Single(journal.ReadAll());
            Assert.Contains("Safe diagnostic message 404.", record, StringComparison.Ordinal);
            Assert.Contains("System.InvalidOperationException", record, StringComparison.Ordinal);
            Assert.Contains("private exception detail", record, StringComparison.Ordinal);
            Assert.Contains(nameof(ThrowDiagnosticException), record, StringComparison.Ordinal);
            Assert.DoesNotContain("Excluded message", record, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("Cotton.Mobile.ViewModels.SyncSettingsViewModel")]
        [InlineData("Cotton.Mobile.ViewModels.SyncSettingsExecutionHandler")]
        [InlineData("Cotton.Mobile.ViewModels.SyncSettingsSetupHandler")]
        [InlineData("Cotton.Mobile.ViewModels.SyncSettingsLoadingHandler")]
        [InlineData("Cotton.Mobile.ViewModels.SyncSettingsManagementHandler")]
        [InlineData("Cotton.Mobile.ViewModels.MediaOriginalRestoreReviewHandler")]
        public void LoggerPersistsUploadScreenFailures(string category)
        {
            using FileSystemCottonDiagnosticJournal journal = new(_directory, TimeProvider.System);
            using CottonDiagnosticLoggerProvider provider = new(journal);
            ILogger logger = provider.CreateLogger(category);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                ThrowDiagnosticException);
            logger.Log(
                LogLevel.Warning,
                new EventId(1),
                "Upload screen command failed.",
                exception,
                static (message, _) => message);

            string record = Assert.Single(journal.ReadAll());
            Assert.Contains(category, record, StringComparison.Ordinal);
            Assert.Contains(nameof(ThrowDiagnosticException), record, StringComparison.Ordinal);
            Assert.False(logger.IsEnabled(LogLevel.Information));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }

        [Fact]
        public void OriginalMediaRecoveryPersistsScanAndConfirmedReplacements()
        {
            using FileSystemCottonDiagnosticJournal journal = new(_directory, TimeProvider.System);
            using CottonDiagnosticLoggerProvider provider = new(journal);
            ILogger scanner = provider.CreateLogger(typeof(CottonMediaOriginalRestoreService).FullName!);
            ILogger executor = provider.CreateLogger(typeof(CottonMediaOriginalRestoreExecutor).FullName!);
            Guid rootId = Guid.NewGuid();
            Guid fileId = Guid.NewGuid();
            Guid operationId = Guid.NewGuid();

            CottonMediaRestoreLog.ScanCompleted(scanner, rootId, 2, 10, 1);
            CottonMediaRestoreLog.FileStarted(executor, rootId, fileId, operationId);
            CottonMediaRestoreLog.FileCompleted(executor, rootId, fileId, operationId);
            CottonMediaRestoreLog.FileChanged(executor, rootId, fileId);

            IReadOnlyList<string> records = journal.ReadAll();
            Assert.Equal(4, records.Count);
            for (int index = 0; index < records.Count; index++)
            {
                Assert.Contains($"\t{2301 + index}\t", records[index], StringComparison.Ordinal);
                Assert.Contains(rootId.ToString(), records[index], StringComparison.Ordinal);
            }

            Assert.Contains(operationId.ToString(), records[2], StringComparison.Ordinal);
            Assert.Contains(fileId.ToString(), records[2], StringComparison.Ordinal);
        }

        [Fact]
        public void TokenReadsDoNotDisplaceSessionChanges()
        {
            using FileSystemCottonDiagnosticJournal journal = new(_directory, TimeProvider.System);
            using CottonDiagnosticLoggerProvider provider = new(journal);
            ILogger logger = provider.CreateLogger("Cotton.Mobile.Services.SecureStorageCottonTokenStore");
            for (int index = 0; index < 1000; index++)
            {
                CottonSessionDiagnosticLog.TokenStoreLoaded(logger);
            }

            CottonSessionDiagnosticLog.TokenStoreSaved(logger);
            CottonSessionDiagnosticLog.TokenStoreCleared(logger);

            IReadOnlyList<string> records = journal.ReadAll();
            Assert.Equal(2, records.Count);
            Assert.Contains(records, record => record.Contains("\t2012\t", StringComparison.Ordinal));
            Assert.Contains(records, record => record.Contains("\t2013\t", StringComparison.Ordinal));
        }

        private static void ThrowDiagnosticException()
        {
            throw new InvalidOperationException("private exception detail");
        }
    }
}
