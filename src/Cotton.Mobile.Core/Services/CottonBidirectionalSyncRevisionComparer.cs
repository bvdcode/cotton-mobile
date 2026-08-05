// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal static class CottonBidirectionalSyncRevisionComparer
    {
        public static bool LocalMatchesManifest(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            CottonSyncedFileSnapshot manifestItem)
        {
            return localItem.ContentHash is not null
                && manifestItem.ContentHash is not null
                && string.Equals(localItem.ContentHash, manifestItem.ContentHash, StringComparison.Ordinal);
        }

        public static CottonBidirectionalSyncActionKind? CreateRemoteMismatchAction(
            CottonDeviceToCloudRemoteItemSnapshot remoteItem,
            CottonSyncedFileSnapshot manifestItem)
        {
            if (remoteItem.Entry.Id != manifestItem.FileId
                || remoteItem.Entry.Type != CottonFileBrowserEntryType.File)
            {
                return CottonBidirectionalSyncActionKind.RemotePathConflict;
            }

            if (string.IsNullOrWhiteSpace(remoteItem.Entry.ETag) || remoteItem.Entry.ContentHash is null)
            {
                return CottonBidirectionalSyncActionKind.NeedsFreshServerRevision;
            }

            if (!string.Equals(remoteItem.Entry.ETag.Trim(), manifestItem.ETag, StringComparison.Ordinal))
            {
                return CottonBidirectionalSyncActionKind.RefreshLocalFile;
            }

            return string.Equals(remoteItem.RelativePath, manifestItem.RelativePath, StringComparison.Ordinal)
                ? null
                : CottonBidirectionalSyncActionKind.RenameLocalFile;
        }

        public static CottonBidirectionalSyncActionKind CreateLocalRemoteMismatchAction(
            CottonBidirectionalSyncActionKind remoteMismatchAction)
        {
            switch (remoteMismatchAction)
            {
                case CottonBidirectionalSyncActionKind.NeedsFreshServerRevision:
                    return CottonBidirectionalSyncActionKind.NeedsFreshServerRevision;

                case CottonBidirectionalSyncActionKind.RenameLocalFile:
                    return CottonBidirectionalSyncActionKind.RenameLocalFile;

                case CottonBidirectionalSyncActionKind.RemotePathConflict:
                case CottonBidirectionalSyncActionKind.RefreshLocalFile:
                    return CottonBidirectionalSyncActionKind.RefreshLocalFile;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(remoteMismatchAction),
                        remoteMismatchAction,
                        "Remote mismatch action is not supported.");
            }
        }
    }
}
