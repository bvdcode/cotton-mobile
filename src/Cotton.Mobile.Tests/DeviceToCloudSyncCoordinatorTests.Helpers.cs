using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public partial class DeviceToCloudSyncCoordinatorTests
    {
        public void Dispose()
        {
            _reviewStore.Dispose();
            _originalRestore.Dispose();
            _loggerFactory.Dispose();
            _journal.Dispose();
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }

        private static void AssertProgress(
            CottonSyncProgressSnapshot? progress,
            int completedItemCount,
            int totalItemCount)
        {
            Assert.NotNull(progress);
            Assert.Equal(CottonSyncProgressStage.ApplyingChanges, progress.Stage);
            Assert.Equal(completedItemCount, progress.CompletedItemCount);
            Assert.Equal(totalItemCount, progress.TotalItemCount);
        }

        private static void AssertUploadProgress(
            CottonSyncProgressSnapshot? progress,
            long transferredBytes)
        {
            Assert.NotNull(progress);
            Assert.Equal(CottonSyncProgressStage.UploadingFile, progress.Stage);
            Assert.Equal("alpha.txt", progress.Transfer?.ItemName);
            Assert.Equal(transferredBytes, progress.Transfer?.TransferredBytes);
            Assert.Equal(42, progress.Transfer?.TotalBytes);
        }
    }
}
