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
        public void Single_file_selection_uses_singular_action_labels()
        {
            CottonFileBrowserEntry file = CreateFile("notes.txt")
                .WithLocalFile(new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt));

            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create([file]);

            Assert.Equal("Copy link", selection.GetAction(CottonFileBulkActionKind.CopyLinks).Label);
            Assert.Equal("Share link", selection.GetAction(CottonFileBulkActionKind.ShareLinks).Label);
            Assert.Equal("Download file", selection.GetAction(CottonFileBulkActionKind.DownloadFiles).Label);
            Assert.Equal("Share file", selection.GetAction(CottonFileBulkActionKind.ShareLocalFiles).Label);
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

        [Fact]
        public void Selection_action_sheet_exposes_multi_file_download_and_link_actions()
        {
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create(
            [
                CreateFile("alpha.txt"),
                CreateFile("beta.txt"),
            ]);

            IReadOnlyList<CottonFileBulkActionSnapshot> actions =
                CottonFileSelectionActionSheet.CreateActions(selection);

            Assert.Equal(
                new[]
                {
                    CottonFileBulkActionKind.CopyLinks,
                    CottonFileBulkActionKind.ShareLinks,
                    CottonFileBulkActionKind.DownloadFiles,
                    CottonFileBulkActionKind.KeepOffline,
                },
                actions.Select(action => action.Kind));
            Assert.Equal("Download files", actions.Single(action => action.Kind == CottonFileBulkActionKind.DownloadFiles).Label);
            Assert.Equal("Keep offline", actions.Single(action => action.Kind == CottonFileBulkActionKind.KeepOffline).Label);
        }

        [Fact]
        public void Selection_action_sheet_exposes_mixed_selection_keep_offline()
        {
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create(
            [
                CreateFile("alpha.txt"),
                CreateFolder("Projects"),
            ]);

            IReadOnlyList<CottonFileBulkActionSnapshot> actions =
                CottonFileSelectionActionSheet.CreateActions(selection);

            Assert.Equal(
                new[]
                {
                    CottonFileBulkActionKind.CopyLinks,
                    CottonFileBulkActionKind.ShareLinks,
                    CottonFileBulkActionKind.KeepOffline,
                },
                actions.Select(action => action.Kind));
        }

        [Fact]
        public void Selection_action_sheet_exposes_single_folder_keep_offline()
        {
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create(
            [
                CreateFolder("Projects"),
            ]);

            IReadOnlyList<CottonFileBulkActionSnapshot> actions =
                CottonFileSelectionActionSheet.CreateActions(selection);

            Assert.Equal(
                new[]
                {
                    CottonFileBulkActionKind.CopyLinks,
                    CottonFileBulkActionKind.ShareLinks,
                    CottonFileBulkActionKind.KeepOffline,
                },
                actions.Select(action => action.Kind));
        }

        [Fact]
        public void Selection_action_sheet_exposes_multi_folder_keep_offline()
        {
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create(
            [
                CreateFolder("Projects"),
                CreateFolder("Archive"),
            ]);

            IReadOnlyList<CottonFileBulkActionSnapshot> actions =
                CottonFileSelectionActionSheet.CreateActions(selection);

            Assert.Equal(
                new[]
                {
                    CottonFileBulkActionKind.CopyLinks,
                    CottonFileBulkActionKind.ShareLinks,
                    CottonFileBulkActionKind.KeepOffline,
                },
                actions.Select(action => action.Kind));
        }

        [Fact]
        public void Selection_action_sheet_keeps_single_local_file_actions_visible()
        {
            CottonFileBrowserEntry file = CreateFile("notes.txt")
                .WithLocalFile(new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt));
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create([file]);

            IReadOnlyList<CottonFileBulkActionSnapshot> actions =
                CottonFileSelectionActionSheet.CreateActions(selection);

            Assert.Contains(actions, action => action.Kind == CottonFileBulkActionKind.DownloadFiles);
            Assert.Contains(actions, action => action.Kind == CottonFileBulkActionKind.KeepOffline);
            Assert.Contains(actions, action => action.Kind == CottonFileBulkActionKind.RemoveOffline);
            Assert.Contains(actions, action => action.Kind == CottonFileBulkActionKind.ShareLocalFiles);
        }

        [Fact]
        public void Selection_action_sheet_exposes_multi_file_remove_offline_when_any_file_is_local()
        {
            CottonFileBrowserEntry local = CreateFile("notes.txt")
                .WithLocalFile(new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt));
            CottonFileBrowserEntry remote = CreateFile("remote.txt");
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create([local, remote]);

            IReadOnlyList<CottonFileBulkActionSnapshot> actions =
                CottonFileSelectionActionSheet.CreateActions(selection);

            Assert.Contains(actions, action => action.Kind == CottonFileBulkActionKind.RemoveOffline);
            Assert.Equal("Remove offline", actions.Single(action => action.Kind == CottonFileBulkActionKind.RemoveOffline).Label);
        }

        [Fact]
        public void Selection_action_sheet_exposes_multi_file_share_when_all_files_are_local()
        {
            CottonFileBrowserEntry first = CreateFile("notes.txt")
                .WithLocalFile(new CottonLocalFileSnapshot("notes.txt", 42, UpdatedAt));
            CottonFileBrowserEntry second = CreateFile("image.png")
                .WithLocalFile(new CottonLocalFileSnapshot("image.png", 100, UpdatedAt));
            CottonFileSelectionSnapshot selection = CottonFileSelectionSnapshot.Create([first, second]);

            IReadOnlyList<CottonFileBulkActionSnapshot> actions =
                CottonFileSelectionActionSheet.CreateActions(selection);

            Assert.Contains(actions, action => action.Kind == CottonFileBulkActionKind.ShareLocalFiles);
            Assert.Equal("Share files", actions.Single(action => action.Kind == CottonFileBulkActionKind.ShareLocalFiles).Label);
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
