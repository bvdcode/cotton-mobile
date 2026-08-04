// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonDeviceToCloudSyncPlanner
    {
        private const char RelativePathSeparator = '/';

        public static CottonDeviceToCloudSyncPlanSnapshot Create(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot localContent,
            CottonDeviceToCloudRemoteContentSnapshot remoteContent,
            IEnumerable<CottonUploadReceiptSnapshot> uploadReceipts)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(localContent);
            ArgumentNullException.ThrowIfNull(remoteContent);
            ArgumentNullException.ThrowIfNull(uploadReceipts);

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Upload-only sync requires a ready sync root.");
            }

            if (root.Direction != CottonSyncDirection.DeviceToCloud)
            {
                throw new InvalidOperationException("Upload-only sync requires a device-to-cloud sync root.");
            }

            if (root.CloudFolder.FolderId != remoteContent.FolderId)
            {
                throw new ArgumentException("Remote folder content does not match the sync root cloud folder.", nameof(remoteContent));
            }

            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> localByPath = CreateLocalItemMap(localContent);
            Dictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath = CreateRemotePathMap(remoteContent);
            Dictionary<string, CottonUploadReceiptSnapshot> receiptsBySource = CreateReceiptSourceMap(uploadReceipts);
            Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteByOperation =
                CreateRemoteOperationMap(remoteContent);
            var requiredFolderPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<CottonDeviceToCloudSyncPlanItem> fileItems = [];

            foreach (CottonDeviceToCloudLocalItemSnapshot localFile in localContent.Items
                .Where(item => item.ItemType == CottonFileBrowserEntryType.File)
                .OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                CottonDeviceToCloudSyncPlanItem fileItem = CreateLocalFileItem(
                    root,
                    localFile,
                    receiptsBySource,
                    remoteByPath,
                    remoteByOperation);
                if (fileItem.RequiresUpload
                    && !TryCollectRequiredFolders(
                        fileItem,
                        localByPath,
                        remoteByPath,
                        requiredFolderPaths,
                        out CottonDeviceToCloudRemoteItemSnapshot? conflictingParent))
                {
                    fileItem = CreateParentConflictItem(fileItem, conflictingParent);
                }

                fileItems.Add(fileItem);
            }

            List<CottonDeviceToCloudSyncPlanItem> items = localContent.Problems
                .OrderBy(problem => problem.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Select(CreateLocalProblemItem)
                .ToList();
            items.AddRange(requiredFolderPaths
                .OrderBy(GetPathDepth)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => CreateRequiredFolderItem(localByPath[path], remoteByPath)));
            items.AddRange(fileItems);

            return new CottonDeviceToCloudSyncPlanSnapshot(
                root.Id,
                remoteContent.FolderId,
                remoteContent.FolderName,
                items);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateLocalFileItem(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalItemSnapshot localFile,
            IReadOnlyDictionary<string, CottonUploadReceiptSnapshot> receiptsBySource,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath,
            IReadOnlyDictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteByOperation)
        {
            if (string.IsNullOrWhiteSpace(localFile.LocalSourceId))
            {
                return CreateLocalItem(CottonDeviceToCloudSyncActionKind.BlockedLocalSource, localFile);
            }

            if (!receiptsBySource.TryGetValue(
                localFile.LocalSourceId,
                out CottonUploadReceiptSnapshot? receipt))
            {
                return remoteByPath.TryGetValue(
                    localFile.RelativePath,
                    out CottonDeviceToCloudRemoteItemSnapshot? remoteConflict)
                    ? CreateRemoteConflictItem(localFile, remoteConflict)
                    : CreateLocalItem(CottonDeviceToCloudSyncActionKind.UploadNewFile, localFile);
            }

            if (receipt.IsUploaded)
            {
                CottonDeviceToCloudSyncActionKind action = root.DeletesOriginalsAfterUpload
                    ? CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile
                    : CottonDeviceToCloudSyncActionKind.KeepExistingFile;
                return CreateReceiptItem(action, receipt);
            }

            if (remoteByOperation.TryGetValue(
                receipt.OperationId,
                out CottonDeviceToCloudRemoteItemSnapshot? uploadedRemote))
            {
                return CreatePendingConfirmationItem(receipt, uploadedRemote);
            }

            if (!receipt.MatchesLocalVersion(localFile)
                || !string.Equals(receipt.RelativePath, localFile.RelativePath, StringComparison.Ordinal))
            {
                return CreateReceiptItem(CottonDeviceToCloudSyncActionKind.PendingLocalVersionChanged, receipt);
            }

            if (remoteByPath.TryGetValue(
                receipt.RelativePath,
                out CottonDeviceToCloudRemoteItemSnapshot? pendingConflict))
            {
                return CreateReceiptConflictItem(receipt, pendingConflict);
            }

            return CreateReceiptItem(CottonDeviceToCloudSyncActionKind.UploadNewFile, receipt);
        }

        private static CottonDeviceToCloudSyncPlanItem CreatePendingConfirmationItem(
            CottonUploadReceiptSnapshot receipt,
            CottonDeviceToCloudRemoteItemSnapshot remoteItem)
        {
            if (remoteItem.Entry.Type != CottonFileBrowserEntryType.File
                || !string.Equals(remoteItem.RelativePath, receipt.RelativePath, StringComparison.Ordinal))
            {
                return CreateReceiptConflictItem(receipt, remoteItem);
            }

            if (string.IsNullOrWhiteSpace(remoteItem.Entry.ETag))
            {
                return CreateReceiptItem(
                    CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision,
                    receipt,
                    remoteItem.Entry.Id,
                    expectedRemoteETag: null);
            }

            return CreateReceiptItem(
                CottonDeviceToCloudSyncActionKind.ConfirmPendingUpload,
                receipt,
                remoteItem.Entry.Id,
                remoteItem.Entry.ETag);
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

        private static CottonDeviceToCloudSyncPlanItem CreateRequiredFolderItem(
            CottonDeviceToCloudLocalItemSnapshot localFolder,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath)
        {
            if (!remoteByPath.TryGetValue(
                localFolder.RelativePath,
                out CottonDeviceToCloudRemoteItemSnapshot? remoteFolder))
            {
                return CreateLocalItem(CottonDeviceToCloudSyncActionKind.CreateRemoteFolder, localFolder);
            }

            return CreateLocalItem(
                CottonDeviceToCloudSyncActionKind.KeepExistingFolder,
                localFolder,
                remoteFolder.Entry.Id);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateParentConflictItem(
            CottonDeviceToCloudSyncPlanItem fileItem,
            CottonDeviceToCloudRemoteItemSnapshot? conflictingParent)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                conflictingParent is null
                    ? CottonDeviceToCloudSyncActionKind.BlockedLocalSource
                    : CottonDeviceToCloudSyncActionKind.RemotePathConflict,
                CottonFileBrowserEntryType.File,
                fileItem.DisplayName,
                fileItem.RelativePath,
                conflictingParent?.Entry.Id,
                conflictingParent?.Entry.ETag,
                fileItem.LocalUpdatedAtUtc,
                fileItem.SizeBytes,
                fileItem.ContentType,
                fileItem.LocalSourceId,
                fileItem.UploadOperationId);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateRemoteConflictItem(
            CottonDeviceToCloudLocalItemSnapshot localFile,
            CottonDeviceToCloudRemoteItemSnapshot remoteItem)
        {
            return CreateLocalItem(
                CottonDeviceToCloudSyncActionKind.RemotePathConflict,
                localFile,
                remoteItem.Entry.Id,
                remoteItem.Entry.ETag);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateReceiptConflictItem(
            CottonUploadReceiptSnapshot receipt,
            CottonDeviceToCloudRemoteItemSnapshot remoteItem)
        {
            return CreateReceiptItem(
                CottonDeviceToCloudSyncActionKind.RemotePathConflict,
                receipt,
                remoteItem.Entry.Id,
                remoteItem.Entry.ETag);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateLocalItem(
            CottonDeviceToCloudSyncActionKind action,
            CottonDeviceToCloudLocalItemSnapshot localItem,
            Guid? cloudItemId = null,
            string? expectedRemoteETag = null)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                action,
                localItem.ItemType,
                localItem.DisplayName,
                localItem.RelativePath,
                cloudItemId,
                expectedRemoteETag,
                localItem.LocalUpdatedAtUtc,
                localItem.SizeBytes,
                localItem.ContentType,
                localItem.LocalSourceId);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateReceiptItem(
            CottonDeviceToCloudSyncActionKind action,
            CottonUploadReceiptSnapshot receipt,
            Guid? cloudItemId = null,
            string? expectedRemoteETag = null)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                action,
                CottonFileBrowserEntryType.File,
                CottonSyncRelativePath.GetFileName(receipt.RelativePath),
                receipt.RelativePath,
                cloudItemId ?? receipt.RemoteFileId,
                expectedRemoteETag ?? receipt.RemoteETag,
                receipt.LocalUpdatedAtUtc,
                receipt.SizeBytes,
                receipt.ContentType,
                receipt.LocalSourceId,
                receipt.OperationId);
        }

        private static bool TryCollectRequiredFolders(
            CottonDeviceToCloudSyncPlanItem fileItem,
            IReadOnlyDictionary<string, CottonDeviceToCloudLocalItemSnapshot> localByPath,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath,
            ISet<string> requiredFolderPaths,
            out CottonDeviceToCloudRemoteItemSnapshot? conflictingParent)
        {
            foreach (string parentPath in GetParentPaths(fileItem.RelativePath))
            {
                if (!localByPath.TryGetValue(parentPath, out CottonDeviceToCloudLocalItemSnapshot? localFolder)
                    || localFolder.ItemType != CottonFileBrowserEntryType.Folder)
                {
                    conflictingParent = null;
                    return false;
                }

                if (remoteByPath.TryGetValue(parentPath, out CottonDeviceToCloudRemoteItemSnapshot? remoteFolder)
                    && remoteFolder.Entry.Type != CottonFileBrowserEntryType.Folder)
                {
                    conflictingParent = remoteFolder;
                    return false;
                }

                requiredFolderPaths.Add(parentPath);
            }

            conflictingParent = null;
            return true;
        }

        private static IReadOnlyList<string> GetParentPaths(string relativePath)
        {
            var paths = new List<string>();
            string currentPath = relativePath;
            while (currentPath.LastIndexOf(RelativePathSeparator) is int separatorIndex && separatorIndex >= 0)
            {
                currentPath = currentPath[..separatorIndex];
                paths.Add(currentPath);
            }

            paths.Reverse();
            return paths;
        }

        private static Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> CreateLocalItemMap(
            CottonDeviceToCloudLocalContentSnapshot localContent)
        {
            var result = new Dictionary<string, CottonDeviceToCloudLocalItemSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (CottonDeviceToCloudLocalItemSnapshot localItem in localContent.Items)
            {
                if (!result.TryAdd(localItem.RelativePath, localItem))
                {
                    throw new ArgumentException("Upload-only local content contains duplicate relative paths.", nameof(localContent));
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
                    throw new ArgumentException("Upload-only remote content contains duplicate relative paths.", nameof(remoteContent));
                }
            }

            return result;
        }

        private static Dictionary<string, CottonUploadReceiptSnapshot> CreateReceiptSourceMap(
            IEnumerable<CottonUploadReceiptSnapshot> uploadReceipts)
        {
            var result = new Dictionary<string, CottonUploadReceiptSnapshot>(StringComparer.Ordinal);
            var operationIds = new HashSet<Guid>();
            foreach (CottonUploadReceiptSnapshot receipt in uploadReceipts)
            {
                if (!result.TryAdd(receipt.LocalSourceId, receipt))
                {
                    throw new ArgumentException("Upload receipt journal contains duplicate local source ids.", nameof(uploadReceipts));
                }

                if (!operationIds.Add(receipt.OperationId))
                {
                    throw new ArgumentException("Upload receipt journal contains duplicate operation ids.", nameof(uploadReceipts));
                }
            }

            return result;
        }

        private static Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> CreateRemoteOperationMap(
            CottonDeviceToCloudRemoteContentSnapshot remoteContent)
        {
            var result = new Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot>();
            foreach (CottonDeviceToCloudRemoteItemSnapshot remoteItem in remoteContent.Items)
            {
                if (remoteItem.Entry.Type != CottonFileBrowserEntryType.File
                    || !remoteItem.Entry.Metadata.TryGetValue(
                        CottonFileUploadMetadataKeys.UploadOperationId,
                        out string? operationValue)
                    || !Guid.TryParseExact(operationValue, "N", out Guid operationId))
                {
                    continue;
                }

                if (!result.TryAdd(operationId, remoteItem))
                {
                    throw new ArgumentException("Remote content contains duplicate upload operation ids.", nameof(remoteContent));
                }
            }

            return result;
        }

        private static int GetPathDepth(string relativePath)
        {
            return relativePath.Count(character => character == RelativePathSeparator);
        }
    }
}
