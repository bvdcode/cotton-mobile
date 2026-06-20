using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncConflictDisplayStateTests
    {
        private static readonly Guid JournalItemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid SyncRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid CloudFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid FileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 13, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Ready_detection_keeps_conflict_ui_hidden()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");
            CottonSyncConflictDetectionSnapshot detection = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                CreateFile("\"etag-1\""));

            CottonSyncConflictDisplayState display = CottonSyncConflictDisplayState.Create(item, detection);

            Assert.False(display.IsVisible);
            Assert.False(display.IsBlocking);
            Assert.Empty(display.Actions);
        }

        [Fact]
        public void Delete_noop_keeps_conflict_ui_hidden()
        {
            CottonSyncJournalItemSnapshot item = CreateDeleteJournalItem("\"etag-1\"");
            CottonSyncConflictDetectionSnapshot detection = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                latestRemoteEntry: null);

            CottonSyncConflictDisplayState display = CottonSyncConflictDisplayState.Create(item, detection);

            Assert.False(display.IsVisible);
            Assert.True(detection.CanCompleteWithoutServerMutation);
        }

        [Fact]
        public void Missing_etag_prompts_refresh()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");
            CottonSyncConflictDetectionSnapshot detection = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                CreateFile(eTag: null));

            CottonSyncConflictDisplayState display = CottonSyncConflictDisplayState.Create(item, detection);

            Assert.True(display.IsVisible);
            Assert.True(display.IsBlocking);
            Assert.Equal("Refresh needed", display.Title);
            Assert.Contains("notes.txt", display.DetailText, StringComparison.Ordinal);
            CottonSyncConflictActionSnapshot action = Assert.Single(display.Actions);
            Assert.Equal(CottonSyncConflictResolutionAction.Refresh, action.Action);
            Assert.True(action.IsPrimary);
            Assert.False(action.IsDestructive);
        }

        [Fact]
        public void Changed_remote_revision_offers_cloud_or_local_choice()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");
            CottonSyncConflictDetectionSnapshot detection = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                CreateFile("\"etag-2\""));

            CottonSyncConflictDisplayState display = CottonSyncConflictDisplayState.Create(item, detection);

            Assert.True(display.IsVisible);
            Assert.Equal("File changed elsewhere", display.Title);
            Assert.Equal(
                [
                    CottonSyncConflictResolutionAction.UseCloudVersion,
                    CottonSyncConflictResolutionAction.KeepLocalChange,
                ],
                display.Actions.Select(action => action.Action).ToArray());
            Assert.True(display.Actions[0].IsPrimary);
            Assert.DoesNotContain(display.Actions, action => action.IsDestructive);
        }

        [Fact]
        public void Missing_remote_target_for_rename_offers_skip_or_keep_local()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");
            CottonSyncConflictDetectionSnapshot detection = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                latestRemoteEntry: null);

            CottonSyncConflictDisplayState display = CottonSyncConflictDisplayState.Create(item, detection);

            Assert.True(display.IsVisible);
            Assert.Equal("File missing in cloud", display.Title);
            Assert.Equal(CottonSyncConflictResolutionAction.SkipLocalChange, display.Actions[0].Action);
            Assert.True(display.Actions[0].IsDestructive);
            Assert.Equal(CottonSyncConflictResolutionAction.KeepLocalChange, display.Actions[1].Action);
        }

        [Fact]
        public void Folder_revision_gap_shows_blocked_folder_state()
        {
            CottonSyncJournalItemSnapshot item = CottonSyncJournalItemSnapshot.Restore(
                JournalItemId,
                SyncRootId,
                CreateReadyRoot().StableKey,
                sequence: 1,
                CottonSyncJournalOperation.Rename,
                CottonFileBrowserEntryType.Folder,
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                "Projects",
                CottonSyncJournalOrigin.Device,
                CottonSyncJournalStatus.Pending,
                attemptCount: 0,
                failureMessage: null,
                expectedETag: null,
                normalizedName: "Renamed Projects",
                targetParentId: null,
                deleteMode: null,
                UpdatedAt,
                UpdatedAt);
            CottonSyncConflictDetectionSnapshot detection = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                latestRemoteEntry: null);

            CottonSyncConflictDisplayState display = CottonSyncConflictDisplayState.Create(item, detection);

            Assert.True(display.IsVisible);
            Assert.Equal("Folder sync blocked", display.Title);
            Assert.Equal(CottonSyncConflictResolutionAction.Dismiss, Assert.Single(display.Actions).Action);
        }

        [Fact]
        public void Root_not_ready_prompts_reconnect()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");
            CottonSyncConflictDetectionSnapshot detection = CottonSyncConflictDetector.Create(
                CreateRoot(CottonSyncRootPermissionStatus.Unavailable),
                item,
                CreateFile("\"etag-1\""));

            CottonSyncConflictDisplayState display = CottonSyncConflictDisplayState.Create(item, detection);

            Assert.True(display.IsVisible);
            Assert.Equal("Sync paused", display.Title);
            Assert.Equal(CottonSyncConflictResolutionAction.ReconnectLocalRoot, Assert.Single(display.Actions).Action);
        }

        private static CottonSyncJournalItemSnapshot CreateRenameJournalItem(string? expectedETag)
        {
            CottonFileBrowserEntry file = CreateFile(expectedETag);
            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                file,
                "renamed.txt",
                ["notes.txt"]);

            return CottonSyncJournalItemSnapshot.CreateRenameMove(
                JournalItemId,
                CreateReadyRoot(),
                file,
                semantics,
                CottonSyncJournalOrigin.Device,
                sequence: 1,
                UpdatedAt);
        }

        private static CottonSyncJournalItemSnapshot CreateDeleteJournalItem(string? expectedETag)
        {
            CottonFileBrowserEntry file = CreateFile(expectedETag);
            CottonSyncDeleteSemanticsSnapshot semantics = CottonSyncDeleteSemantics.Create(
                file,
                CottonSyncDeleteMode.MoveToTrash);

            return CottonSyncJournalItemSnapshot.CreateDelete(
                JournalItemId,
                CreateReadyRoot(),
                file,
                semantics,
                CottonSyncJournalOrigin.Device,
                sequence: 1,
                UpdatedAt);
        }

        private static CottonSyncRootSnapshot CreateReadyRoot()
        {
            return CreateRoot(CottonSyncRootPermissionStatus.Available);
        }

        private static CottonSyncRootSnapshot CreateRoot(CottonSyncRootPermissionStatus permissionStatus)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                new Uri("https://app.cottoncloud.dev"),
                "account-1",
                new CottonUploadDestinationSnapshot(
                    CloudFolderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    "app-private-sync-root",
                    "On this device",
                    permissionStatus),
                CottonSyncDirection.Bidirectional);
        }

        private static CottonFileBrowserEntry CreateFile(string? eTag)
        {
            return CottonFileBrowserEntry.CreateCached(
                FileId,
                CottonFileBrowserEntryType.File,
                "notes.txt",
                "Text",
                "42 B · Text",
                "More",
                "TXT",
                UpdatedAt,
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag);
        }
    }
}
