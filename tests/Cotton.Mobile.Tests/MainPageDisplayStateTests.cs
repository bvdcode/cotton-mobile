using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MainPageDisplayStateTests
    {
        private static readonly DateTime Older = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Newer = new(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Show_files_sorts_by_name_with_folders_first()
        {
            MainPageDisplayState display = CreateSignedInDisplay();

            display.ShowFiles(
                CreateContent(
                    CreateFile("zeta.txt", "Text", 10, Older),
                    CreateFolder("Projects"),
                    CreateFile("alpha.png", "Image", 200, Newer)),
                isRoot: true,
                canNavigateUp: false,
                path: "Files");

            Assert.Equal(["Projects", "alpha.png", "zeta.txt"], VisibleNames(display));
            Assert.Equal("3 items · A-Z", display.FilesStatus);
            Assert.False(display.IsFilesPathVisible);
            Assert.False(display.IsFileUpButtonVisible);
        }

        [Fact]
        public void Sort_modes_keep_folder_grouping_and_update_status()
        {
            MainPageDisplayState display = CreateDisplayWithMixedFiles();

            display.ShowFileSortMode(CottonFileBrowserSortMode.Updated);

            Assert.Equal(["Projects", "alpha.png", "archive.zip", "zeta.txt"], VisibleNames(display));
            Assert.Equal("4 items · Newest", display.FilesStatus);
            Assert.Equal("New", display.FileSortButtonText);

            display.ShowFileSortMode(CottonFileBrowserSortMode.Type);

            Assert.Equal(["Projects", "archive.zip", "alpha.png", "zeta.txt"], VisibleNames(display));
            Assert.Equal("4 items · Type", display.FilesStatus);

            display.ShowFileSortMode(CottonFileBrowserSortMode.Size);

            Assert.Equal(["Projects", "alpha.png", "zeta.txt", "archive.zip"], VisibleNames(display));
            Assert.Equal("4 items · Size", display.FilesStatus);
        }

        [Fact]
        public void Search_filters_current_folder_and_reports_matches()
        {
            MainPageDisplayState display = CreateDisplayWithMixedFiles();

            display.FileSearchText = "png";

            Assert.Equal(["alpha.png"], VisibleNames(display));
            Assert.True(display.IsFileSearchVisible);
            Assert.True(display.IsFileSearchActive);
            Assert.Equal("1 match · A-Z", display.FilesStatus);
            Assert.False(display.IsFilesEmptyVisible);

            display.FileSearchText = "missing";

            Assert.Empty(display.FileEntries);
            Assert.Equal("No matching files", display.FilesEmptyMessage);
            Assert.Equal("Try another name, type, or extension.", display.FilesEmptyDetails);
            Assert.True(display.IsFilesEmptyVisible);
            Assert.False(display.IsFilesEmptyAddActionVisible);
            Assert.Equal("0 matches · A-Z", display.FilesStatus);
        }

        [Fact]
        public void Empty_folder_has_stable_empty_state()
        {
            MainPageDisplayState display = CreateSignedInDisplay();

            display.ShowFiles(CreateContent(), isRoot: false, canNavigateUp: true, path: "Files / Empty");

            Assert.Empty(display.FileEntries);
            Assert.Equal("Empty", display.FilesTitle);
            Assert.Equal("Files / Empty", display.FilesPath);
            Assert.True(display.IsFilesPathVisible);
            Assert.True(display.IsFileUpButtonEnabled);
            Assert.Equal("This folder is empty", display.FilesEmptyMessage);
            Assert.Equal("Files added here will appear automatically.", display.FilesEmptyDetails);
            Assert.True(display.IsFilesEmptyVisible);
            Assert.True(display.IsFilesEmptyAddActionVisible);
            Assert.False(display.IsFilesStatusVisible);
        }

        [Fact]
        public void Offline_cached_listing_notice_reports_cache_age()
        {
            MainPageDisplayState display = CreateSignedInDisplay();
            display.ShowFiles(
                CreateContent(CreateFile("zeta.txt", "Text", 10, Older)),
                isRoot: true,
                canNavigateUp: false,
                path: "Files");

            display.ShowOfflineFilesNotice(
                isCachedListing: true,
                cachedAtUtc: DateTime.UtcNow.AddHours(-2).AddMinutes(-5));

            Assert.True(display.IsFilesNoticeVisible);
            Assert.Equal("Offline", display.FilesNoticeTitle);
            Assert.Equal(
                "Saved folder list cached 2 hours ago. Files marked On device can still open.",
                display.FilesNoticeMessage);
        }

        [Fact]
        public void Empty_offline_cached_listing_notice_reports_age_without_claiming_files()
        {
            MainPageDisplayState display = CreateSignedInDisplay();
            display.ShowFiles(CreateContent(), isRoot: true, canNavigateUp: false, path: "Files");

            display.ShowOfflineFilesNotice(
                isCachedListing: true,
                cachedAtUtc: DateTime.UtcNow.AddDays(-1).AddHours(-1));

            Assert.Equal(
                "Saved folder list is empty. Cached 1 day ago. Reconnect to refresh.",
                display.FilesNoticeMessage);
        }

        [Fact]
        public void Busy_file_action_preserves_status_and_disables_browser_chrome()
        {
            MainPageDisplayState display = CreateDisplayWithMixedFiles();

            display.ShowFileActionLoading("Downloading zeta.txt... 50%");
            display.FileSearchText = "alpha";

            Assert.Equal(["alpha.png"], VisibleNames(display));
            Assert.Equal("Downloading zeta.txt... 50%", display.FilesStatus);
            Assert.False(display.IsFileBrowserChromeEnabled);
            Assert.False(display.IsAccountActionEnabled);
            Assert.True(display.CanCancelFileAction);

            display.ShowFilesSummary();

            Assert.Equal("1 match · A-Z", display.FilesStatus);
            Assert.True(display.IsFileBrowserChromeEnabled);
            Assert.True(display.IsAccountActionEnabled);
        }

        [Fact]
        public void Refresh_request_keeps_refresh_available_without_enabling_file_actions()
        {
            MainPageDisplayState display = CreateDisplayWithMixedFiles();

            display.IsFilesRefreshing = true;

            Assert.False(display.IsFileBrowserChromeEnabled);
            Assert.True(display.CanRefreshFiles);
            Assert.True(display.IsAccountActionEnabled);

            display.StopFilesRefreshing();

            Assert.True(display.IsFileBrowserChromeEnabled);
            Assert.True(display.CanRefreshFiles);

            display.ShowFileActionLoading("Uploading notes.txt...");

            Assert.False(display.IsFileBrowserChromeEnabled);
            Assert.False(display.CanRefreshFiles);
            Assert.False(display.IsAccountActionEnabled);

            display.ShowFilesLoading("Loading files...");

            Assert.False(display.IsFileBrowserChromeEnabled);
            Assert.False(display.CanRefreshFiles);
        }

        [Fact]
        public void Transfer_activity_indicator_is_visible_only_for_signed_in_active_work()
        {
            MainPageDisplayState display = CreateSignedInDisplay();
            CottonTransferActivityIndicator indicator = CottonTransferActivityIndicator.Create(
                [
                    CottonTransferQueueItem.CreateUpload(
                        Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        "photo.jpg",
                        100,
                        Older),
                ]);

            display.ShowTransferActivity(indicator);

            Assert.True(display.IsTransferActivityIndicatorVisible);
            Assert.Equal("1 transfer waiting", display.TransferActivityIndicator.Text);

            display.ShowSignIn("Signed out.");

            Assert.False(display.IsTransferActivityIndicatorVisible);
            Assert.False(display.TransferActivityIndicator.IsVisible);
        }

        [Fact]
        public void Backup_activity_indicator_is_visible_only_for_signed_in_backup_work()
        {
            MainPageDisplayState display = CreateSignedInDisplay();
            CottonCameraBackupActivityIndicator indicator = CottonCameraBackupActivityIndicator.Create(
                [
                    CreateCameraBackupUpload(),
                ]);

            display.ShowBackupActivity(indicator);

            Assert.True(display.IsBackupActivityIndicatorVisible);
            Assert.Equal("1 backup waiting", display.BackupActivityIndicator.Text);

            display.ShowSignIn("Signed out.");

            Assert.False(display.IsBackupActivityIndicatorVisible);
            Assert.False(display.BackupActivityIndicator.IsVisible);
        }

        [Fact]
        public void Profile_status_can_report_cached_offline_session()
        {
            MainPageDisplayState display = CreateSignedInDisplay();

            display.ShowProfileStatus("Offline. Showing saved files until your session can refresh.");

            Assert.True(display.IsProfileVisible);
            Assert.True(display.IsProfileStatusVisible);
            Assert.Equal("Offline. Showing saved files until your session can refresh.", display.ProfileStatus);
            Assert.Equal("Mobile Demo", display.ProfileName);
            Assert.Equal("app.cottoncloud.dev", display.ProfileInstance);
        }

        [Fact]
        public void Files_shell_navigation_catalog_keeps_compact_entry_order()
        {
            Assert.Equal(
                [
                    CottonFilesShellNavigationDestination.Files,
                    CottonFilesShellNavigationDestination.Transfers,
                    CottonFilesShellNavigationDestination.Inbox,
                    CottonFilesShellNavigationDestination.Backup,
                    CottonFilesShellNavigationDestination.Settings,
                ],
                CottonFilesShellNavigationCatalog.CreateItems().Select(item => item.Destination));
        }

        [Fact]
        public void Signed_in_display_exposes_files_first_navigation_copy()
        {
            MainPageDisplayState display = CreateSignedInDisplay();

            Assert.Equal("Files", display.FilesNavigation.Label);
            Assert.Equal("Transfers", display.TransfersNavigation.Label);
            Assert.Equal("Inbox", display.InboxNavigation.Label);
            Assert.Equal("Backup", display.BackupNavigation.Label);
            Assert.Equal("Settings", display.SettingsNavigation.Label);
            Assert.Equal("Open account and settings", display.SettingsNavigation.AccessibilityText);
        }

        [Fact]
        public void Offline_pack_progress_survives_file_summary_and_clears_on_sign_out()
        {
            MainPageDisplayState display = CreateDisplayWithMixedFiles();
            var progress = new CottonOfflinePackProgressSnapshot(
                CottonOfflinePackProgressStatus.Running,
                "Projects",
                completedCount: 1,
                totalCount: 2,
                completedBytes: 1024,
                totalBytes: 3072,
                currentFileName: "zeta.txt");

            display.ShowOfflinePackProgress(progress);
            display.ShowFilesSummary();

            Assert.True(display.IsOfflinePackProgressVisible);
            Assert.Equal("Keeping Projects offline", display.OfflinePackProgress.Text);
            Assert.Equal("4 items · A-Z", display.FilesStatus);

            display.ShowSignIn("Signed out.");

            Assert.False(display.IsOfflinePackProgressVisible);
            Assert.False(display.OfflinePackProgress.IsVisible);
        }

        [Fact]
        public void Local_file_markers_update_visible_and_backing_entries()
        {
            MainPageDisplayState display = CreateDisplayWithMixedFiles();
            CottonFileBrowserEntry file = display.FileEntries.Single(entry => entry.Name == "zeta.txt");
            var localFile = new CottonLocalFileSnapshot("zeta.txt", 10, Older);

            display.ShowFileLocalCopy(file, localFile);

            CottonFileBrowserEntry marked = display.FileEntries.Single(entry => entry.Name == "zeta.txt");
            Assert.True(marked.HasLocalCopy);
            Assert.Equal("On device", marked.LocalCopyStatus);
            Assert.Equal("10 B · Text · On device", marked.DisplayDetails);

            bool unchanged = display.RefreshFileLocalCopies(entry => entry.Id == file.Id ? localFile : null);
            Assert.False(unchanged);

            bool changed = display.RefreshFileLocalCopies(_ => null);
            Assert.True(changed);
            Assert.False(display.FileEntries.Single(entry => entry.Name == "zeta.txt").HasLocalCopy);
        }

        [Fact]
        public void Local_file_state_refresh_updates_offline_attention_and_fresh_marker()
        {
            MainPageDisplayState display = CreateDisplayWithMixedFiles();
            CottonFileBrowserEntry file = display.FileEntries.Single(entry => entry.Name == "zeta.txt");
            CottonOfflineFilePinSnapshot pin = CottonOfflineFilePinSnapshot.Create(file, Newer);
            var staleLocalFile = new CottonLocalFileSnapshot("zeta.txt", 10, Older.AddSeconds(-3));
            var freshLocalFile = new CottonLocalFileSnapshot("zeta.txt", 10, Older);

            display.RefreshFileLocalStates(entry =>
                entry.Id == file.Id
                    ? entry
                        .WithOfflineAvailability(CottonOfflineFileAvailabilitySnapshot.Create(entry, pin, staleLocalFile))
                        .WithoutLocalFile()
                    : entry);

            CottonFileBrowserEntry stale = display.FileEntries.Single(entry => entry.Name == "zeta.txt");
            Assert.False(stale.HasLocalCopy);
            Assert.True(stale.IsOfflineAttentionVisible);
            Assert.Equal("10 B · Text · Offline stale", stale.DisplayDetails);

            display.RefreshFileLocalStates(entry =>
                entry.Id == file.Id
                    ? entry
                        .WithOfflineAvailability(CottonOfflineFileAvailabilitySnapshot.Create(entry, pin, freshLocalFile))
                        .WithLocalFile(freshLocalFile)
                    : entry);

            CottonFileBrowserEntry fresh = display.FileEntries.Single(entry => entry.Name == "zeta.txt");
            Assert.True(fresh.HasLocalCopy);
            Assert.False(fresh.IsOfflineAttentionVisible);
            Assert.Equal("10 B · Text · On device", fresh.DisplayDetails);
        }

        [Fact]
        public void Preferences_apply_view_mode_and_sort_mode()
        {
            MainPageDisplayState display = CreateDisplayWithMixedFiles();

            display.ApplyFileBrowserPreferences(
                new CottonFileBrowserPreferences(
                    CottonFileBrowserViewMode.Tiles,
                    CottonFileBrowserSortMode.Size));

            Assert.Equal(CottonFileBrowserViewMode.Tiles, display.FileViewMode);
            Assert.True(display.IsFileTileViewVisible);
            Assert.False(display.IsFileListViewVisible);
            Assert.Equal("Size", display.FileSortButtonText);
            Assert.Equal(["Projects", "alpha.png", "zeta.txt", "archive.zip"], VisibleNames(display));
        }

        private static MainPageDisplayState CreateDisplayWithMixedFiles()
        {
            MainPageDisplayState display = CreateSignedInDisplay();
            display.ShowFiles(
                CreateContent(
                    CreateFile("zeta.txt", "Text", 10, Older),
                    CreateFolder("Projects"),
                    CreateFile("archive.zip", "File", null, Older),
                    CreateFile("alpha.png", "Image", 200, Newer)),
                isRoot: true,
                canNavigateUp: false,
                path: "Files");
            return display;
        }

        private static MainPageDisplayState CreateSignedInDisplay()
        {
            var display = new MainPageDisplayState("https://app.cottoncloud.dev");
            display.ShowProfile(new MainPageProfile("Mobile Demo", null, "app.cottoncloud.dev"));
            return display;
        }

        private static CottonFolderContent CreateContent(params CottonFileBrowserEntry[] entries)
        {
            return new CottonFolderContent(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Empty", entries);
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
                Newer,
                null,
                null,
                null,
                eTag: null);
        }

        private static CottonFileBrowserEntry CreateFile(string name, string kind, long? sizeBytes, DateTime updatedAtUtc)
        {
            string details = sizeBytes.HasValue
                ? $"{CottonFileSizeFormatter.Format(sizeBytes.Value)} · {kind}"
                : $"Unknown · {kind}";
            return CottonFileBrowserEntry.CreateCached(
                Guid.NewGuid(),
                CottonFileBrowserEntryType.File,
                name,
                kind,
                details,
                "More",
                kind.ToUpperInvariant(),
                updatedAtUtc,
                sizeBytes,
                null,
                null,
                eTag: null);
        }

        private static CottonTransferQueueItem CreateCameraBackupUpload()
        {
            return CottonTransferQueueItem.CreateUpload(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                "photo.jpg",
                100,
                Older,
                destination: null,
                contentType: "image/jpeg",
                source: new CottonTransferSourceSnapshot(
                    CottonTransferSourceKind.CameraBackup,
                    "content://media/external/images/media/100",
                    Older,
                    100,
                    Older));
        }

        private static string[] VisibleNames(MainPageDisplayState display)
        {
            return display.FileEntries.Select(entry => entry.Name).ToArray();
        }
    }
}
