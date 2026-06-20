using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncConflictDetectionTests
    {
        private static readonly Guid JournalItemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid SyncRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid CloudFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid FileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Matching_file_etag_allows_server_mutation()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");
            CottonFileBrowserEntry latest = CreateFile("\"etag-1\"");

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                latest);

            Assert.Equal(CottonSyncConflictDetectionStatus.Ready, result.Status);
            Assert.True(result.CanExecuteServerMutation);
            Assert.False(result.RequiresConflictResolution);
            Assert.False(result.NeedsFreshListing);
            Assert.Equal("\"etag-1\"", result.ExpectedETag);
            Assert.Equal("\"etag-1\"", result.ActualETag);
        }

        [Fact]
        public void Changed_file_etag_requires_conflict_resolution()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");
            CottonFileBrowserEntry latest = CreateFile("\"etag-2\"");

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                latest);

            Assert.Equal(CottonSyncConflictDetectionStatus.ServerRevisionChanged, result.Status);
            Assert.False(result.CanExecuteServerMutation);
            Assert.True(result.RequiresConflictResolution);
            Assert.True(result.IsBlocked);
            Assert.Equal("\"etag-2\"", result.ActualETag);
        }

        [Fact]
        public void Missing_latest_etag_requires_fresh_listing()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");
            CottonFileBrowserEntry latest = CreateFile(eTag: null);

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                latest);

            Assert.Equal(CottonSyncConflictDetectionStatus.NeedsFreshServerRevision, result.Status);
            Assert.True(result.NeedsFreshListing);
            Assert.False(result.RequiresConflictResolution);
            Assert.True(result.IsBlocked);
        }

        [Fact]
        public void Missing_expected_etag_requires_fresh_listing()
        {
            CottonSyncJournalItemSnapshot item = CottonSyncJournalItemSnapshot.Restore(
                JournalItemId,
                SyncRootId,
                CreateReadyRoot().StableKey,
                sequence: 1,
                CottonSyncJournalOperation.Rename,
                CottonFileBrowserEntryType.File,
                FileId,
                "notes.txt",
                CottonSyncJournalOrigin.Device,
                CottonSyncJournalStatus.Pending,
                attemptCount: 0,
                failureMessage: null,
                expectedETag: null,
                normalizedName: "renamed.txt",
                targetParentId: null,
                deleteMode: null,
                UpdatedAt,
                UpdatedAt);

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                CreateFile("\"etag-1\""));

            Assert.Equal(CottonSyncConflictDetectionStatus.NeedsFreshServerRevision, result.Status);
            Assert.True(result.NeedsFreshListing);
        }

        [Fact]
        public void Missing_remote_target_completes_delete_without_server_mutation()
        {
            CottonSyncJournalItemSnapshot item = CreateDeleteJournalItem("\"etag-1\"");

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                latestRemoteEntry: null);

            Assert.Equal(CottonSyncConflictDetectionStatus.RemoteTargetMissing, result.Status);
            Assert.True(result.CanCompleteWithoutServerMutation);
            Assert.False(result.RequiresConflictResolution);
            Assert.False(result.CanExecuteServerMutation);
        }

        [Fact]
        public void Missing_remote_target_blocks_rename_as_conflict()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                latestRemoteEntry: null);

            Assert.Equal(CottonSyncConflictDetectionStatus.RemoteTargetMissing, result.Status);
            Assert.False(result.CanCompleteWithoutServerMutation);
            Assert.True(result.RequiresConflictResolution);
            Assert.True(result.IsBlocked);
        }

        [Fact]
        public void Wrong_latest_remote_target_blocks_mutation()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");
            CottonFileBrowserEntry latest = CreateFile(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                "\"etag-1\"");

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                latest);

            Assert.Equal(CottonSyncConflictDetectionStatus.RemoteTargetMismatch, result.Status);
            Assert.True(result.RequiresConflictResolution);
        }

        [Fact]
        public void Folder_journal_item_is_unsupported_until_folder_revision_exists()
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

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                latestRemoteEntry: null);

            Assert.Equal(CottonSyncConflictDetectionStatus.FolderRevisionUnsupported, result.Status);
            Assert.False(result.CanExecuteServerMutation);
            Assert.True(result.IsBlocked);
        }

        [Fact]
        public void Conflict_detection_rejects_wrong_sync_root()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");
            CottonSyncRootSnapshot wrongRoot = new(
                Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
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
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.Bidirectional);

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                wrongRoot,
                item,
                CreateFile("\"etag-1\""));

            Assert.Equal(CottonSyncConflictDetectionStatus.WrongSyncRoot, result.Status);
            Assert.True(result.IsBlocked);
        }

        [Fact]
        public void Conflict_detection_rejects_not_ready_root()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"");

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                CreateRoot(CottonSyncRootPermissionStatus.Unavailable),
                item,
                CreateFile("\"etag-1\""));

            Assert.Equal(CottonSyncConflictDetectionStatus.RootNotReady, result.Status);
            Assert.True(result.IsBlocked);
        }

        [Fact]
        public void Conflict_detection_ignores_terminal_journal_item()
        {
            CottonSyncJournalItemSnapshot item = CreateRenameJournalItem("\"etag-1\"")
                .Start(UpdatedAt.AddMinutes(1))
                .Complete(UpdatedAt.AddMinutes(2));

            CottonSyncConflictDetectionSnapshot result = CottonSyncConflictDetector.Create(
                CreateReadyRoot(),
                item,
                CreateFile("\"etag-1\""));

            Assert.Equal(CottonSyncConflictDetectionStatus.TerminalJournalItem, result.Status);
            Assert.True(result.IsBlocked);
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
            return CreateFile(FileId, eTag);
        }

        private static CottonFileBrowserEntry CreateFile(Guid id, string? eTag)
        {
            return CottonFileBrowserEntry.CreateCached(
                id,
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
