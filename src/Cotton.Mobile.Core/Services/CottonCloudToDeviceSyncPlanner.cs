namespace Cotton.Mobile.Services
{
    public static class CottonCloudToDeviceSyncPlanner
    {
        public static CottonCloudToDeviceSyncPlanSnapshot Create(
            CottonSyncRootSnapshot root,
            CottonFolderContent remoteContent,
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
            Dictionary<Guid, CottonFileBrowserEntry> remoteFilesById = CreateRemoteFileMap(remoteContent);
            List<CottonCloudToDeviceSyncPlanItem> items = [];

            foreach (CottonFileBrowserEntry entry in remoteContent.Entries
                .OrderBy(entry => entry.Type)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Id))
            {
                items.Add(CreateRemoteItem(entry, localByFileId));
            }

            foreach (CottonSyncedFileSnapshot localFile in localByFileId.Values
                .Where(localFile => !remoteFilesById.ContainsKey(localFile.FileId))
                .OrderBy(localFile => localFile.FileName, StringComparer.OrdinalIgnoreCase)
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
                    localFile.ContentType));
            }

            return new CottonCloudToDeviceSyncPlanSnapshot(
                root.Id,
                remoteContent.FolderId,
                remoteContent.FolderName,
                items);
        }

        private static CottonCloudToDeviceSyncPlanItem CreateRemoteItem(
            CottonFileBrowserEntry entry,
            IReadOnlyDictionary<Guid, CottonSyncedFileSnapshot> localByFileId)
        {
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
                    contentType: null);
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
                    entry.ContentType);
            }

            if (!localByFileId.TryGetValue(entry.Id, out CottonSyncedFileSnapshot? localFile))
            {
                return CreateFileItem(CottonCloudToDeviceSyncActionKind.DownloadNewFile, entry);
            }

            if (!string.Equals(localFile.ETag, entry.ETag.Trim(), StringComparison.Ordinal))
            {
                return CreateFileItem(CottonCloudToDeviceSyncActionKind.RefreshChangedFile, entry);
            }

            if (!string.Equals(localFile.FileName, entry.Name, StringComparison.Ordinal))
            {
                return CreateFileItem(CottonCloudToDeviceSyncActionKind.RenameLocalFile, entry);
            }

            return CreateFileItem(CottonCloudToDeviceSyncActionKind.KeepExistingFile, entry);
        }

        private static CottonCloudToDeviceSyncPlanItem CreateFileItem(
            CottonCloudToDeviceSyncActionKind action,
            CottonFileBrowserEntry entry)
        {
            return new CottonCloudToDeviceSyncPlanItem(
                action,
                CottonFileBrowserEntryType.File,
                entry.Id,
                entry.Name,
                entry.ETag,
                entry.UpdatedAtUtc,
                entry.SizeBytes,
                entry.ContentType);
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

        private static Dictionary<Guid, CottonFileBrowserEntry> CreateRemoteFileMap(CottonFolderContent remoteContent)
        {
            var result = new Dictionary<Guid, CottonFileBrowserEntry>();
            foreach (CottonFileBrowserEntry entry in remoteContent.Entries.Where(entry => entry.Type == CottonFileBrowserEntryType.File))
            {
                if (!result.TryAdd(entry.Id, entry))
                {
                    throw new ArgumentException("Remote folder content contains duplicate file ids.", nameof(remoteContent));
                }
            }

            return result;
        }
    }
}
