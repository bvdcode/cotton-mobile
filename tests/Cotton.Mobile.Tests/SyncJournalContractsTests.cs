using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncJournalContractsTests
    {
        private static readonly Guid JournalItemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid SyncRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid CloudFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid TargetParentId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid FileId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        private static readonly DateTime CreatedAt = new(2026, 6, 20, 11, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Later = CreatedAt.AddMinutes(1);

        [Fact]
        public void Rename_journal_item_captures_root_target_name_and_expected_etag()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");
            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                file,
                "renamed.txt",
                ["notes.txt"]);

            CottonSyncJournalItemSnapshot item = CottonSyncJournalItemSnapshot.CreateRenameMove(
                JournalItemId,
                CreateReadyRoot(),
                file,
                semantics,
                CottonSyncJournalOrigin.Device,
                sequence: 1,
                CreatedAt);

            Assert.Equal(CottonSyncJournalOperation.Rename, item.Operation);
            Assert.Equal(CottonSyncJournalStatus.Pending, item.Status);
            Assert.Equal(CottonSyncJournalOrigin.Device, item.Origin);
            Assert.Equal(SyncRootId, item.SyncRootId);
            Assert.Equal(FileId, item.TargetId);
            Assert.Equal("notes.txt", item.DisplayName);
            Assert.Equal("renamed.txt", item.NormalizedName);
            Assert.Equal("\"etag-1\"", item.ExpectedETag);
            Assert.Null(item.TargetParentId);
            Assert.False(string.IsNullOrWhiteSpace(item.SyncRootStableKey));
            Assert.True(item.CanCancel);
            Assert.False(item.CanRetry);
        }

        [Fact]
        public void Move_journal_item_captures_target_parent()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");
            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateMove(
                file,
                TargetParentId);

            CottonSyncJournalItemSnapshot item = CottonSyncJournalItemSnapshot.CreateRenameMove(
                JournalItemId,
                CreateReadyRoot(),
                file,
                semantics,
                CottonSyncJournalOrigin.Cloud,
                sequence: 2,
                CreatedAt);

            Assert.Equal(CottonSyncJournalOperation.Move, item.Operation);
            Assert.Equal(CottonSyncJournalOrigin.Cloud, item.Origin);
            Assert.Equal(TargetParentId, item.TargetParentId);
            Assert.Null(item.NormalizedName);
            Assert.Equal("\"etag-1\"", item.ExpectedETag);
        }

        [Fact]
        public void Delete_journal_item_captures_delete_mode_and_expected_etag()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");
            CottonSyncDeleteSemanticsSnapshot semantics = CottonSyncDeleteSemantics.Create(
                file,
                CottonSyncDeleteMode.MoveToTrash);

            CottonSyncJournalItemSnapshot item = CottonSyncJournalItemSnapshot.CreateDelete(
                JournalItemId,
                CreateReadyRoot(),
                file,
                semantics,
                CottonSyncJournalOrigin.Device,
                sequence: 3,
                CreatedAt);

            Assert.Equal(CottonSyncJournalOperation.Delete, item.Operation);
            Assert.Equal(CottonSyncDeleteMode.MoveToTrash, item.DeleteMode);
            Assert.Equal("\"etag-1\"", item.ExpectedETag);
            Assert.Null(item.NormalizedName);
            Assert.Null(item.TargetParentId);
        }

        [Fact]
        public void Journal_rejects_mutation_without_conflict_precondition()
        {
            CottonFileBrowserEntry file = CreateFile(eTag: null);
            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                file,
                "renamed.txt",
                ["notes.txt"]);

            Assert.Throws<InvalidOperationException>(() =>
                CottonSyncJournalItemSnapshot.CreateRenameMove(
                    JournalItemId,
                    CreateReadyRoot(),
                    file,
                    semantics,
                    CottonSyncJournalOrigin.Device,
                    sequence: 1,
                    CreatedAt));
        }

        [Fact]
        public void Journal_rejects_mutation_when_sync_root_is_not_ready()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");
            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                file,
                "renamed.txt",
                ["notes.txt"]);

            Assert.Throws<InvalidOperationException>(() =>
                CottonSyncJournalItemSnapshot.CreateRenameMove(
                    JournalItemId,
                    CreateRoot(CottonSyncRootPermissionStatus.Revoked),
                    file,
                    semantics,
                    CottonSyncJournalOrigin.Device,
                    sequence: 1,
                    CreatedAt));
        }

        [Fact]
        public void Journal_rejects_folder_mutation_until_folder_revision_precondition_exists()
        {
            CottonFileBrowserEntry folder = CreateFolder();
            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                folder,
                "Renamed Projects",
                ["Projects"]);

            Assert.Throws<InvalidOperationException>(() =>
                CottonSyncJournalItemSnapshot.CreateRenameMove(
                    JournalItemId,
                    CreateReadyRoot(),
                    folder,
                    semantics,
                    CottonSyncJournalOrigin.Device,
                    sequence: 1,
                    CreatedAt));
        }

        [Fact]
        public void Journal_state_machine_tracks_attempt_failure_retry_cancel_and_terminal_states()
        {
            CottonSyncJournalItemSnapshot pending = CreateRenameJournalItem();
            CottonSyncJournalItemSnapshot running = pending.Start(Later);
            CottonSyncJournalItemSnapshot failed = running.Fail("Offline", Later.AddSeconds(1));
            CottonSyncJournalItemSnapshot retry = failed.Retry(Later.AddSeconds(2));
            CottonSyncJournalItemSnapshot cancelled = retry.Cancel(Later.AddSeconds(3));

            Assert.Equal(CottonSyncJournalStatus.Running, running.Status);
            Assert.Equal(1, running.AttemptCount);
            Assert.False(running.CanRetry);

            Assert.Equal(CottonSyncJournalStatus.Failed, failed.Status);
            Assert.Equal("Offline", failed.FailureMessage);
            Assert.True(failed.CanRetry);
            Assert.Equal(1, failed.AttemptCount);

            Assert.Equal(CottonSyncJournalStatus.Pending, retry.Status);
            Assert.Null(retry.FailureMessage);
            Assert.Equal(1, retry.AttemptCount);

            Assert.Equal(CottonSyncJournalStatus.Cancelled, cancelled.Status);
            Assert.True(cancelled.IsTerminal);
            Assert.False(cancelled.CanCancel);
        }

        [Fact]
        public void Completed_journal_item_is_terminal()
        {
            CottonSyncJournalItemSnapshot completed = CreateRenameJournalItem()
                .Start(Later)
                .Complete(Later.AddSeconds(1));

            Assert.Equal(CottonSyncJournalStatus.Completed, completed.Status);
            Assert.True(completed.IsTerminal);
            Assert.False(completed.CanCancel);
            Assert.False(completed.CanRetry);
            Assert.Throws<InvalidOperationException>(() => completed.Retry(Later.AddSeconds(2)));
        }

        [Fact]
        public void Conflict_blocks_retry_until_future_conflict_resolution()
        {
            CottonSyncJournalItemSnapshot conflict = CreateRenameJournalItem()
                .MarkConflict("Remote changed first", Later);

            Assert.Equal(CottonSyncJournalStatus.Conflict, conflict.Status);
            Assert.Equal("Remote changed first", conflict.FailureMessage);
            Assert.True(conflict.IsConflict);
            Assert.False(conflict.CanRetry);
            Assert.Throws<InvalidOperationException>(() => conflict.Retry(Later.AddSeconds(1)));
        }

        [Fact]
        public void Restart_restores_running_journal_item_to_pending_without_touching_terminal_items()
        {
            CottonSyncJournalItemSnapshot running = CreateRenameJournalItem().Start(Later);
            CottonSyncJournalItemSnapshot completed = running.Complete(Later.AddSeconds(1));

            CottonSyncJournalItemSnapshot restoredRunning = running.RestoreAfterRestart(Later.AddMinutes(5));
            CottonSyncJournalItemSnapshot restoredCompleted = completed.RestoreAfterRestart(Later.AddMinutes(5));

            Assert.Equal(CottonSyncJournalStatus.Pending, restoredRunning.Status);
            Assert.Equal(1, restoredRunning.AttemptCount);
            Assert.Equal(Later.AddMinutes(5), restoredRunning.UpdatedAtUtc);
            Assert.Same(completed, restoredCompleted);
        }

        [Fact]
        public void Restore_rejects_invalid_operation_payloads()
        {
            Assert.Throws<ArgumentException>(() =>
                CottonSyncJournalItemSnapshot.Restore(
                    JournalItemId,
                    SyncRootId,
                    "stable-key",
                    sequence: 1,
                    CottonSyncJournalOperation.Rename,
                    CottonFileBrowserEntryType.File,
                    FileId,
                    "notes.txt",
                    CottonSyncJournalOrigin.Device,
                    CottonSyncJournalStatus.Pending,
                    attemptCount: 0,
                    failureMessage: null,
                    expectedETag: "\"etag-1\"",
                    normalizedName: null,
                    targetParentId: null,
                    deleteMode: null,
                    CreatedAt,
                    CreatedAt));

            Assert.Throws<ArgumentException>(() =>
                CottonSyncJournalItemSnapshot.Restore(
                    JournalItemId,
                    SyncRootId,
                    "stable-key",
                    sequence: 1,
                    CottonSyncJournalOperation.Move,
                    CottonFileBrowserEntryType.File,
                    FileId,
                    "notes.txt",
                    CottonSyncJournalOrigin.Device,
                    CottonSyncJournalStatus.Pending,
                    attemptCount: 0,
                    failureMessage: null,
                    expectedETag: "\"etag-1\"",
                    normalizedName: null,
                    targetParentId: null,
                    deleteMode: null,
                    CreatedAt,
                    CreatedAt));

            Assert.Throws<ArgumentException>(() =>
                CottonSyncJournalItemSnapshot.Restore(
                    JournalItemId,
                    SyncRootId,
                    "stable-key",
                    sequence: 1,
                    CottonSyncJournalOperation.Delete,
                    CottonFileBrowserEntryType.File,
                    FileId,
                    "notes.txt",
                    CottonSyncJournalOrigin.Device,
                    CottonSyncJournalStatus.Pending,
                    attemptCount: 0,
                    failureMessage: null,
                    expectedETag: "\"etag-1\"",
                    normalizedName: null,
                    targetParentId: null,
                    deleteMode: null,
                    CreatedAt,
                    CreatedAt));
        }

        private static CottonSyncJournalItemSnapshot CreateRenameJournalItem()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");
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
                CreatedAt);
        }

        private static CottonSyncRootSnapshot CreateReadyRoot()
        {
            return CreateRoot(CottonSyncRootPermissionStatus.Available);
        }

        private static CottonSyncRootSnapshot CreateRoot(CottonSyncRootPermissionStatus permissionStatus)
        {
            var localRoot = new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.AppPrivateDirectory,
                "app-private-sync-root",
                "On this device",
                permissionStatus);

            return new CottonSyncRootSnapshot(
                SyncRootId,
                new Uri("https://app.cottoncloud.dev"),
                "account-1",
                new CottonUploadDestinationSnapshot(
                    CloudFolderId,
                    "Projects",
                    "Files / Projects"),
                localRoot,
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
                CreatedAt,
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag);
        }

        private static CottonFileBrowserEntry CreateFolder()
        {
            return CottonFileBrowserEntry.CreateCached(
                Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                CottonFileBrowserEntryType.Folder,
                "Projects",
                "Folder",
                "Folder",
                "Open",
                "Folder",
                CreatedAt,
                sizeBytes: null,
                contentType: null,
                previewHashEncryptedHex: null,
                eTag: null);
        }
    }
}
