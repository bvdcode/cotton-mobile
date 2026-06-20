namespace Cotton.Mobile.Services
{
    public static class CottonCloudToDeviceSyncPlanner
    {
        public static CottonCloudToDeviceSyncPlanSnapshot Create(
            CottonSyncRootSnapshot root,
            CottonFolderContent remoteContent,
            IEnumerable<CottonSyncedFileSnapshot> localFiles)
        {
            return Create(
                root,
                CottonCloudToDeviceRemoteContentSnapshot.FromFolderContent(remoteContent),
                localFiles);
        }

        public static CottonCloudToDeviceSyncPlanSnapshot Create(
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceRemoteContentSnapshot remoteContent,
            IEnumerable<CottonSyncedFileSnapshot> localFiles)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(remoteContent);
            ArgumentNullException.ThrowIfNull(localFiles);

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Cloud-to-device sync requires a ready sync root.");
            }

            if (root.CloudFolder.FolderId != remoteContent.FolderId)
            {
                throw new ArgumentException("Remote folder content does not match the sync root cloud folder.", nameof(remoteContent));
            }

            Dictionary<Guid, CottonSyncedFileSnapshot> localByFileId = CreateLocalFileMap(localFiles);
            Dictionary<Guid, CottonCloudToDeviceRemoteItemSnapshot> remoteFilesById = CreateRemoteFileMap(remoteContent);
            List<CottonCloudToDeviceSyncPlanItem> items = [];

            foreach (CottonCloudToDeviceRemoteItemSnapshot item in remoteContent.Entries
                .OrderBy(item => item.Entry.Type)
                .ThenBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Entry.Id))
            {
                items.Add(CreateRemoteItem(item, localByFileId));
            }

            foreach (CottonSyncedFileSnapshot localFile in localByFileId.Values
                .Where(localFile => !remoteFilesById.ContainsKey(localFile.FileId))
                .OrderBy(localFile => localFile.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(localFile => localFile.FileId))
            {
                items.Add(new CottonCloudToDeviceSyncPlanItem(
                    CottonCloudToDeviceSyncActionKind.RemoveLocalOrphan,
                    CottonFileBrowserEntryType.File,
                    localFile.FileId,
                    localFile.FileName,
                    localFile.ETag,
                    localFile.RemoteUpdatedAtUtc,
                    localFile.SizeBytes,
                    localFile.ContentType,
                    localFile.RelativePath));
            }

            return new CottonCloudToDeviceSyncPlanSnapshot(
                root.Id,
                remoteContent.FolderId,
                remoteContent.FolderName,
                items);
        }

        private static CottonCloudToDeviceSyncPlanItem CreateRemoteItem(
            CottonCloudToDeviceRemoteItemSnapshot remoteItem,
            IReadOnlyDictionary<Guid, CottonSyncedFileSnapshot> localByFileId)
        {
            CottonFileBrowserEntry entry = remoteItem.Entry;
            if (entry.Type == CottonFileBrowserEntryType.Folder)
            {
                return new CottonCloudToDeviceSyncPlanItem(
                    CottonCloudToDeviceSyncActionKind.BlockedFolder,
                    CottonFileBrowserEntryType.Folder,
                    entry.Id,
                    entry.Name,
                    remoteETag: null,
                    remoteUpdatedAtUtc: null,
                    sizeBytes: null,
                    contentType: null,
                    remoteItem.RelativePath);
            }

            if (string.IsNullOrWhiteSpace(entry.ETag))
            {
                return new CottonCloudToDeviceSyncPlanItem(
                    CottonCloudToDeviceSyncActionKind.NeedsFreshServerRevision,
                    CottonFileBrowserEntryType.File,
                    entry.Id,
                    entry.Name,
                    remoteETag: null,
                    entry.UpdatedAtUtc,
                    entry.SizeBytes,
                    entry.ContentType,
                    remoteItem.RelativePath);
            }

            if (!localByFileId.TryGetValue(entry.Id, out CottonSyncedFileSnapshot? localFile))
            {
                return CreateFileItem(CottonCloudToDeviceSyncActionKind.DownloadNewFile, remoteItem);
            }

            if (!string.Equals(localFile.ETag, entry.ETag.Trim(), StringComparison.Ordinal))
            {
                return CreateFileItem(CottonCloudToDeviceSyncActionKind.RefreshChangedFile, remoteItem);
            }

            if (!string.Equals(localFile.FileName, entry.Name, StringComparison.Ordinal)
                || !string.Equals(localFile.RelativePath, remoteItem.RelativePath, StringComparison.Ordinal))
            {
                return CreateFileItem(
                    CottonCloudToDeviceSyncActionKind.RenameLocalFile,
                    remoteItem,
                    localFile.RelativePath);
            }

            return CreateFileItem(CottonCloudToDeviceSyncActionKind.KeepExistingFile, remoteItem);
        }

        private static CottonCloudToDeviceSyncPlanItem CreateFileItem(
            CottonCloudToDeviceSyncActionKind action,
            CottonCloudToDeviceRemoteItemSnapshot remoteItem,
            string? previousRelativePath = null)
        {
            CottonFileBrowserEntry entry = remoteItem.Entry;
            return new CottonCloudToDeviceSyncPlanItem(
                action,
                CottonFileBrowserEntryType.File,
                entry.Id,
                entry.Name,
                entry.ETag,
                entry.UpdatedAtUtc,
                entry.SizeBytes,
                entry.ContentType,
                remoteItem.RelativePath,
                previousRelativePath);
        }

        private static Dictionary<Guid, CottonSyncedFileSnapshot> CreateLocalFileMap(
            IEnumerable<CottonSyncedFileSnapshot> localFiles)
        {
            var result = new Dictionary<Guid, CottonSyncedFileSnapshot>();
            foreach (CottonSyncedFileSnapshot localFile in localFiles)
            {
                if (!result.TryAdd(localFile.FileId, localFile))
                {
                    throw new ArgumentException("Local sync manifest contains duplicate file ids.", nameof(localFiles));
                }
            }

            return result;
        }

        private static Dictionary<Guid, CottonCloudToDeviceRemoteItemSnapshot> CreateRemoteFileMap(
            CottonCloudToDeviceRemoteContentSnapshot remoteContent)
        {
            var result = new Dictionary<Guid, CottonCloudToDeviceRemoteItemSnapshot>();
            foreach (CottonCloudToDeviceRemoteItemSnapshot item in remoteContent.Entries
                .Where(item => item.Entry.Type == CottonFileBrowserEntryType.File))
            {
                if (!result.TryAdd(item.Entry.Id, item))
                {
                    throw new ArgumentException("Remote folder content contains duplicate file ids.", nameof(remoteContent));
                }
            }

            return result;
        }
    }
}
