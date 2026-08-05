// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonBidirectionalSyncItemPlanner
    {
        private readonly CottonBidirectionalSyncIndex _index;

        public CottonBidirectionalSyncItemPlanner(CottonBidirectionalSyncIndex index)
        {
            ArgumentNullException.ThrowIfNull(index);

            _index = index;
        }

        public CottonBidirectionalSyncPlanItem CreateLocalItem(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            ISet<string> handledManifestPaths)
        {
            if (localItem.ItemType == CottonFileBrowserEntryType.Folder)
            {
                return CreateLocalFolderItem(localItem);
            }

            if (!_index.ManifestByPath.TryGetValue(
                localItem.RelativePath,
                out CottonSyncedFileSnapshot? manifestItem))
            {
                return CreateNewLocalFileItem(localItem);
            }

            handledManifestPaths.Add(manifestItem.RelativePath);
            bool localChanged = !CottonBidirectionalSyncRevisionComparer.LocalMatchesManifest(
                localItem,
                manifestItem);
            if (!_index.RemoteById.TryGetValue(
                manifestItem.FileId,
                out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                if (_index.RemoteByPath.TryGetValue(
                    localItem.RelativePath,
                    out CottonDeviceToCloudRemoteItemSnapshot? replacementRemoteItem))
                {
                    return CreateRemoteReplacementItem(localItem, replacementRemoteItem, localChanged);
                }

                return localChanged
                    ? CottonBidirectionalSyncPlanItemFactory.CreateLocal(
                        CottonBidirectionalSyncActionKind.RemoteTargetMissing,
                        CottonFileBrowserEntryType.File,
                        localItem.DisplayName,
                        localItem.RelativePath,
                        manifestItem.FileId,
                        manifestItem.ETag,
                        localItem,
                        remoteUpdatedAtUtc: null)
                    : CottonBidirectionalSyncPlanItemFactory.CreateManifest(
                        CottonBidirectionalSyncActionKind.RemoveLocalFile,
                        manifestItem,
                        manifestItem.ETag,
                        remoteUpdatedAtUtc: null);
            }

            if (remoteItem.Entry.Type != CottonFileBrowserEntryType.File)
            {
                return CottonBidirectionalSyncPlanItemFactory.CreateLocal(
                    CottonBidirectionalSyncActionKind.RemotePathConflict,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag,
                    localItem,
                    remoteItem.Entry.UpdatedAtUtc,
                    remoteItem.Entry.ContentHash);
            }

            CottonBidirectionalSyncActionKind? remoteMismatch =
                CottonBidirectionalSyncRevisionComparer.CreateRemoteMismatchAction(remoteItem, manifestItem);
            if (remoteMismatch.HasValue)
            {
                if (localChanged)
                {
                    return CottonBidirectionalSyncPlanItemFactory.CreateLocal(
                        CottonBidirectionalSyncActionKind.FileChangedOnBothSides,
                        CottonFileBrowserEntryType.File,
                        localItem.DisplayName,
                        localItem.RelativePath,
                        remoteItem.Entry.Id,
                        remoteItem.Entry.ETag,
                        localItem,
                        remoteItem.Entry.UpdatedAtUtc,
                        remoteItem.Entry.ContentHash);
                }

                return CottonBidirectionalSyncPlanItemFactory.CreateRemote(
                    CottonBidirectionalSyncRevisionComparer.CreateLocalRemoteMismatchAction(remoteMismatch.Value),
                    remoteItem,
                    manifestItem.RelativePath);
            }

            if (localChanged)
            {
                return CottonBidirectionalSyncPlanItemFactory.CreateLocal(
                    CottonBidirectionalSyncActionKind.UploadChangedFile,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    manifestItem.FileId,
                    manifestItem.ETag,
                    localItem,
                    remoteItem.Entry.UpdatedAtUtc,
                    remoteItem.Entry.ContentHash);
            }

            return CottonBidirectionalSyncPlanItemFactory.CreateRemote(
                CottonBidirectionalSyncActionKind.KeepExistingFile,
                remoteItem);
        }

        public CottonBidirectionalSyncPlanItem CreateMissingLocalItem(CottonSyncedFileSnapshot manifestItem)
        {
            if (!_index.RemoteById.TryGetValue(
                manifestItem.FileId,
                out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CottonBidirectionalSyncPlanItemFactory.CreateManifest(
                    CottonBidirectionalSyncActionKind.RemoveManifestOrphan,
                    manifestItem,
                    manifestItem.ETag,
                    remoteUpdatedAtUtc: null);
            }

            CottonBidirectionalSyncActionKind? remoteMismatch =
                CottonBidirectionalSyncRevisionComparer.CreateRemoteMismatchAction(remoteItem, manifestItem);
            if (remoteMismatch.HasValue)
            {
                CottonBidirectionalSyncActionKind action =
                    remoteMismatch.Value == CottonBidirectionalSyncActionKind.NeedsFreshServerRevision
                        ? CottonBidirectionalSyncActionKind.NeedsFreshServerRevision
                        : CottonBidirectionalSyncActionKind.RemoteTargetMissing;
                return CottonBidirectionalSyncPlanItemFactory.CreateManifest(
                    action,
                    manifestItem,
                    remoteItem.Entry.ETag,
                    remoteItem.Entry.UpdatedAtUtc,
                    remoteItem.Entry.ContentHash);
            }

            return CottonBidirectionalSyncPlanItemFactory.CreateManifest(
                CottonBidirectionalSyncActionKind.DeleteRemoteFile,
                manifestItem,
                manifestItem.ETag,
                remoteItem.Entry.UpdatedAtUtc,
                remoteItem.Entry.ContentHash);
        }

        public CottonBidirectionalSyncPlanItem CreateRemoteOnlyItem(
            CottonDeviceToCloudRemoteItemSnapshot remoteItem)
        {
            if (remoteItem.Entry.Type == CottonFileBrowserEntryType.Folder)
            {
                return CottonBidirectionalSyncPlanItemFactory.CreateRemote(
                    CottonBidirectionalSyncActionKind.BlockedRemoteFolder,
                    remoteItem);
            }

            CottonBidirectionalSyncActionKind action = string.IsNullOrWhiteSpace(remoteItem.Entry.ETag)
                || remoteItem.Entry.ContentHash is null
                    ? CottonBidirectionalSyncActionKind.NeedsFreshServerRevision
                    : CottonBidirectionalSyncActionKind.DownloadNewFile;
            return CottonBidirectionalSyncPlanItemFactory.CreateRemote(action, remoteItem);
        }

        private CottonBidirectionalSyncPlanItem CreateRemoteReplacementItem(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            CottonDeviceToCloudRemoteItemSnapshot remoteItem,
            bool localChanged)
        {
            if (remoteItem.Entry.Type != CottonFileBrowserEntryType.File)
            {
                return CottonBidirectionalSyncPlanItemFactory.CreateLocal(
                    CottonBidirectionalSyncActionKind.RemotePathConflict,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag,
                    localItem,
                    remoteItem.Entry.UpdatedAtUtc,
                    remoteItem.Entry.ContentHash);
            }

            if (localChanged)
            {
                return CottonBidirectionalSyncPlanItemFactory.CreateLocal(
                    CottonBidirectionalSyncActionKind.FileChangedOnBothSides,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag,
                    localItem,
                    remoteItem.Entry.UpdatedAtUtc,
                    remoteItem.Entry.ContentHash);
            }

            CottonBidirectionalSyncActionKind action = string.IsNullOrWhiteSpace(remoteItem.Entry.ETag)
                || remoteItem.Entry.ContentHash is null
                    ? CottonBidirectionalSyncActionKind.NeedsFreshServerRevision
                    : CottonBidirectionalSyncActionKind.RefreshLocalFile;
            return CottonBidirectionalSyncPlanItemFactory.CreateRemote(action, remoteItem);
        }

        private CottonBidirectionalSyncPlanItem CreateLocalFolderItem(
            CottonDeviceToCloudLocalItemSnapshot localItem)
        {
            if (!_index.RemoteByPath.TryGetValue(
                localItem.RelativePath,
                out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CottonBidirectionalSyncPlanItemFactory.CreateLocal(
                    CottonBidirectionalSyncActionKind.CreateRemoteFolder,
                    CottonFileBrowserEntryType.Folder,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    cloudItemId: null,
                    expectedRemoteETag: null,
                    localItem,
                    remoteUpdatedAtUtc: null);
            }

            CottonBidirectionalSyncActionKind action = remoteItem.Entry.Type == CottonFileBrowserEntryType.Folder
                ? CottonBidirectionalSyncActionKind.KeepExistingFolder
                : CottonBidirectionalSyncActionKind.RemotePathConflict;
            return CottonBidirectionalSyncPlanItemFactory.CreateLocal(
                action,
                CottonFileBrowserEntryType.Folder,
                localItem.DisplayName,
                localItem.RelativePath,
                remoteItem.Entry.Id,
                remoteItem.Entry.ETag,
                localItem,
                remoteItem.Entry.UpdatedAtUtc,
                remoteItem.Entry.ContentHash);
        }

        private CottonBidirectionalSyncPlanItem CreateNewLocalFileItem(
            CottonDeviceToCloudLocalItemSnapshot localItem)
        {
            if (_index.RemoteByPath.TryGetValue(
                localItem.RelativePath,
                out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CottonBidirectionalSyncPlanItemFactory.CreateLocal(
                    CottonBidirectionalSyncActionKind.RemotePathConflict,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag,
                    localItem,
                    remoteItem.Entry.UpdatedAtUtc,
                    remoteItem.Entry.ContentHash);
            }

            return CottonBidirectionalSyncPlanItemFactory.CreateLocal(
                CottonBidirectionalSyncActionKind.UploadNewFile,
                CottonFileBrowserEntryType.File,
                localItem.DisplayName,
                localItem.RelativePath,
                cloudItemId: null,
                expectedRemoteETag: null,
                localItem,
                remoteUpdatedAtUtc: null);
        }
    }
}
