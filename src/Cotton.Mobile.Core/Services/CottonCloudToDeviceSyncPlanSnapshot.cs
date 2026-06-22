// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncPlanSnapshot
    {
        public CottonCloudToDeviceSyncPlanSnapshot(
            Guid syncRootId,
            Guid folderId,
            string folderName,
            IReadOnlyList<CottonCloudToDeviceSyncPlanItem> items)
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

        public IReadOnlyList<CottonCloudToDeviceSyncPlanItem> Items { get; }

        public int DownloadCount => Items.Count(item => item.RequiresDownload);

        public int LocalRenameCount => Items.Count(item => item.RequiresLocalRename);

        public int LocalRemovalCount => Items.Count(item => item.RemovesLocalFile);

        public int BlockedCount => Items.Count(item => item.IsBlocked);

        public int NoOpCount => Items.Count(item => item.IsNoOp);

        public bool HasExecutableChanges => Items.Any(item =>
            item.RequiresDownload || item.RequiresLocalRename || item.RemovesLocalFile);

        public bool HasBlockingItems => BlockedCount > 0;
    }
}
