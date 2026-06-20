using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class OfflineFolderPlanningTests
    {
        private static readonly Guid FolderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly DateTime UpdatedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Plan_create_marks_file_only_folder_ready_with_exact_size()
        {
            CottonFolderContent content = CreateContent(
                CreateFile("notes.txt", 1024),
                CreateFile("photo.jpg", 2048));

            CottonOfflineFolderPlanSnapshot plan = CottonOfflineFolderPlanSnapshot.Create(content);

            Assert.Equal(FolderId, plan.FolderId);
            Assert.Equal("Projects", plan.FolderName);
            Assert.Equal(2, plan.FileCount);
            Assert.Equal(0, plan.FolderCount);
            Assert.Equal(3072, plan.KnownSizeBytes);
            Assert.Equal(0, plan.UnknownSizeFileCount);
            Assert.True(plan.HasExactSize);
            Assert.True(plan.CanQueueDirectFiles);
            Assert.Equal(CottonOfflineFolderPlanStatus.Ready, plan.Status);
        }

        [Fact]
        public void Plan_create_marks_empty_folder()
        {
            CottonOfflineFolderPlanSnapshot plan = CottonOfflineFolderPlanSnapshot.Create(CreateContent());

            Assert.Equal(CottonOfflineFolderPlanStatus.Empty, plan.Status);
            Assert.False(plan.CanQueueDirectFiles);
        }

        [Fact]
        public void Plan_create_marks_folders_as_recursive_planning_needed()
        {
            CottonFolderContent content = CreateContent(
                CreateFile("notes.txt", 1024),
                CreateFolder("Archive"));

            CottonOfflineFolderPlanSnapshot plan = CottonOfflineFolderPlanSnapshot.Create(content);

            Assert.Equal(1, plan.FileCount);
            Assert.Equal(1, plan.FolderCount);
            Assert.Equal(CottonOfflineFolderPlanStatus.ContainsFolders, plan.Status);
            Assert.False(plan.CanQueueDirectFiles);
        }

        [Fact]
        public void Plan_create_marks_unknown_size_files()
        {
            CottonFolderContent content = CreateContent(
                CreateFile("known.txt", 1024),
                CreateFile("unknown.bin", sizeBytes: null));

            CottonOfflineFolderPlanSnapshot plan = CottonOfflineFolderPlanSnapshot.Create(content);

            Assert.Equal(2, plan.FileCount);
            Assert.Equal(1024, plan.KnownSizeBytes);
            Assert.Equal(1, plan.UnknownSizeFileCount);
            Assert.False(plan.HasExactSize);
            Assert.Equal(CottonOfflineFolderPlanStatus.HasUnknownSize, plan.Status);
        }

        [Fact]
        public void Status_text_describes_folder_offline_plan_states()
        {
            CottonOfflineFolderPlanSnapshot ready = CottonOfflineFolderPlanSnapshot.Create(CreateContent(
                CreateFile("notes.txt", 1024),
                CreateFile("photo.jpg", 2048)));
            CottonOfflineFolderPlanSnapshot empty = CottonOfflineFolderPlanSnapshot.Create(CreateContent());
            CottonOfflineFolderPlanSnapshot recursive = CottonOfflineFolderPlanSnapshot.Create(CreateContent(
                CreateFile("notes.txt", 1024),
                CreateFolder("Archive")));
            CottonOfflineFolderPlanSnapshot unknownSize = CottonOfflineFolderPlanSnapshot.Create(CreateContent(
                CreateFile("unknown.bin", sizeBytes: null)));

            Assert.Equal(
                "Checking Projects for offline use...",
                CottonOfflineFolderStatusText.CreateStartingStatus(" Projects "));
            Assert.Equal(
                "Projects: 2 files, 3 KB. Ready to keep offline.",
                CottonOfflineFolderStatusText.CreatePlanStatus(ready));
            Assert.Equal(
                "Cached estimate: Projects: 2 files, 3 KB. Connect to keep offline.",
                CottonOfflineFolderStatusText.CreatePlanStatus(ready, isCachedEstimate: true));
            Assert.Equal(
                "Projects has no files to keep offline.",
                CottonOfflineFolderStatusText.CreatePlanStatus(empty));
            Assert.Equal(
                "Projects: 1 file, 1 folder, 1 KB. Nested folders need scanning before offline download.",
                CottonOfflineFolderStatusText.CreatePlanStatus(recursive));
            Assert.Equal(
                "Projects: 1 file, size unknown. Offline folder needs exact file sizes.",
                CottonOfflineFolderStatusText.CreatePlanStatus(unknownSize));
            Assert.Equal("Offline. Folder offline needs internet.", CottonOfflineFolderStatusText.OfflineUnavailableStatus);
            Assert.Equal("Keep folder offline cancelled.", CottonOfflineFolderStatusText.CancelledStatus);
            Assert.Equal("Could not plan folder offline.", CottonOfflineFolderStatusText.FailedStatus);
        }

        [Fact]
        public void Plan_rejects_invalid_counts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonOfflineFolderPlanSnapshot(FolderId, "Projects", 1, 0, 0, 2));
        }

        private static CottonFolderContent CreateContent(params CottonFileBrowserEntry[] entries)
        {
            return new CottonFolderContent(FolderId, "Projects", entries);
        }

        private static CottonFileBrowserEntry CreateFile(string name, long? sizeBytes)
        {
            return CottonFileBrowserEntry.CreateCached(
                Guid.NewGuid(),
                CottonFileBrowserEntryType.File,
                name,
                "File",
                "File",
                "More",
                "FILE",
                UpdatedAt,
                sizeBytes,
                contentType: null,
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
