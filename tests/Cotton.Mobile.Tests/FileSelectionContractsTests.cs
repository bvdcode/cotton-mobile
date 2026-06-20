using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileSelectionContractsTests
    {
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 16, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Empty_selection_has_no_bulk_actions()
        {
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create([]);

            Assert.False(selection.IsActive);
            Assert.Equal(0, selection.Count);
            Assert.Equal("0 selected", selection.TitleText);
            Assert.Equal(string.Empty, selection.DetailText);
            Assert.Empty(selection.Actions);
        }

        [Fact]
        public void Selection_deduplicates_entries_and_counts_file_folder_mix()
        {
            CottonFileBrowserEntry file = CreateFile("report.pdf");
            CottonFileBrowserEntry folder = CreateFolder("Projects");

            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create([file, folder, file]);

            Assert.True(selection.IsActive);
            Assert.True(selection.HasMixedTypes);
            Assert.Equal(2, selection.Count);
            Assert.Equal(1, selection.FileCount);
            Assert.Equal(1, selection.FolderCount);
            Assert.Equal("2 selected", selection.TitleText);
            Assert.Equal("1 file · 1 folder", selection.DetailText);
        }

        [Fact]
        public void File_only_selection_enables_link_download_and_keep_offline_actions()
        {
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create(
            [
                CreateFile("report.pdf"),
                CreateFile("photo.jpg"),
            ]);

            Assert.True(selection.GetAction(CottonFileBulkActionKind.CopyLinks).IsEnabled);
            Assert.True(selection.GetAction(CottonFileBulkActionKind.ShareLinks).IsEnabled);
            Assert.True(selection.GetAction(CottonFileBulkActionKind.DownloadFiles).IsEnabled);
            Assert.True(selection.GetAction(CottonFileBulkActionKind.KeepOffline).IsEnabled);
            Assert.False(selection.GetAction(CottonFileBulkActionKind.ShareLocalFiles).IsEnabled);
            Assert.Equal(
                "Download files before sharing them.",
                selection.GetAction(CottonFileBulkActionKind.ShareLocalFiles).DisabledReason);
            Assert.Equal("2 files", selection.DetailText);
        }

        [Fact]
        public void Mixed_selection_keeps_link_and_offline_actions_but_blocks_file_only_actions()
        {
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create(
            [
                CreateFile("report.pdf"),
                CreateFolder("Projects"),
            ]);

            Assert.True(selection.GetAction(CottonFileBulkActionKind.CopyLinks).IsEnabled);
            Assert.True(selection.GetAction(CottonFileBulkActionKind.ShareLinks).IsEnabled);
            Assert.True(selection.GetAction(CottonFileBulkActionKind.KeepOffline).IsEnabled);
            Assert.False(selection.GetAction(CottonFileBulkActionKind.DownloadFiles).IsEnabled);
            Assert.False(selection.GetAction(CottonFileBulkActionKind.RemoveOffline).IsEnabled);
            Assert.False(selection.GetAction(CottonFileBulkActionKind.ShareLocalFiles).IsEnabled);
            Assert.Equal(
                "Select only files for this action.",
                selection.GetAction(CottonFileBulkActionKind.DownloadFiles).DisabledReason);
        }

        [Fact]
        public void Local_file_selection_can_share_and_remove_offline()
        {
            CottonFileBrowserEntry file = CreateFile("notes.txt")
                .WithLocalFile(new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt));

            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create([file]);

            Assert.Equal(1, selection.LocalFileCount);
            Assert.True(selection.GetAction(CottonFileBulkActionKind.ShareLocalFiles).IsEnabled);
            Assert.True(selection.GetAction(CottonFileBulkActionKind.RemoveOffline).IsEnabled);
            Assert.Equal(string.Empty, selection.GetAction(CottonFileBulkActionKind.ShareLocalFiles).DisabledReason);
            Assert.Equal(string.Empty, selection.GetAction(CottonFileBulkActionKind.RemoveOffline).DisabledReason);
        }

        [Fact]
        public void Stale_offline_file_selection_can_remove_offline_but_not_share_local_file()
        {
            CottonFileBrowserEntry file = CreateFile("notes.txt");
            CottonOfflineFilePinSnapshot pin = CottonOfflineFilePinSnapshot.Create(file, UpdatedAt);
            CottonFileBrowserEntry stale = file.WithOfflineAvailability(
                CottonOfflineFileAvailabilitySnapshot.Create(
                    file,
                    pin,
                    new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt.AddMinutes(-1))));

            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create([stale]);

            Assert.Equal(1, selection.OfflineAttentionCount);
            Assert.True(selection.GetAction(CottonFileBulkActionKind.RemoveOffline).IsEnabled);
            Assert.False(selection.GetAction(CottonFileBulkActionKind.ShareLocalFiles).IsEnabled);
        }

        [Fact]
        public void Fresh_non_local_file_selection_cannot_remove_offline()
        {
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create([CreateFile("report.pdf")]);

            Assert.False(selection.GetAction(CottonFileBulkActionKind.RemoveOffline).IsEnabled);
            Assert.Equal(
                "No selected files are stored on this device.",
                selection.GetAction(CottonFileBulkActionKind.RemoveOffline).DisabledReason);
        }

        private static CottonFileBrowserEntry CreateFile(string name)
        {
            return CottonFileBrowserEntry.CreateFile(
                Guid.NewGuid(),
                name,
                UpdatedAt,
                42,
                null,
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
