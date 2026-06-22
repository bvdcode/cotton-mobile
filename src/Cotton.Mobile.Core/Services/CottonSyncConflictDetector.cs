// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncConflictDetector
    {
        public static CottonSyncConflictDetectionSnapshot Create(
            CottonSyncRootSnapshot root,
            CottonSyncJournalItemSnapshot item,
            CottonFileBrowserEntry? latestRemoteEntry)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(item);

            if (root.Id != item.SyncRootId
                || !string.Equals(root.StableKey, item.SyncRootStableKey, StringComparison.Ordinal))
            {
                return CreateSnapshot(item, CottonSyncConflictDetectionStatus.WrongSyncRoot, "Wrong sync root");
            }

            if (!root.CanRunSync)
            {
                return CreateSnapshot(item, CottonSyncConflictDetectionStatus.RootNotReady, "Sync root not ready");
            }

            if (item.IsTerminal)
            {
                return CreateSnapshot(item, CottonSyncConflictDetectionStatus.TerminalJournalItem, "Journal item already terminal");
            }

            if (item.TargetType != CottonFileBrowserEntryType.File)
            {
                return CreateSnapshot(
                    item,
                    CottonSyncConflictDetectionStatus.FolderRevisionUnsupported,
                    "Folder revision unavailable");
            }

            if (string.IsNullOrWhiteSpace(item.ExpectedETag))
            {
                return CreateSnapshot(
                    item,
                    CottonSyncConflictDetectionStatus.NeedsFreshServerRevision,
                    "Expected ETag required");
            }

            if (latestRemoteEntry is null)
            {
                return CreateSnapshot(
                    item,
                    CottonSyncConflictDetectionStatus.RemoteTargetMissing,
                    "Remote target missing");
            }

            if (latestRemoteEntry.Id != item.TargetId)
            {
                return CreateSnapshot(
                    item,
                    CottonSyncConflictDetectionStatus.RemoteTargetMismatch,
                    "Remote target mismatch",
                    latestRemoteEntry.ETag);
            }

            if (latestRemoteEntry.Type != item.TargetType)
            {
                return CreateSnapshot(
                    item,
                    CottonSyncConflictDetectionStatus.RemoteTargetTypeChanged,
                    "Remote target type changed",
                    latestRemoteEntry.ETag);
            }

            if (string.IsNullOrWhiteSpace(latestRemoteEntry.ETag))
            {
                return CreateSnapshot(
                    item,
                    CottonSyncConflictDetectionStatus.NeedsFreshServerRevision,
                    "Fresh remote ETag required");
            }

            string expectedETag = item.ExpectedETag.Trim();
            string actualETag = latestRemoteEntry.ETag.Trim();
            if (!string.Equals(expectedETag, actualETag, StringComparison.Ordinal))
            {
                return CreateSnapshot(
                    item,
                    CottonSyncConflictDetectionStatus.ServerRevisionChanged,
                    "Remote revision changed",
                    actualETag);
            }

            return CreateSnapshot(
                item,
                CottonSyncConflictDetectionStatus.Ready,
                "Expected ETag matches",
                actualETag);
        }

        private static CottonSyncConflictDetectionSnapshot CreateSnapshot(
            CottonSyncJournalItemSnapshot item,
            CottonSyncConflictDetectionStatus status,
            string detailText,
            string? actualETag = null)
        {
            return new CottonSyncConflictDetectionSnapshot(
                item.Operation,
                status,
                item.ExpectedETag,
                actualETag,
                $"{item.Operation}: {detailText}");
        }
    }
}
