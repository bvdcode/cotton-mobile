// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncRenameMoveSemantics
    {
        public static CottonSyncRenameMoveSemanticsSnapshot CreateRename(
            CottonFileBrowserEntry entry,
            string? proposedName,
            IEnumerable<string> existingNames)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(existingNames);

            if (!TryNormalizeRenameName(
                entry,
                proposedName,
                existingNames,
                out string normalizedName,
                out CottonSyncRenameMoveSafetyStatus rejectedStatus,
                out string rejectedSummary))
            {
                return new CottonSyncRenameMoveSemanticsSnapshot(
                    entry.Type,
                    CottonSyncRenameMoveOperation.Rename,
                    rejectedStatus,
                    expectedETag: null,
                    normalizedName: null,
                    targetParentId: null,
                    CreateSummary(CottonSyncRenameMoveOperation.Rename, rejectedSummary));
            }

            if (string.Equals(entry.Name, normalizedName, StringComparison.Ordinal))
            {
                return new CottonSyncRenameMoveSemanticsSnapshot(
                    entry.Type,
                    CottonSyncRenameMoveOperation.Rename,
                    CottonSyncRenameMoveSafetyStatus.NoChange,
                    expectedETag: null,
                    normalizedName,
                    targetParentId: null,
                    CreateSummary(CottonSyncRenameMoveOperation.Rename, "Name unchanged"));
            }

            return CreateMutationSnapshot(
                entry,
                CottonSyncRenameMoveOperation.Rename,
                normalizedName,
                targetParentId: null);
        }

        public static CottonSyncRenameMoveSemanticsSnapshot CreateMove(
            CottonFileBrowserEntry entry,
            Guid targetParentId)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (targetParentId == Guid.Empty)
            {
                return new CottonSyncRenameMoveSemanticsSnapshot(
                    entry.Type,
                    CottonSyncRenameMoveOperation.Move,
                    CottonSyncRenameMoveSafetyStatus.InvalidMoveTarget,
                    expectedETag: null,
                    normalizedName: null,
                    targetParentId: null,
                    CreateSummary(CottonSyncRenameMoveOperation.Move, "Target folder required"));
            }

            if (entry.Type == CottonFileBrowserEntryType.Folder && entry.Id == targetParentId)
            {
                return new CottonSyncRenameMoveSemanticsSnapshot(
                    entry.Type,
                    CottonSyncRenameMoveOperation.Move,
                    CottonSyncRenameMoveSafetyStatus.SelfMoveUnsupported,
                    expectedETag: null,
                    normalizedName: null,
                    targetParentId,
                    CreateSummary(CottonSyncRenameMoveOperation.Move, "Cannot move folder into itself"));
            }

            return CreateMutationSnapshot(
                entry,
                CottonSyncRenameMoveOperation.Move,
                normalizedName: null,
                targetParentId);
        }

        private static CottonSyncRenameMoveSemanticsSnapshot CreateMutationSnapshot(
            CottonFileBrowserEntry entry,
            CottonSyncRenameMoveOperation operation,
            string? normalizedName,
            Guid? targetParentId)
        {
            if (entry.Type != CottonFileBrowserEntryType.File)
            {
                return new CottonSyncRenameMoveSemanticsSnapshot(
                    entry.Type,
                    operation,
                    CottonSyncRenameMoveSafetyStatus.FolderRevisionUnsupported,
                    expectedETag: null,
                    normalizedName,
                    targetParentId,
                    CreateSummary(operation, "Folder revision unavailable"));
            }

            CottonSyncServerRevisionSnapshot revision = CottonSyncServerRevisionContract.Create(entry);
            if (!revision.SupportsExpectedETagMutation)
            {
                return new CottonSyncRenameMoveSemanticsSnapshot(
                    entry.Type,
                    operation,
                    CottonSyncRenameMoveSafetyStatus.NeedsFreshFileETag,
                    expectedETag: null,
                    normalizedName,
                    targetParentId,
                    CreateSummary(operation, "Fresh file ETag required"));
            }

            return new CottonSyncRenameMoveSemanticsSnapshot(
                entry.Type,
                operation,
                CottonSyncRenameMoveSafetyStatus.ConflictSafe,
                revision.ETag,
                normalizedName,
                targetParentId,
                CreateSummary(operation, "Expected ETag ready"));
        }

        private static bool TryNormalizeRenameName(
            CottonFileBrowserEntry entry,
            string? proposedName,
            IEnumerable<string> existingNames,
            out string normalizedName,
            out CottonSyncRenameMoveSafetyStatus rejectedStatus,
            out string rejectedSummary)
        {
            normalizedName = string.Empty;
            rejectedStatus = CottonSyncRenameMoveSafetyStatus.InvalidName;
            rejectedSummary = string.Empty;

            if (string.IsNullOrWhiteSpace(proposedName))
            {
                rejectedSummary = "Name required";
                return false;
            }

            string name = proposedName.Trim();
            if (CottonCloudItemNameRules.IsReservedPathSegment(name)
                || CottonCloudItemNameRules.ContainsInvalidCharacter(name))
            {
                rejectedSummary = "Invalid name";
                return false;
            }

            IEnumerable<string> otherNames = existingNames.Where(existingName => !string.Equals(
                entry.Name,
                existingName.Trim(),
                StringComparison.OrdinalIgnoreCase));
            if (CottonCloudItemNameRules.ContainsDuplicateName(name, otherNames))
            {
                rejectedStatus = CottonSyncRenameMoveSafetyStatus.DuplicateName;
                rejectedSummary = "Duplicate name";
                return false;
            }

            normalizedName = name;
            return true;
        }

        private static string CreateSummary(
            CottonSyncRenameMoveOperation operation,
            string safetyText)
        {
            string operationText = operation == CottonSyncRenameMoveOperation.Rename
                ? "Rename"
                : "Move";
            return $"{operationText}: {safetyText}";
        }
    }
}
