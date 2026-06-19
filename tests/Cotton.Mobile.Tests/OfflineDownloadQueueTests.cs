using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class OfflineDownloadQueueTests
    {
        private static readonly Guid FolderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly DateTime UpdatedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime PinnedAt = new(2026, 6, 19, 12, 30, 0, DateTimeKind.Utc);

        [Fact]
        public void Create_queue_orders_ready_folder_direct_files_by_name()
        {
            CottonFolderContent content = CreateContent(
                CreateFile(SecondFileId, "zeta.txt", 2048),
                CreateFile(FirstFileId, "alpha.txt", 1024));

            CottonOfflineDownloadQueueSnapshot queue = CottonOfflineDownloadQueueSnapshot.Create(content);

            Assert.Equal(FolderId, queue.FolderId);
            Assert.Equal("Projects", queue.FolderName);
            Assert.Equal(2, queue.TotalCount);
            Assert.Equal(3072, queue.TotalSizeBytes);
            Assert.Equal(FirstFileId, queue.Items[0].FileId);
            Assert.Equal("alpha.txt", queue.Items[0].FileName);
            Assert.Equal(1, queue.Items[0].Position);
            Assert.Equal(SecondFileId, queue.Items[1].FileId);
            Assert.Equal(2, queue.Items[1].Position);
        }

        [Fact]
        public void Create_queue_rejects_folders_with_child_folders()
        {
            CottonFolderContent content = CreateContent(
                CreateFile(FirstFileId, "alpha.txt", 1024),
                CreateFolder("Archive"));

            Assert.Throws<InvalidOperationException>(() => CottonOfflineDownloadQueueSnapshot.Create(content));
        }

        [Fact]
        public void Create_queue_rejects_unknown_size_files()
        {
            CottonFolderContent content = CreateContent(CreateFile(FirstFileId, "alpha.txt", sizeBytes: null));

            Assert.Throws<InvalidOperationException>(() => CottonOfflineDownloadQueueSnapshot.Create(content));
        }

        [Fact]
        public void Queue_item_creates_existing_offline_file_pin_metadata()
        {
            CottonOfflineDownloadQueueItem item = CottonOfflineDownloadQueueItem.Create(
                position: 1,
                CreateFile(FirstFileId, "alpha.txt", 1024));

            CottonOfflineFilePinSnapshot pin = item.CreatePin(PinnedAt);

            Assert.Equal(FirstFileId, pin.FileId);
            Assert.Equal("alpha.txt", pin.FileName);
            Assert.Equal(PinnedAt, pin.PinnedAtUtc);
            Assert.Equal(UpdatedAt, pin.RemoteUpdatedAtUtc);
            Assert.Equal(1024, pin.SizeBytes);
            Assert.Equal("text/plain", pin.ContentType);
        }

        [Fact]
        public void Queue_status_text_covers_visible_progress_and_failure_copy()
        {
            CottonOfflineDownloadQueueSnapshot queue = CottonOfflineDownloadQueueSnapshot.Create(CreateContent(
                CreateFile(FirstFileId, "alpha.txt", 1024),
                CreateFile(SecondFileId, "zeta.txt", 2048)));

            Assert.Equal(
                "Queued 2 files for offline use (3 KB).",
                CottonOfflineDownloadQueueStatusText.CreateQueuedStatus(queue));
            Assert.Equal(
                "Saving 1 of 2: alpha.txt...",
                CottonOfflineDownloadQueueStatusText.CreateStartingItemStatus(queue.Items[0], queue.TotalCount));
            Assert.Equal(
                "2 files available offline from Projects.",
                CottonOfflineDownloadQueueStatusText.CreateCompletedStatus(queue));
            Assert.Equal(
                "Keep folder offline cancelled after 1/2 files.",
                CottonOfflineDownloadQueueStatusText.CreateCancelledStatus(1, queue.TotalCount));
            Assert.Equal(
                "Keep folder offline failed after 1/2 files.",
                CottonOfflineDownloadQueueStatusText.CreateFailedStatus(1, queue.TotalCount));
        }

        [Fact]
        public void Queue_item_rejects_folder_entries()
        {
            Assert.Throws<ArgumentException>(() => CottonOfflineDownloadQueueItem.Create(1, CreateFolder("Archive")));
        }

        private static CottonFolderContent CreateContent(params CottonFileBrowserEntry[] entries)
        {
            return new CottonFolderContent(FolderId, "Projects", entries);
        }

        private static CottonFileBrowserEntry CreateFile(Guid id, string name, long? sizeBytes)
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
                previewHashEncryptedHex: null);
        }

        private static CottonFileBrowserEntry CreateFolder(string name)
        {
            return CottonFileBrowserEntry.CreateCached(
                Guid.NewGuid(),
                CottonFileBrowserEntryType.Folder,
                name,
                "Folder",
                "Folder",
                "Open",
                "Folder",
                UpdatedAt,
                sizeBytes: null,
                contentType: null,
                previewHashEncryptedHex: null);
        }
    }
}
