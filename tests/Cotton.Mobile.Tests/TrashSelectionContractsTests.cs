using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TrashSelectionContractsTests
    {
        private static readonly DateTime UpdatedAt = new(2026, 6, 21, 22, 30, 0, DateTimeKind.Utc);

        [Fact]
        public void Empty_trash_selection_has_no_bulk_actions()
        {
            CottonTrashSelectionSnapshot selection = CottonTrashSelectionSnapshot.Create([]);

            Assert.False(selection.IsActive);
            Assert.Equal(0, selection.Count);
            Assert.Equal("0 selected", selection.TitleText);
            Assert.Equal(string.Empty, selection.DetailText);
            Assert.Empty(selection.Actions);
        }

        [Fact]
        public void Trash_selection_deduplicates_entries_and_counts_types()
        {
            CottonFileBrowserEntry file = CreateFile("report.pdf");
            CottonFileBrowserEntry folder = CreateFolder("Archive");

            CottonTrashSelectionSnapshot selection = CottonTrashSelectionSnapshot.Create([file, folder, file]);

            Assert.True(selection.IsActive);
            Assert.True(selection.HasMixedTypes);
            Assert.Equal(2, selection.Count);
            Assert.Equal(1, selection.FileCount);
            Assert.Equal(1, selection.FolderCount);
            Assert.Equal("2 selected", selection.TitleText);
            Assert.Equal("1 file · 1 folder", selection.DetailText);
        }

        [Fact]
        public void Trash_selection_exposes_restore_and_delete_forever_actions_only()
        {
            CottonTrashSelectionSnapshot selection = CottonTrashSelectionSnapshot.Create(
            [
                CreateFile("alpha.txt"),
                CreateFolder("Old project"),
            ]);

            Assert.Equal(
                new[]
                {
                    CottonTrashBulkActionKind.Restore,
                    CottonTrashBulkActionKind.DeleteForever,
                },
                selection.Actions.Select(action => action.Kind));
            Assert.Equal("Restore", selection.GetAction(CottonTrashBulkActionKind.Restore).Label);
            Assert.Equal("Delete forever", selection.GetAction(CottonTrashBulkActionKind.DeleteForever).Label);
        }

        [Fact]
        public void Trash_bulk_status_text_formats_restore_and_delete_flow()
        {
            Assert.Equal(
                "Restore 2 selected items to their original locations?",
                CottonTrashBulkStatusText.CreateRestoreConfirmMessage(1, 1));
            Assert.Equal(
                "Permanently delete 2 selected items? This cannot be undone.",
                CottonTrashBulkStatusText.CreateDeleteForeverConfirmMessage(2, 0));
            Assert.Equal("Restoring 2 selected items...", CottonTrashBulkStatusText.CreateRestoringStatus(2));
            Assert.Equal("Deleting 2 selected items...", CottonTrashBulkStatusText.CreateDeletingStatus(2));
            Assert.Equal(
                "Restoring 1 of 2: report.pdf",
                CottonTrashBulkStatusText.CreateRestoringItemStatus(1, 2, " report.pdf "));
            Assert.Equal(
                "Deleting 2 of 2: item",
                CottonTrashBulkStatusText.CreateDeletingItemStatus(2, 2, " "));
            Assert.Equal("2 selected items restored.", CottonTrashBulkStatusText.CreateRestoredStatus(2));
            Assert.Equal("1 selected item deleted forever.", CottonTrashBulkStatusText.CreateDeletedStatus(1));
            Assert.Equal(
                "1 of 2 selected items restored. Review remaining items.",
                CottonTrashBulkStatusText.CreatePartialRestoreStatus(1, 2));
            Assert.Equal(
                "1 of 2 selected items deleted forever. Review remaining items.",
                CottonTrashBulkStatusText.CreatePartialDeleteStatus(1, 2));
        }

        [Fact]
        public void Trash_bulk_status_text_rejects_invalid_counts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashBulkStatusText.CreateRestoreConfirmMessage(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashBulkStatusText.CreateDeleteForeverConfirmMessage(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashBulkStatusText.CreateRestoringStatus(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashBulkStatusText.CreateDeletingItemStatus(2, 1, "name"));
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashBulkStatusText.CreatePartialRestoreStatus(2, 2));
        }

        private static CottonFileBrowserEntry CreateFile(string name)
        {
            return CottonFileBrowserEntry.CreateFile(
                Guid.NewGuid(),
                name,
                UpdatedAt,
                42,
                "text/plain",
                null,
                "\"etag\"");
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
                null,
                null,
                null,
                null);
        }
    }
}
