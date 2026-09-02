using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncUploadProgressReporterTests
    {
        [Fact]
        public void CompletedTransferReportsFileBytesAndOverallItemPosition()
        {
            Guid rootId = Guid.NewGuid();
            CottonSyncProgressHub hub = new();
            CottonSyncUploadProgressReporter reporter = new(
                rootId,
                "photo.jpg",
                uploadNumber: 2,
                uploadCount: 3,
                completedItemCount: 1,
                totalItemCount: 3,
                totalBytes: 1024,
                progressHub: hub,
                timeProvider: TimeProvider.System);

            reporter.Report(0);
            reporter.Report(1024);

            CottonSyncProgressSnapshot progress = Assert.Single(hub.GetCurrent());
            Assert.Equal(CottonSyncProgressStage.UploadingFile, progress.Stage);
            Assert.Equal(1, progress.CompletedItemCount);
            Assert.Equal(3, progress.TotalItemCount);
            Assert.Equal("photo.jpg", progress.Transfer?.ItemName);
            Assert.Equal(2, progress.Transfer?.UploadNumber);
            Assert.Equal(3, progress.Transfer?.UploadCount);
            Assert.Equal(1024, progress.Transfer?.TransferredBytes);
            Assert.Equal(1024, progress.Transfer?.TotalBytes);
        }
    }
}
