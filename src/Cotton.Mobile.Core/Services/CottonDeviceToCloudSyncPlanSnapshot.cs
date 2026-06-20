namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncPlanSnapshot
    {
        public CottonDeviceToCloudSyncPlanSnapshot(
            Guid syncRootId,
            Guid folderId,
            string folderName,
            IReadOnlyList<CottonDeviceToCloudSyncPlanItem> items)
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

        public IReadOnlyList<CottonDeviceToCloudSyncPlanItem> Items { get; }

        public int UploadCount => Items.Count(item => item.RequiresUpload);

        public int RemoteFolderCreateCount => Items.Count(item => item.RequiresRemoteFolderCreate);

        public int RemoteDeleteCount => Items.Count(item => item.RequiresRemoteDelete);

        public int ManifestRemovalCount => Items.Count(item => item.RemovesManifestOnly);

        public int BlockedCount => Items.Count(item => item.IsBlocked);

        public int NoOpCount => Items.Count(item => item.IsNoOp);

        public bool HasExecutableChanges => Items.Any(item =>
            item.RequiresServerMutation || item.RemovesManifestOnly);

        public bool HasDestructiveChanges => Items.Any(item => item.IsDestructive);

        public bool HasBlockingItems => BlockedCount > 0;
    }
}
