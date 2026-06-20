namespace Cotton.Mobile.Services
{
    public static class CottonDeviceToCloudSyncPlanner
    {
        public static CottonDeviceToCloudSyncPlanSnapshot Create(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot localContent,
            CottonDeviceToCloudRemoteContentSnapshot remoteContent,
            IEnumerable<CottonSyncedFileSnapshot> manifestFiles)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(localContent);
            ArgumentNullException.ThrowIfNull(remoteContent);
            ArgumentNullException.ThrowIfNull(manifestFiles);

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Device-to-cloud sync requires a ready sync root.");
            }

            if (root.Direction == CottonSyncDirection.CloudToDevice)
            {
                throw new InvalidOperationException("Device-to-cloud sync requires a device-to-cloud capable sync root.");
            }

            if (root.CloudFolder.FolderId != remoteContent.FolderId)
            {
                throw new ArgumentException("Remote folder content does not match the sync root cloud folder.", nameof(remoteContent));
            }

            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> localByPath = CreateLocalItemMap(localContent);
            Dictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath = CreateRemotePathMap(remoteContent);
            Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteById = CreateRemoteIdMap(remoteContent);
            Dictionary<string, CottonSyncedFileSnapshot> manifestByPath = CreateManifestPathMap(manifestFiles);
            List<CottonDeviceToCloudSyncPlanItem> items = [];

            foreach (CottonDeviceToCloudLocalProblemSnapshot problem in localContent.Problems
                .OrderBy(problem => problem.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                items.Add(CreateLocalProblemItem(problem));
            }

            foreach (CottonDeviceToCloudLocalItemSnapshot localItem in localContent.Items
                .OrderBy(item => item.ItemType == CottonFileBrowserEntryType.Folder ? 0 : 1)
                .ThenBy(item => GetPathDepth(item.RelativePath))
                .ThenBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                items.Add(CreateLocalItem(localItem, manifestByPath, remoteByPath, remoteById));
            }

            foreach (CottonSyncedFileSnapshot manifestItem in manifestByPath.Values
                .Where(item => !localByPath.ContainsKey(item.RelativePath))
                .OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.FileId))
            {
                items.Add(CreateMissingLocalItem(manifestItem, remoteById));
            }

            return new CottonDeviceToCloudSyncPlanSnapshot(
                root.Id,
                remoteContent.FolderId,
                remoteContent.FolderName,
                items);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateLocalProblemItem(
            CottonDeviceToCloudLocalProblemSnapshot problem)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                CottonDeviceToCloudSyncActionKind.BlockedLocalItemName,
                problem.ItemType,
                problem.DisplayName,
                problem.RelativePath,
                cloudItemId: null,
                expectedRemoteETag: null,
                localUpdatedAtUtc: null,
                sizeBytes: null,
                contentType: null);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateLocalItem(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            IReadOnlyDictionary<string, CottonSyncedFileSnapshot> manifestByPath,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath,
            IReadOnlyDictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteById)
        {
            if (localItem.ItemType == CottonFileBrowserEntryType.Folder)
            {
                return CreateLocalFolderItem(localItem, remoteByPath);
            }

            if (!manifestByPath.TryGetValue(localItem.RelativePath, out CottonSyncedFileSnapshot? manifestItem))
            {
                return CreateNewLocalFileItem(localItem, remoteByPath);
            }

            if (!remoteById.TryGetValue(manifestItem.FileId, out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CreatePlanItem(
                    CottonDeviceToCloudSyncActionKind.RemoteTargetMissing,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    manifestItem.FileId,
                    manifestItem.ETag,
                    localItem);
            }

            if (remoteItem.Entry.Type != CottonFileBrowserEntryType.File)
            {
                return CreatePlanItem(
                    CottonDeviceToCloudSyncActionKind.RemotePathConflict,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag,
                    localItem);
            }

            if (!RemoteMatchesManifest(remoteItem, manifestItem))
            {
                return CreatePlanItem(
                    CreateRemoteMismatchAction(remoteItem),
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag,
                    localItem);
            }

            if (LocalMatchesManifest(localItem, manifestItem))
            {
                return CreatePlanItem(
                    CottonDeviceToCloudSyncActionKind.KeepExistingFile,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    manifestItem.FileId,
                    manifestItem.ETag,
                    localItem);
            }

            return CreatePlanItem(
                CottonDeviceToCloudSyncActionKind.UploadChangedFile,
                CottonFileBrowserEntryType.File,
                localItem.DisplayName,
                localItem.RelativePath,
                manifestItem.FileId,
                manifestItem.ETag,
                localItem);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateLocalFolderItem(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath)
        {
            if (!remoteByPath.TryGetValue(localItem.RelativePath, out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CreatePlanItem(
                    CottonDeviceToCloudSyncActionKind.CreateRemoteFolder,
                    CottonFileBrowserEntryType.Folder,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    cloudItemId: null,
                    expectedRemoteETag: null,
                    localItem);
            }

            if (remoteItem.Entry.Type == CottonFileBrowserEntryType.Folder)
            {
                return CreatePlanItem(
                    CottonDeviceToCloudSyncActionKind.KeepExistingFolder,
                    CottonFileBrowserEntryType.Folder,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    expectedRemoteETag: null,
                    localItem);
            }

            return CreatePlanItem(
                CottonDeviceToCloudSyncActionKind.RemotePathConflict,
                CottonFileBrowserEntryType.Folder,
                localItem.DisplayName,
                localItem.RelativePath,
                remoteItem.Entry.Id,
                remoteItem.Entry.ETag,
                localItem);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateNewLocalFileItem(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath)
        {
            if (remoteByPath.TryGetValue(localItem.RelativePath, out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CreatePlanItem(
                    CottonDeviceToCloudSyncActionKind.RemotePathConflict,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag,
                    localItem);
            }

            return CreatePlanItem(
                CottonDeviceToCloudSyncActionKind.UploadNewFile,
                CottonFileBrowserEntryType.File,
                localItem.DisplayName,
                localItem.RelativePath,
                cloudItemId: null,
                expectedRemoteETag: null,
                localItem);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateMissingLocalItem(
            CottonSyncedFileSnapshot manifestItem,
            IReadOnlyDictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteById)
        {
            if (!remoteById.TryGetValue(manifestItem.FileId, out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CreateManifestItem(
                    CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan,
                    manifestItem,
                    manifestItem.ETag);
            }

            if (!RemoteMatchesManifest(remoteItem, manifestItem))
            {
                return CreateManifestItem(
                    CreateRemoteMismatchAction(remoteItem),
                    manifestItem,
                    remoteItem.Entry.ETag);
            }

            return CreateManifestItem(
                CottonDeviceToCloudSyncActionKind.DeleteRemoteFile,
                manifestItem,
                manifestItem.ETag);
        }

        private static CottonDeviceToCloudSyncPlanItem CreatePlanItem(
            CottonDeviceToCloudSyncActionKind action,
            CottonFileBrowserEntryType targetType,
            string displayName,
            string relativePath,
            Guid? cloudItemId,
            string? expectedRemoteETag,
            CottonDeviceToCloudLocalItemSnapshot localItem)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                action,
                targetType,
                displayName,
                relativePath,
                cloudItemId,
                expectedRemoteETag,
                localItem.LocalUpdatedAtUtc,
                localItem.SizeBytes,
                localItem.ContentType,
                localItem.LocalSourceId);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateManifestItem(
            CottonDeviceToCloudSyncActionKind action,
            CottonSyncedFileSnapshot manifestItem,
            string? expectedRemoteETag)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                action,
                CottonFileBrowserEntryType.File,
                manifestItem.FileName,
                manifestItem.RelativePath,
                manifestItem.FileId,
                expectedRemoteETag,
                localUpdatedAtUtc: null,
                manifestItem.SizeBytes,
                manifestItem.ContentType);
        }

        private static bool LocalMatchesManifest(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            CottonSyncedFileSnapshot manifestItem)
        {
            return localItem.SizeBytes == manifestItem.SizeBytes
                && CottonLocalFileFreshness.IsFresh(manifestItem.SyncedAtUtc, localItem.LocalUpdatedAtUtc);
        }

        private static bool RemoteMatchesManifest(
            CottonDeviceToCloudRemoteItemSnapshot remoteItem,
            CottonSyncedFileSnapshot manifestItem)
        {
            return remoteItem.Entry.Id == manifestItem.FileId
                && remoteItem.Entry.Type == CottonFileBrowserEntryType.File
                && !string.IsNullOrWhiteSpace(remoteItem.Entry.ETag)
                && string.Equals(remoteItem.Entry.ETag.Trim(), manifestItem.ETag, StringComparison.Ordinal)
                && string.Equals(remoteItem.RelativePath, manifestItem.RelativePath, StringComparison.Ordinal);
        }

        private static CottonDeviceToCloudSyncActionKind CreateRemoteMismatchAction(
            CottonDeviceToCloudRemoteItemSnapshot remoteItem)
        {
            return string.IsNullOrWhiteSpace(remoteItem.Entry.ETag)
                ? CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision
                : CottonDeviceToCloudSyncActionKind.RemoteRevisionChanged;
        }

        private static Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> CreateLocalItemMap(
            CottonDeviceToCloudLocalContentSnapshot localContent)
        {
            var result = new Dictionary<string, CottonDeviceToCloudLocalItemSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (CottonDeviceToCloudLocalItemSnapshot localItem in localContent.Items)
            {
                if (!result.TryAdd(localItem.RelativePath, localItem))
                {
                    throw new ArgumentException("Device-to-cloud local content contains duplicate relative paths.", nameof(localContent));
                }
            }

            return result;
        }

        private static Dictionary<string, CottonDeviceToCloudRemoteItemSnapshot> CreateRemotePathMap(
            CottonDeviceToCloudRemoteContentSnapshot remoteContent)
        {
            var result = new Dictionary<string, CottonDeviceToCloudRemoteItemSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (CottonDeviceToCloudRemoteItemSnapshot remoteItem in remoteContent.Items)
            {
                if (!result.TryAdd(remoteItem.RelativePath, remoteItem))
                {
                    throw new ArgumentException("Device-to-cloud remote content contains duplicate relative paths.", nameof(remoteContent));
                }
            }

            return result;
        }

        private static Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> CreateRemoteIdMap(
            CottonDeviceToCloudRemoteContentSnapshot remoteContent)
        {
            var result = new Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot>();
            foreach (CottonDeviceToCloudRemoteItemSnapshot remoteItem in remoteContent.Items)
            {
                if (remoteItem.Entry.Type != CottonFileBrowserEntryType.File)
                {
                    continue;
                }

                if (!result.TryAdd(remoteItem.Entry.Id, remoteItem))
                {
                    throw new ArgumentException("Device-to-cloud remote content contains duplicate file ids.", nameof(remoteContent));
                }
            }

            return result;
        }

        private static Dictionary<string, CottonSyncedFileSnapshot> CreateManifestPathMap(
            IEnumerable<CottonSyncedFileSnapshot> manifestFiles)
        {
            var result = new Dictionary<string, CottonSyncedFileSnapshot>(StringComparer.OrdinalIgnoreCase);
            var fileIds = new HashSet<Guid>();
            foreach (CottonSyncedFileSnapshot manifestFile in manifestFiles)
            {
                if (!fileIds.Add(manifestFile.FileId))
                {
                    throw new ArgumentException("Device-to-cloud sync manifest contains duplicate file ids.", nameof(manifestFiles));
                }

                if (!result.TryAdd(manifestFile.RelativePath, manifestFile))
                {
                    throw new ArgumentException("Device-to-cloud sync manifest contains duplicate relative paths.", nameof(manifestFiles));
                }
            }

            return result;
        }

        private static int GetPathDepth(string relativePath)
        {
            return relativePath.Count(character => character == '/');
        }
    }
}
