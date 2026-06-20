using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRenameMoveSemanticsTests
    {
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
        private static readonly Guid TargetParentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        [Fact]
        public void File_rename_with_etag_normalizes_name_and_uses_expected_etag()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                file,
                " renamed.txt ",
                ["notes.txt", "archive.txt"]);

            Assert.Equal(CottonSyncRenameMoveOperation.Rename, semantics.Operation);
            Assert.Equal(CottonSyncRenameMoveSafetyStatus.ConflictSafe, semantics.SafetyStatus);
            Assert.Equal("renamed.txt", semantics.NormalizedName);
            Assert.Equal("\"etag-1\"", semantics.ExpectedETag);
            Assert.True(semantics.HasConflictPrecondition);
            Assert.True(semantics.RequiresExpectedETag);
            Assert.False(semantics.IsRejected);
        }

        [Fact]
        public void File_rename_without_etag_requires_fresh_file_revision()
        {
            CottonFileBrowserEntry file = CreateFile(eTag: null);

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                file,
                "renamed.txt",
                ["notes.txt"]);

            Assert.Equal(CottonSyncRenameMoveSafetyStatus.NeedsFreshFileETag, semantics.SafetyStatus);
            Assert.Equal("renamed.txt", semantics.NormalizedName);
            Assert.Null(semantics.ExpectedETag);
            Assert.True(semantics.RequiresFreshListing);
            Assert.True(semantics.RequiresExpectedETag);
        }

        [Fact]
        public void Folder_rename_is_not_conflict_safe_until_folder_revision_exists()
        {
            CottonFileBrowserEntry folder = CreateFolder();

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                folder,
                "Renamed Projects",
                ["Projects"]);

            Assert.Equal(CottonSyncRenameMoveSafetyStatus.FolderRevisionUnsupported, semantics.SafetyStatus);
            Assert.Equal("Renamed Projects", semantics.NormalizedName);
            Assert.Null(semantics.ExpectedETag);
            Assert.True(semantics.HasFolderRevisionGap);
            Assert.False(semantics.RequiresExpectedETag);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(".")]
        [InlineData("..")]
        [InlineData("renamed/path.txt")]
        [InlineData("renamed\\path.txt")]
        [InlineData("renamed?.txt")]
        public void Rename_rejects_empty_paths_and_reserved_characters(string? proposedName)
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                file,
                proposedName,
                ["notes.txt"]);

            Assert.Equal(CottonSyncRenameMoveSafetyStatus.InvalidName, semantics.SafetyStatus);
            Assert.Null(semantics.NormalizedName);
            Assert.Null(semantics.ExpectedETag);
            Assert.True(semantics.IsRejected);
        }

        [Fact]
        public void Rename_rejects_duplicate_target_name()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                file,
                "archive.txt",
                ["notes.txt", " Archive.txt "]);

            Assert.Equal(CottonSyncRenameMoveSafetyStatus.DuplicateName, semantics.SafetyStatus);
            Assert.Null(semantics.NormalizedName);
            Assert.True(semantics.IsRejected);
        }

        [Fact]
        public void Rename_to_same_exact_name_is_no_change()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateRename(
                file,
                " notes.txt ",
                ["notes.txt"]);

            Assert.Equal(CottonSyncRenameMoveSafetyStatus.NoChange, semantics.SafetyStatus);
            Assert.Equal("notes.txt", semantics.NormalizedName);
            Assert.Null(semantics.ExpectedETag);
            Assert.True(semantics.IsNoChange);
            Assert.False(semantics.RequiresExpectedETag);
        }

        [Fact]
        public void File_move_with_etag_uses_target_parent_and_expected_etag()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateMove(
                file,
                TargetParentId);

            Assert.Equal(CottonSyncRenameMoveOperation.Move, semantics.Operation);
            Assert.Equal(CottonSyncRenameMoveSafetyStatus.ConflictSafe, semantics.SafetyStatus);
            Assert.Equal(TargetParentId, semantics.TargetParentId);
            Assert.Equal("\"etag-1\"", semantics.ExpectedETag);
            Assert.True(semantics.HasConflictPrecondition);
            Assert.True(semantics.RequiresExpectedETag);
        }

        [Fact]
        public void File_move_without_etag_requires_fresh_file_revision()
        {
            CottonFileBrowserEntry file = CreateFile(eTag: null);

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateMove(
                file,
                TargetParentId);

            Assert.Equal(CottonSyncRenameMoveSafetyStatus.NeedsFreshFileETag, semantics.SafetyStatus);
            Assert.Equal(TargetParentId, semantics.TargetParentId);
            Assert.Null(semantics.ExpectedETag);
            Assert.True(semantics.RequiresFreshListing);
        }

        [Fact]
        public void Folder_move_is_not_conflict_safe_until_folder_revision_exists()
        {
            CottonFileBrowserEntry folder = CreateFolder();

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateMove(
                folder,
                TargetParentId);

            Assert.Equal(CottonSyncRenameMoveSafetyStatus.FolderRevisionUnsupported, semantics.SafetyStatus);
            Assert.Equal(TargetParentId, semantics.TargetParentId);
            Assert.Null(semantics.ExpectedETag);
            Assert.True(semantics.HasFolderRevisionGap);
        }

        [Fact]
        public void Move_rejects_empty_target_parent()
        {
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateMove(
                file,
                Guid.Empty);

            Assert.Equal(CottonSyncRenameMoveSafetyStatus.InvalidMoveTarget, semantics.SafetyStatus);
            Assert.Null(semantics.TargetParentId);
            Assert.Null(semantics.ExpectedETag);
            Assert.True(semantics.IsRejected);
        }

        [Fact]
        public void Folder_move_rejects_self_target()
        {
            CottonFileBrowserEntry folder = CreateFolder();

            CottonSyncRenameMoveSemanticsSnapshot semantics = CottonSyncRenameMoveSemantics.CreateMove(
                folder,
                folder.Id);

            Assert.Equal(CottonSyncRenameMoveSafetyStatus.SelfMoveUnsupported, semantics.SafetyStatus);
            Assert.Equal(folder.Id, semantics.TargetParentId);
            Assert.True(semantics.IsRejected);
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
