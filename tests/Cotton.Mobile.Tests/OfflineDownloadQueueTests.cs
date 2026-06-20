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
            Assert.Equal("alpha.txt", queue.Items[0].DisplayName);
            Assert.Equal(1, queue.Items[0].Position);
            Assert.Equal(SecondFileId, queue.Items[1].FileId);
            Assert.Equal("zeta.txt", queue.Items[1].DisplayName);
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
        public void Queue_item_display_name_does_not_change_pin_file_name()
        {
            CottonOfflineDownloadQueueItem item = CottonOfflineDownloadQueueItem.Create(
                position: 1,
                CreateFile(FirstFileId, "alpha.txt", 1024),
                displayName: "Archive/alpha.txt");

            CottonOfflineFilePinSnapshot pin = item.CreatePin(PinnedAt);

            Assert.Equal("alpha.txt", item.FileName);
            Assert.Equal("Archive/alpha.txt", item.DisplayName);
            Assert.Equal("alpha.txt", pin.FileName);
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
        public void Free_space_warning_policy_skips_small_folder_when_space_is_healthy()
        {
            CottonOfflineDownloadQueueSnapshot queue = CottonOfflineDownloadQueueSnapshot.Create(
                CreateContent(CreateFile(FirstFileId, "alpha.txt", 1024)));

            CottonOfflineFolderFreeSpaceWarning warning =
                CottonOfflineFolderFreeSpaceWarningPolicy.CreateWarning(
                    queue,
                    CottonDeviceStorageSpaceSnapshot.Available(2L * 1024L * 1024L * 1024L));

            Assert.False(warning.ShouldWarn);
            Assert.Equal(CottonOfflineFolderFreeSpaceWarningKind.None, warning.Kind);
        }

        [Fact]
        public void Free_space_warning_policy_warns_for_large_folder()
        {
            CottonOfflineDownloadQueueSnapshot queue = CottonOfflineDownloadQueueSnapshot.Create(
                CreateContent(CreateFile(
                    FirstFileId,
                    "movie.mp4",
                    CottonOfflineFolderFreeSpaceWarningPolicy.LargeFolderWarningBytes)));

            CottonOfflineFolderFreeSpaceWarning warning =
                CottonOfflineFolderFreeSpaceWarningPolicy.CreateWarning(
                    queue,
                    CottonDeviceStorageSpaceSnapshot.Available(2L * 1024L * 1024L * 1024L));

            Assert.True(warning.ShouldWarn);
            Assert.Equal(CottonOfflineFolderFreeSpaceWarningKind.LargeFolder, warning.Kind);
            Assert.Equal("Keep large folder offline?", warning.Title);
            Assert.Equal(
                "Projects will use 100 MB on this device. 2 GB is free.",
                warning.Message);
            Assert.Equal("Keep offline", CottonOfflineFolderFreeSpaceWarningText.AcceptAction);
            Assert.Equal("Cancel", CottonOfflineFolderFreeSpaceWarningText.CancelAction);
        }

        [Fact]
        public void Free_space_warning_policy_warns_when_folder_would_leave_low_space()
        {
            CottonOfflineDownloadQueueSnapshot queue = CottonOfflineDownloadQueueSnapshot.Create(
                CreateContent(CreateFile(FirstFileId, "archive.zip", 20L * 1024L * 1024L)));

            CottonOfflineFolderFreeSpaceWarning warning =
                CottonOfflineFolderFreeSpaceWarningPolicy.CreateWarning(
                    queue,
                    CottonDeviceStorageSpaceSnapshot.Available(520L * 1024L * 1024L));

            Assert.True(warning.ShouldWarn);
            Assert.Equal(CottonOfflineFolderFreeSpaceWarningKind.LowFreeSpaceAfterDownload, warning.Kind);
            Assert.Equal("Free device space will be low", warning.Title);
            Assert.Equal(
                "Projects will use 20 MB and may leave only 500 MB free. 520 MB is free now.",
                warning.Message);
        }

        [Fact]
        public void Free_space_warning_policy_warns_when_folder_exceeds_reported_space()
        {
            CottonOfflineDownloadQueueSnapshot queue = CottonOfflineDownloadQueueSnapshot.Create(
                CreateContent(CreateFile(FirstFileId, "archive.zip", 2L * 1024L * 1024L * 1024L)));

            CottonOfflineFolderFreeSpaceWarning warning =
                CottonOfflineFolderFreeSpaceWarningPolicy.CreateWarning(
                    queue,
                    CottonDeviceStorageSpaceSnapshot.Available(1L * 1024L * 1024L * 1024L));

            Assert.True(warning.ShouldWarn);
            Assert.Equal(CottonOfflineFolderFreeSpaceWarningKind.NotEnoughFreeSpace, warning.Kind);
            Assert.Equal("Device storage may be full", warning.Title);
            Assert.Equal(
                "Projects needs 2 GB, but this device reports 1 GB free. The download may fail.",
                warning.Message);
        }

        [Fact]
        public void Free_space_warning_policy_warns_for_large_folder_when_space_is_unknown()
        {
            CottonOfflineDownloadQueueSnapshot queue = CottonOfflineDownloadQueueSnapshot.Create(
                CreateContent(CreateFile(
                    FirstFileId,
                    "movie.mp4",
                    CottonOfflineFolderFreeSpaceWarningPolicy.LargeFolderWarningBytes)));

            CottonOfflineFolderFreeSpaceWarning warning =
                CottonOfflineFolderFreeSpaceWarningPolicy.CreateWarning(
                    queue,
                    CottonDeviceStorageSpaceSnapshot.Unavailable("Not mounted."));

            Assert.True(warning.ShouldWarn);
            Assert.Equal(CottonOfflineFolderFreeSpaceWarningKind.UnknownFreeSpaceForLargeFolder, warning.Kind);
            Assert.Equal(
                "Projects will use 100 MB. Free device space could not be checked.",
                warning.Message);
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
                previewHashEncryptedHex: null,
                eTag: null);
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
                previewHashEncryptedHex: null,
                eTag: null);
        }
    }
}
