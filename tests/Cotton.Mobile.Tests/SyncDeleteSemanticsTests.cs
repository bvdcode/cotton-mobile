using Cotton.Files;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncDeleteSemanticsTests
    {
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Soft_delete_file_with_etag_uses_server_trash_and_expected_etag()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");

            CottonSyncDeleteSemanticsSnapshot semantics = CottonSyncDeleteSemantics.Create(
                file,
                CottonSyncDeleteMode.MoveToTrash);

            Assert.Equal(CottonSyncDeleteSafetyStatus.ConflictSafe, semantics.SafetyStatus);
            Assert.True(semantics.UsesServerTrash);
            Assert.False(semantics.SkipsServerTrash);
            Assert.False(semantics.RequiresExplicitConfirmation);
            Assert.True(semantics.RequiresExpectedETag);
            Assert.True(semantics.HasConflictPrecondition);
            Assert.Equal("\"etag-1\"", semantics.ExpectedETag);
        }

        [Fact]
        public void File_delete_without_etag_requires_fresh_file_revision()
        {
            CottonFileBrowserEntry file = CreateFile(eTag: null);

            CottonSyncDeleteSemanticsSnapshot semantics = CottonSyncDeleteSemantics.Create(
                file,
                CottonSyncDeleteMode.MoveToTrash);

            Assert.Equal(CottonSyncDeleteSafetyStatus.NeedsFreshFileETag, semantics.SafetyStatus);
            Assert.True(semantics.UsesServerTrash);
            Assert.True(semantics.RequiresExpectedETag);
            Assert.False(semantics.HasConflictPrecondition);
            Assert.Null(semantics.ExpectedETag);
        }

        [Fact]
        public void Folder_delete_is_not_conflict_safe_until_folder_revision_exists()
        {
            CottonFileBrowserEntry folder = CreateFolder();

            CottonSyncDeleteSemanticsSnapshot semantics = CottonSyncDeleteSemantics.Create(
                folder,
                CottonSyncDeleteMode.MoveToTrash);

            Assert.Equal(CottonSyncDeleteSafetyStatus.FolderRevisionUnsupported, semantics.SafetyStatus);
            Assert.True(semantics.UsesServerTrash);
            Assert.False(semantics.RequiresExpectedETag);
            Assert.False(semantics.HasConflictPrecondition);
            Assert.Null(semantics.ExpectedETag);
        }

        [Fact]
        public void Permanent_delete_skips_trash_and_requires_explicit_confirmation()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");

            CottonSyncDeleteSemanticsSnapshot semantics = CottonSyncDeleteSemantics.Create(
                file,
                CottonSyncDeleteMode.Permanent);

            Assert.False(semantics.UsesServerTrash);
            Assert.True(semantics.SkipsServerTrash);
            Assert.True(semantics.RequiresExplicitConfirmation);
            Assert.Equal(CottonSyncDeleteSafetyStatus.ConflictSafe, semantics.SafetyStatus);
            Assert.Equal("\"etag-1\"", semantics.ExpectedETag);
        }

        [Fact]
        public void Default_restore_request_does_not_recreate_parents_or_overwrite()
        {
            RestoreItemRequestDto request = CottonSyncRestorePolicy.CreateDefaultRequest();

            Assert.False(request.CreateMissingParents);
            Assert.False(request.Overwrite);
        }

        [Theory]
        [InlineData(RestoreStatus.Restored, CottonSyncRestoreOutcomeStatus.Restored, false, false, true)]
        [InlineData(RestoreStatus.ParentMissing, CottonSyncRestoreOutcomeStatus.ParentMissingNeedsChoice, true, false, false)]
        [InlineData(RestoreStatus.Conflict, CottonSyncRestoreOutcomeStatus.ConflictNeedsChoice, false, true, false)]
        [InlineData(RestoreStatus.NotRestorable, CottonSyncRestoreOutcomeStatus.NotRestorable, false, false, true)]
        public void Restore_outcome_maps_to_safe_next_action(
            RestoreStatus sdkStatus,
            CottonSyncRestoreOutcomeStatus expectedStatus,
            bool expectedCreateParentsRetry,
            bool expectedOverwriteRetry,
            bool expectedTerminal)
        {
            var outcome = new RestoreOutcomeDto
            {
                Status = sdkStatus,
            };

            CottonSyncRestoreOutcomeSnapshot snapshot = CottonSyncRestorePolicy.CreateOutcome(outcome);

            Assert.Equal(expectedStatus, snapshot.Status);
            Assert.Equal(expectedCreateParentsRetry, snapshot.CanRetryWithCreateMissingParents);
            Assert.Equal(expectedOverwriteRetry, snapshot.CanRetryWithOverwrite);
            Assert.Equal(expectedTerminal, snapshot.IsTerminal);
        }

        private static CottonFileBrowserEntry CreateFile(string? eTag)
        {
            return CottonFileBrowserEntry.CreateCached(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
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

        private static CottonFileBrowserEntry CreateFolder()
        {
            return CottonFileBrowserEntry.CreateCached(
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                CottonFileBrowserEntryType.Folder,
                "Projects",
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
