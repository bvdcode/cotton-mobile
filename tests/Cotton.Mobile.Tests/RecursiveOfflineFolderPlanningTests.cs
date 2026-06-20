using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RecursiveOfflineFolderPlanningTests
    {
        private static readonly Guid RootFolderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid ArchiveFolderId = Guid.Parse("bbbbbbbb-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid NestedFolderId = Guid.Parse("cccccccc-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid FirstFileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid ThirdFileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Plan_create_counts_loaded_nested_folders()
        {
            CottonOfflineFolderTreeContent tree = CreateLoadedTree();

            CottonRecursiveOfflineFolderPlanSnapshot plan =
                CottonRecursiveOfflineFolderPlanSnapshot.Create(tree);

            Assert.Equal(RootFolderId, plan.FolderId);
            Assert.Equal("Projects", plan.FolderName);
            Assert.Equal(3, plan.FileCount);
            Assert.Equal(2, plan.FolderCount);
            Assert.Equal(6144, plan.KnownSizeBytes);
            Assert.Equal(0, plan.UnknownSizeFileCount);
            Assert.Equal(0, plan.MissingFolderContentCount);
            Assert.True(plan.HasExactSize);
            Assert.True(plan.CanQueueRecursiveFiles);
            Assert.Equal(CottonRecursiveOfflineFolderPlanStatus.Ready, plan.Status);
        }

        [Fact]
        public void Plan_create_marks_missing_child_folder_content()
        {
            CottonOfflineFolderTreeContent tree = CottonOfflineFolderTreeContent.Create(
                CreateContent(
                    RootFolderId,
                    "Projects",
                    CreateFile(FirstFileId, "root.txt", 1024),
                    CreateFolder(ArchiveFolderId, "Archive")));

            CottonRecursiveOfflineFolderPlanSnapshot plan =
                CottonRecursiveOfflineFolderPlanSnapshot.Create(tree);

            Assert.Equal(1, plan.FileCount);
            Assert.Equal(1, plan.FolderCount);
            Assert.Equal(1, plan.MissingFolderContentCount);
            Assert.False(plan.CanQueueRecursiveFiles);
            Assert.Equal(CottonRecursiveOfflineFolderPlanStatus.NeedsFolderScan, plan.Status);
        }

        [Fact]
        public void Plan_create_marks_unknown_nested_file_size()
        {
            CottonOfflineFolderTreeContent tree = CottonOfflineFolderTreeContent.Create(
                CreateContent(
                    RootFolderId,
                    "Projects",
                    CreateFolder(ArchiveFolderId, "Archive")),
                CottonOfflineFolderTreeContent.Create(CreateContent(
                    ArchiveFolderId,
                    "Archive",
                    CreateFile(SecondFileId, "unknown.bin", sizeBytes: null))));

            CottonRecursiveOfflineFolderPlanSnapshot plan =
                CottonRecursiveOfflineFolderPlanSnapshot.Create(tree);

            Assert.Equal(1, plan.FileCount);
            Assert.Equal(1, plan.FolderCount);
            Assert.Equal(1, plan.UnknownSizeFileCount);
            Assert.False(plan.HasExactSize);
            Assert.Equal(CottonRecursiveOfflineFolderPlanStatus.HasUnknownSize, plan.Status);
        }

        [Fact]
        public void Plan_create_marks_loaded_empty_tree()
        {
            CottonOfflineFolderTreeContent tree = CottonOfflineFolderTreeContent.Create(
                CreateContent(
                    RootFolderId,
                    "Projects",
                    CreateFolder(ArchiveFolderId, "Archive")),
                CottonOfflineFolderTreeContent.Create(CreateContent(ArchiveFolderId, "Archive")));

            CottonRecursiveOfflineFolderPlanSnapshot plan =
                CottonRecursiveOfflineFolderPlanSnapshot.Create(tree);

            Assert.Equal(0, plan.FileCount);
            Assert.Equal(1, plan.FolderCount);
            Assert.Equal(0, plan.MissingFolderContentCount);
            Assert.Equal(CottonRecursiveOfflineFolderPlanStatus.Empty, plan.Status);
        }

        [Fact]
        public void Status_text_describes_recursive_plan_states()
        {
            CottonRecursiveOfflineFolderPlanSnapshot ready =
                CottonRecursiveOfflineFolderPlanSnapshot.Create(CreateLoadedTree());
            CottonRecursiveOfflineFolderPlanSnapshot missing =
                CottonRecursiveOfflineFolderPlanSnapshot.Create(CottonOfflineFolderTreeContent.Create(
                    CreateContent(
                        RootFolderId,
                        "Projects",
                        CreateFile(FirstFileId, "root.txt", 1024),
                        CreateFolder(ArchiveFolderId, "Archive"))));
            CottonRecursiveOfflineFolderPlanSnapshot unknownSize =
                CottonRecursiveOfflineFolderPlanSnapshot.Create(CottonOfflineFolderTreeContent.Create(
                    CreateContent(
                        RootFolderId,
                        "Projects",
                        CreateFile(FirstFileId, "unknown.bin", sizeBytes: null))));
            CottonRecursiveOfflineFolderPlanSnapshot empty =
                CottonRecursiveOfflineFolderPlanSnapshot.Create(CottonOfflineFolderTreeContent.Create(
                    CreateContent(RootFolderId, "Projects")));

            Assert.Equal(
                "Scanning nested folders for offline use...",
                CottonRecursiveOfflineFolderStatusText.ScanningStatus);
            Assert.Equal(
                "Projects: 3 files, 2 folders, 6 KB. Recursive offline folder plan is ready.",
                CottonRecursiveOfflineFolderStatusText.CreatePlanStatus(ready));
            Assert.Equal(
                "Cached estimate: Projects: 1 file, 1 folder needs scanning, 1 KB.",
                CottonRecursiveOfflineFolderStatusText.CreatePlanStatus(missing, isCachedEstimate: true));
            Assert.Equal(
                "Projects: 1 file, 0 folders, size unknown. Offline folder needs exact file sizes.",
                CottonRecursiveOfflineFolderStatusText.CreatePlanStatus(unknownSize));
            Assert.Equal(
                "Projects has no files to keep offline.",
                CottonRecursiveOfflineFolderStatusText.CreatePlanStatus(empty));
        }

        [Fact]
        public void Tree_content_rejects_unmatched_child_folder()
        {
            CottonOfflineFolderTreeContent child = CottonOfflineFolderTreeContent.Create(
                CreateContent(ArchiveFolderId, "Archive"));

            Assert.Throws<ArgumentException>(
                () => CottonOfflineFolderTreeContent.Create(
                    CreateContent(RootFolderId, "Projects"),
                    child));
        }

        [Fact]
        public void Recursive_queue_orders_all_loaded_tree_files_by_name()
        {
            CottonOfflineDownloadQueueSnapshot queue =
                CottonOfflineDownloadQueueSnapshot.Create(CreateLoadedTree());

            Assert.Equal(RootFolderId, queue.FolderId);
            Assert.Equal("Projects", queue.FolderName);
            Assert.Equal(3, queue.TotalCount);
            Assert.Equal(6144, queue.TotalSizeBytes);
            Assert.Equal(SecondFileId, queue.Items[0].FileId);
            Assert.Equal("archive.txt", queue.Items[0].FileName);
            Assert.Equal(ThirdFileId, queue.Items[1].FileId);
            Assert.Equal("nested.txt", queue.Items[1].FileName);
            Assert.Equal(FirstFileId, queue.Items[2].FileId);
            Assert.Equal("root.txt", queue.Items[2].FileName);
        }

        [Fact]
        public void Recursive_queue_rejects_incomplete_tree()
        {
            CottonOfflineFolderTreeContent tree = CottonOfflineFolderTreeContent.Create(
                CreateContent(
                    RootFolderId,
                    "Projects",
                    CreateFolder(ArchiveFolderId, "Archive")));

            Assert.Throws<InvalidOperationException>(() => CottonOfflineDownloadQueueSnapshot.Create(tree));
        }

        [Fact]
        public void Plan_rejects_invalid_missing_folder_count()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CottonRecursiveOfflineFolderPlanSnapshot(
                    RootFolderId,
                    "Projects",
                    fileCount: 1,
                    folderCount: 1,
                    knownSizeBytes: 1024,
                    unknownSizeFileCount: 0,
                    missingFolderContentCount: 2));
        }

        private static CottonOfflineFolderTreeContent CreateLoadedTree()
        {
            return CottonOfflineFolderTreeContent.Create(
                CreateContent(
                    RootFolderId,
                    "Projects",
                    CreateFile(FirstFileId, "root.txt", 1024),
                    CreateFolder(ArchiveFolderId, "Archive")),
                CottonOfflineFolderTreeContent.Create(
                    CreateContent(
                        ArchiveFolderId,
                        "Archive",
                        CreateFile(SecondFileId, "archive.txt", 2048),
                        CreateFolder(NestedFolderId, "Nested")),
                    CottonOfflineFolderTreeContent.Create(CreateContent(
                        NestedFolderId,
                        "Nested",
                        CreateFile(ThirdFileId, "nested.txt", 3072)))));
        }

        private static CottonFolderContent CreateContent(
            Guid folderId,
            string folderName,
            params CottonFileBrowserEntry[] entries)
        {
            return new CottonFolderContent(folderId, folderName, entries);
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

        private static CottonFileBrowserEntry CreateFolder(Guid id, string name)
        {
            return CottonFileBrowserEntry.CreateCached(
                id,
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
