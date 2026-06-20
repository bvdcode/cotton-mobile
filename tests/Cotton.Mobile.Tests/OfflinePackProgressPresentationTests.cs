using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class OfflinePackProgressPresentationTests
    {
        private static readonly Guid FolderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly DateTime UpdatedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Running_progress_shows_current_file_and_size_progress()
        {
            CottonOfflineDownloadQueueSnapshot queue = CreateQueue();

            CottonOfflinePackProgressSnapshot progress =
                CottonOfflinePackProgressSnapshot.CreateRunning(
                    queue,
                    completedCount: 1,
                    completedBytes: 1024,
                    currentItem: queue.Items[1]);

            Assert.True(progress.IsVisible);
            Assert.True(progress.IsRunning);
            Assert.Equal(CottonOfflinePackProgressStatus.Running, progress.Status);
            Assert.Equal("Keeping Projects offline", progress.Text);
            Assert.Equal("1/2 files · 1 KB of 3 KB · Saving zeta.txt", progress.Details);
            Assert.Equal(0.5d, progress.ProgressFraction);
            Assert.Equal("Keeping Projects offline. 1/2 files · 1 KB of 3 KB · Saving zeta.txt.", progress.AccessibilityText);
        }

        [Fact]
        public void Terminal_progress_copy_keeps_results_visible()
        {
            CottonOfflineDownloadQueueSnapshot queue = CreateQueue();

            CottonOfflinePackProgressSnapshot completed = CottonOfflinePackProgressSnapshot.CreateCompleted(queue);
            CottonOfflinePackProgressSnapshot cancelled =
                CottonOfflinePackProgressSnapshot.CreateCancelled(queue, completedCount: 1, completedBytes: 1024);
            CottonOfflinePackProgressSnapshot failed =
                CottonOfflinePackProgressSnapshot.CreateFailed(
                    queue,
                    completedCount: 1,
                    completedBytes: 1024,
                    failureText: "Network lost");

            Assert.Equal("Projects available offline", completed.Text);
            Assert.Equal("2 files · 3 KB", completed.Details);
            Assert.Equal("Projects offline cancelled", cancelled.Text);
            Assert.Equal("1/2 files saved", cancelled.Details);
            Assert.Equal("Projects offline failed", failed.Text);
            Assert.Equal("1/2 files saved · Network lost", failed.Details);
        }

        [Fact]
        public void Empty_progress_is_hidden()
        {
            CottonOfflinePackProgressSnapshot progress = CottonOfflinePackProgressSnapshot.Empty;

            Assert.False(progress.IsVisible);
            Assert.False(progress.IsRunning);
            Assert.Equal(string.Empty, progress.Text);
            Assert.Equal("No offline folder activity.", progress.AccessibilityText);
        }

        [Fact]
        public void Progress_rejects_invalid_counts_and_sizes()
        {
            Assert.Throws<ArgumentException>(
                () => new CottonOfflinePackProgressSnapshot(
                    CottonOfflinePackProgressStatus.Running,
                    " ",
                    0,
                    1,
                    0,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonOfflinePackProgressSnapshot(
                    CottonOfflinePackProgressStatus.Running,
                    "Projects",
                    2,
                    1,
                    0,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonOfflinePackProgressSnapshot(
                    CottonOfflinePackProgressStatus.Completed,
                    "Projects",
                    1,
                    2,
                    1,
                    2));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonOfflinePackProgressSnapshot(
                    CottonOfflinePackProgressStatus.Failed,
                    "Projects",
                    1,
                    2,
                    3,
                    2));
        }

        private static CottonOfflineDownloadQueueSnapshot CreateQueue()
        {
            return CottonOfflineDownloadQueueSnapshot.Create(new CottonFolderContent(
                FolderId,
                "Projects",
                [
                    CreateFile(SecondFileId, "zeta.txt", 2048),
                    CreateFile(FirstFileId, "alpha.txt", 1024),
                ]));
        }

        private static CottonFileBrowserEntry CreateFile(Guid id, string name, long sizeBytes)
        {
            return CottonFileBrowserEntry.CreateCached(
                id,
                CottonFileBrowserEntryType.File,
                name,
                "Text",
                "Text",
                "More",
                "TXT",
                UpdatedAt,
                sizeBytes,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag: null);
        }
    }
}
