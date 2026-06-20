namespace Cotton.Mobile.Services
{
    public static class CottonBidirectionalSyncPlanner
    {
        public static CottonBidirectionalSyncPlanSnapshot Create(
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
                throw new InvalidOperationException("Bidirectional sync requires a ready sync root.");
            }

            if (root.Direction != CottonSyncDirection.Bidirectional)
            {
                throw new InvalidOperationException("Bidirectional sync requires a bidirectional sync root.");
            }

            if (root.CloudFolder.FolderId != remoteContent.FolderId)
            {
                throw new ArgumentException("Remote folder content does not match the sync root cloud folder.", nameof(remoteContent));
            }

            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> localByPath = CreateLocalItemMap(localContent);
            Dictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath = CreateRemotePathMap(remoteContent);
            Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteById = CreateRemoteIdMap(remoteContent);
            Dictionary<string, CottonSyncedFileSnapshot> manifestByPath = CreateManifestPathMap(manifestFiles);
            var handledRemoteIds = new HashSet<Guid>();
            var handledManifestPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<CottonBidirectionalSyncPlanItem> items = [];

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
                CottonBidirectionalSyncPlanItem item = CreateLocalItem(
                    localItem,
                    manifestByPath,
                    remoteByPath,
                    remoteById,
                    handledManifestPaths);
                items.Add(item);
                if (item.CloudItemId.HasValue)
                {
                    handledRemoteIds.Add(item.CloudItemId.Value);
                }
            }

            foreach (CottonSyncedFileSnapshot manifestItem in manifestByPath.Values
                .Where(item => !handledManifestPaths.Contains(item.RelativePath))
                .OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.FileId))
            {
                CottonBidirectionalSyncPlanItem item = CreateMissingLocalItem(manifestItem, remoteById);
                items.Add(item);
                if (item.CloudItemId.HasValue)
                {
                    handledRemoteIds.Add(item.CloudItemId.Value);
                }
            }

            foreach (CottonDeviceToCloudRemoteItemSnapshot remoteItem in remoteContent.Items
                .Where(item => !handledRemoteIds.Contains(item.Entry.Id))
                .OrderBy(item => item.Entry.Type == CottonFileBrowserEntryType.Folder ? 0 : 1)
                .ThenBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                if (localByPath.ContainsKey(remoteItem.RelativePath))
                {
                    continue;
                }

                items.Add(CreateRemoteOnlyItem(remoteItem));
            }

            return new CottonBidirectionalSyncPlanSnapshot(
                root.Id,
                remoteContent.FolderId,
                remoteContent.FolderName,
                items);
        }

        private static CottonBidirectionalSyncPlanItem CreateLocalProblemItem(
            CottonDeviceToCloudLocalProblemSnapshot problem)
        {
            return new CottonBidirectionalSyncPlanItem(
                CottonBidirectionalSyncActionKind.BlockedLocalItemName,
                problem.ItemType,
                problem.DisplayName,
                problem.RelativePath,
                previousRelativePath: null,
                cloudItemId: null,
                expectedRemoteETag: null,
                localUpdatedAtUtc: null,
                remoteUpdatedAtUtc: null,
                sizeBytes: null,
                contentType: null);
        }

        private static CottonBidirectionalSyncPlanItem CreateLocalItem(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            IReadOnlyDictionary<string, CottonSyncedFileSnapshot> manifestByPath,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath,
            IReadOnlyDictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteById,
            ISet<string> handledManifestPaths)
        {
            if (localItem.ItemType == CottonFileBrowserEntryType.Folder)
            {
                return CreateLocalFolderItem(localItem, remoteByPath);
            }

            if (!manifestByPath.TryGetValue(localItem.RelativePath, out CottonSyncedFileSnapshot? manifestItem))
            {
                return CreateNewLocalFileItem(localItem, remoteByPath);
            }

            handledManifestPaths.Add(manifestItem.RelativePath);
            bool localChanged = !LocalMatchesManifest(localItem, manifestItem);
            if (!remoteById.TryGetValue(manifestItem.FileId, out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return localChanged
                    ? CreatePlanItem(
                        CottonBidirectionalSyncActionKind.RemoteTargetMissing,
                        CottonFileBrowserEntryType.File,
                        localItem.DisplayName,
                        localItem.RelativePath,
                        manifestItem.FileId,
                        manifestItem.ETag,
                        localItem,
                        remoteUpdatedAtUtc: null)
                    : CreateManifestItem(
                        CottonBidirectionalSyncActionKind.RemoveLocalFile,
                        manifestItem,
                        manifestItem.ETag,
                        remoteUpdatedAtUtc: null);
            }

            if (remoteItem.Entry.Type != CottonFileBrowserEntryType.File)
            {
                return CreatePlanItem(
                    CottonBidirectionalSyncActionKind.RemotePathConflict,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag,
                    localItem,
                    remoteItem.Entry.UpdatedAtUtc);
            }

            CottonBidirectionalSyncActionKind? remoteMismatch = CreateRemoteMismatchAction(remoteItem, manifestItem);
            if (remoteMismatch.HasValue)
            {
                if (localChanged)
                {
                    return CreatePlanItem(
                        CottonBidirectionalSyncActionKind.FileChangedOnBothSides,
                        CottonFileBrowserEntryType.File,
                        localItem.DisplayName,
                        localItem.RelativePath,
                        remoteItem.Entry.Id,
                        remoteItem.Entry.ETag,
                        localItem,
                        remoteItem.Entry.UpdatedAtUtc);
                }

                return CreateRemoteFileItem(
                    CreateLocalRemoteMismatchAction(remoteMismatch.Value),
                    remoteItem,
                    manifestItem.RelativePath);
            }

            if (localChanged)
            {
                return CreatePlanItem(
                    CottonBidirectionalSyncActionKind.UploadChangedFile,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    manifestItem.FileId,
                    manifestItem.ETag,
                    localItem,
                    remoteItem.Entry.UpdatedAtUtc);
            }

            return CreateRemoteFileItem(CottonBidirectionalSyncActionKind.KeepExistingFile, remoteItem);
        }

        private static CottonBidirectionalSyncPlanItem CreateLocalFolderItem(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath)
        {
            if (!remoteByPath.TryGetValue(localItem.RelativePath, out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CreatePlanItem(
                    CottonBidirectionalSyncActionKind.CreateRemoteFolder,
                    CottonFileBrowserEntryType.Folder,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    cloudItemId: null,
                    expectedRemoteETag: null,
                    localItem,
                    remoteUpdatedAtUtc: null);
            }

            if (remoteItem.Entry.Type == CottonFileBrowserEntryType.Folder)
            {
                return CreatePlanItem(
                    CottonBidirectionalSyncActionKind.KeepExistingFolder,
                    CottonFileBrowserEntryType.Folder,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    expectedRemoteETag: null,
                    localItem,
                    remoteItem.Entry.UpdatedAtUtc);
            }

            return CreatePlanItem(
                CottonBidirectionalSyncActionKind.RemotePathConflict,
                CottonFileBrowserEntryType.Folder,
                localItem.DisplayName,
                localItem.RelativePath,
                remoteItem.Entry.Id,
                remoteItem.Entry.ETag,
                localItem,
                remoteItem.Entry.UpdatedAtUtc);
        }

        private static CottonBidirectionalSyncPlanItem CreateNewLocalFileItem(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath)
        {
            if (remoteByPath.TryGetValue(localItem.RelativePath, out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CreatePlanItem(
                    CottonBidirectionalSyncActionKind.RemotePathConflict,
                    CottonFileBrowserEntryType.File,
                    localItem.DisplayName,
                    localItem.RelativePath,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag,
                    localItem,
                    remoteItem.Entry.UpdatedAtUtc);
            }

            return CreatePlanItem(
                CottonBidirectionalSyncActionKind.UploadNewFile,
                CottonFileBrowserEntryType.File,
                localItem.DisplayName,
                localItem.RelativePath,
                cloudItemId: null,
                expectedRemoteETag: null,
                localItem,
                remoteUpdatedAtUtc: null);
        }

        private static CottonBidirectionalSyncPlanItem CreateMissingLocalItem(
            CottonSyncedFileSnapshot manifestItem,
            IReadOnlyDictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteById)
        {
            if (!remoteById.TryGetValue(manifestItem.FileId, out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CreateManifestItem(
                    CottonBidirectionalSyncActionKind.RemoveManifestOrphan,
                    manifestItem,
                    manifestItem.ETag,
                    remoteUpdatedAtUtc: null);
            }

            CottonBidirectionalSyncActionKind? remoteMismatch = CreateRemoteMismatchAction(remoteItem, manifestItem);
            if (remoteMismatch.HasValue)
            {
                return CreateManifestItem(
                    remoteMismatch.Value == CottonBidirectionalSyncActionKind.NeedsFreshServerRevision
                        ? CottonBidirectionalSyncActionKind.NeedsFreshServerRevision
                        : CottonBidirectionalSyncActionKind.RemoteTargetMissing,
                    manifestItem,
                    remoteItem.Entry.ETag,
                    remoteItem.Entry.UpdatedAtUtc);
            }

            return CreateManifestItem(
                CottonBidirectionalSyncActionKind.DeleteRemoteFile,
                manifestItem,
                manifestItem.ETag,
                remoteItem.Entry.UpdatedAtUtc);
        }

        private static CottonBidirectionalSyncPlanItem CreateRemoteOnlyItem(
            CottonDeviceToCloudRemoteItemSnapshot remoteItem)
        {
            if (remoteItem.Entry.Type == CottonFileBrowserEntryType.Folder)
            {
                return CreateRemoteFileItem(CottonBidirectionalSyncActionKind.BlockedRemoteFolder, remoteItem);
            }

            return string.IsNullOrWhiteSpace(remoteItem.Entry.ETag)
                ? CreateRemoteFileItem(CottonBidirectionalSyncActionKind.NeedsFreshServerRevision, remoteItem)
                : CreateRemoteFileItem(CottonBidirectionalSyncActionKind.DownloadNewFile, remoteItem);
        }

        private static CottonBidirectionalSyncPlanItem CreatePlanItem(
            CottonBidirectionalSyncActionKind action,
            CottonFileBrowserEntryType targetType,
            string displayName,
            string relativePath,
            Guid? cloudItemId,
            string? expectedRemoteETag,
            CottonDeviceToCloudLocalItemSnapshot localItem,
            DateTime? remoteUpdatedAtUtc)
        {
            return new CottonBidirectionalSyncPlanItem(
                action,
                targetType,
                displayName,
                relativePath,
                previousRelativePath: null,
                cloudItemId,
                expectedRemoteETag,
                localItem.LocalUpdatedAtUtc,
                remoteUpdatedAtUtc,
                localItem.SizeBytes,
                localItem.ContentType,
                localItem.LocalSourceId);
        }

        private static CottonBidirectionalSyncPlanItem CreateManifestItem(
            CottonBidirectionalSyncActionKind action,
            CottonSyncedFileSnapshot manifestItem,
            string? expectedRemoteETag,
            DateTime? remoteUpdatedAtUtc)
        {
            return new CottonBidirectionalSyncPlanItem(
                action,
                CottonFileBrowserEntryType.File,
                manifestItem.FileName,
                manifestItem.RelativePath,
                previousRelativePath: null,
                manifestItem.FileId,
                expectedRemoteETag,
                localUpdatedAtUtc: null,
                remoteUpdatedAtUtc,
                manifestItem.SizeBytes,
                manifestItem.ContentType);
        }

        private static CottonBidirectionalSyncPlanItem CreateRemoteFileItem(
            CottonBidirectionalSyncActionKind action,
            CottonDeviceToCloudRemoteItemSnapshot remoteItem,
            string? previousRelativePath = null)
        {
            CottonFileBrowserEntry entry = remoteItem.Entry;
            return new CottonBidirectionalSyncPlanItem(
                action,
                entry.Type,
                entry.Name,
                remoteItem.RelativePath,
                previousRelativePath,
                entry.Id,
                entry.ETag,
                localUpdatedAtUtc: null,
                entry.UpdatedAtUtc,
                entry.SizeBytes,
                entry.ContentType);
        }

        private static bool LocalMatchesManifest(
            CottonDeviceToCloudLocalItemSnapshot localItem,
            CottonSyncedFileSnapshot manifestItem)
        {
            return localItem.SizeBytes == manifestItem.SizeBytes
                && CottonLocalFileFreshness.IsFresh(manifestItem.SyncedAtUtc, localItem.LocalUpdatedAtUtc);
        }

        private static CottonBidirectionalSyncActionKind? CreateRemoteMismatchAction(
            CottonDeviceToCloudRemoteItemSnapshot remoteItem,
            CottonSyncedFileSnapshot manifestItem)
        {
            if (remoteItem.Entry.Id != manifestItem.FileId
                || remoteItem.Entry.Type != CottonFileBrowserEntryType.File)
            {
                return CottonBidirectionalSyncActionKind.RemotePathConflict;
            }

            if (string.IsNullOrWhiteSpace(remoteItem.Entry.ETag))
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

        private static CottonBidirectionalSyncActionKind CreateLocalRemoteMismatchAction(
            CottonBidirectionalSyncActionKind remoteMismatchAction)
        {
            return remoteMismatchAction switch
            {
                CottonBidirectionalSyncActionKind.NeedsFreshServerRevision =>
                    CottonBidirectionalSyncActionKind.NeedsFreshServerRevision,
                CottonBidirectionalSyncActionKind.RenameLocalFile =>
                    CottonBidirectionalSyncActionKind.RenameLocalFile,
                _ => CottonBidirectionalSyncActionKind.RefreshLocalFile,
            };
        }

        private static Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> CreateLocalItemMap(
            CottonDeviceToCloudLocalContentSnapshot localContent)
        {
            var result = new Dictionary<string, CottonDeviceToCloudLocalItemSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (CottonDeviceToCloudLocalItemSnapshot localItem in localContent.Items)
            {
                if (!result.TryAdd(localItem.RelativePath, localItem))
                {
                    throw new ArgumentException("Bidirectional local content contains duplicate relative paths.", nameof(localContent));
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
                    throw new ArgumentException("Bidirectional remote content contains duplicate relative paths.", nameof(remoteContent));
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
                    throw new ArgumentException("Bidirectional remote content contains duplicate file ids.", nameof(remoteContent));
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
                    throw new ArgumentException("Bidirectional manifest contains duplicate file ids.", nameof(manifestFiles));
                }

                if (!result.TryAdd(manifestFile.RelativePath, manifestFile))
                {
                    throw new ArgumentException("Bidirectional manifest contains duplicate relative paths.", nameof(manifestFiles));
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
