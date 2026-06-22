// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonBidirectionalSyncPlanSnapshot
    {
        public CottonBidirectionalSyncPlanSnapshot(
            Guid syncRootId,
            Guid folderId,
            string folderName,
            IReadOnlyList<CottonBidirectionalSyncPlanItem> items)
        {
            if (syncRootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(syncRootId));
            }

            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Cloud folder id is required.", nameof(folderId));
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new ArgumentException("Cloud folder name is required.", nameof(folderName));
            }

            ArgumentNullException.ThrowIfNull(items);

            SyncRootId = syncRootId;
            FolderId = folderId;
            FolderName = folderName.Trim();
            Items = items;
        }

        public Guid SyncRootId { get; }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public IReadOnlyList<CottonBidirectionalSyncPlanItem> Items { get; }

        public int DownloadCount => Items.Count(item => item.RequiresDownload);

        public int LocalRenameCount => Items.Count(item => item.RequiresLocalRename);

        public int LocalDeleteCount => Items.Count(item => item.RequiresLocalDelete);

        public int UploadCount => Items.Count(item => item.RequiresUpload);

        public int RemoteFolderCreateCount => Items.Count(item => item.RequiresRemoteFolderCreate);

        public int RemoteDeleteCount => Items.Count(item => item.RequiresRemoteDelete);

        public int ManifestRemovalCount => Items.Count(item => item.RemovesManifestOnly);

        public int BlockedCount => Items.Count(item => item.IsBlocked);

        public int ConflictCount => Items.Count(item => item.IsConflict);

        public int NoOpCount => Items.Count(item => item.IsNoOp);

        public bool HasCloudToDeviceChanges => Items.Any(item => item.RequiresCloudToDeviceMutation);

        public bool HasDeviceToCloudChanges => Items.Any(item => item.RequiresDeviceToCloudMutation);

        public bool HasExecutableChanges =>
            Items.Any(item =>
                item.RequiresCloudToDeviceMutation
                || item.RequiresDeviceToCloudMutation
                || item.RemovesManifestOnly);

        public bool HasBlockingItems => BlockedCount > 0;

        public bool HasDestructiveChanges => Items.Any(item => item.IsDestructive);
    }
}
