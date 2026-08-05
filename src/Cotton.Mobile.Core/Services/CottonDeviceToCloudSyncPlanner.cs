// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static partial class CottonDeviceToCloudSyncPlanner
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

            if (!receipt.MatchesLocalVersion(localFile)
                || !string.Equals(receipt.RelativePath, localFile.RelativePath, StringComparison.Ordinal))
            {
                return CreateReceiptItem(CottonDeviceToCloudSyncActionKind.PendingLocalVersionChanged, receipt);
            }

            if (receipt.IsUploaded)
            {
                return CreateUploadedReceiptItem(root, receipt, remoteByPath);
            }

            if (remoteByOperation.TryGetValue(
                receipt.OperationId,
                out CottonDeviceToCloudRemoteItemSnapshot? uploadedRemote))
            {
                return CreatePendingConfirmationItem(receipt, uploadedRemote);
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

            if (receipt.SizeBytes.HasValue && remoteItem.Entry.SizeBytes != receipt.SizeBytes)
            {
                return CreateReceiptItem(
                    CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision,
                    receipt,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag);
            }

            if (receipt.ContentHash is null
                || !string.Equals(remoteItem.Entry.ContentHash, receipt.ContentHash, StringComparison.Ordinal))
            {
                return CreateReceiptItem(
                    CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision,
                    receipt,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag);
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

        private static CottonDeviceToCloudSyncPlanItem CreateUploadedReceiptItem(
            CottonSyncRootSnapshot root,
            CottonUploadReceiptSnapshot receipt,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath)
        {
            if (!root.DeletesOriginalsAfterUpload)
            {
                return CreateReceiptItem(CottonDeviceToCloudSyncActionKind.KeepExistingFile, receipt);
            }

            if (!remoteByPath.TryGetValue(
                    receipt.RelativePath,
                    out CottonDeviceToCloudRemoteItemSnapshot? remoteItem)
                || !MatchesUploadedRevision(receipt, remoteItem.Entry))
            {
                return CreateReceiptItem(CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision, receipt);
            }

            return CreateReceiptItem(CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile, receipt);
        }

        private static bool MatchesUploadedRevision(
            CottonUploadReceiptSnapshot receipt,
            CottonFileBrowserEntry remoteFile)
        {
            return remoteFile.Type == CottonFileBrowserEntryType.File
                && remoteFile.Id == receipt.RemoteFileId
                && string.Equals(remoteFile.ETag, receipt.RemoteETag, StringComparison.Ordinal)
                && (!receipt.SizeBytes.HasValue || remoteFile.SizeBytes == receipt.SizeBytes)
                && receipt.ContentHash is not null
                && string.Equals(remoteFile.ContentHash, receipt.ContentHash, StringComparison.Ordinal);
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
                fileItem.UploadOperationId,
                fileItem.ContentHash);
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
                localItem.LocalSourceId,
                uploadOperationId: null,
                localItem.ContentHash);
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
                receipt.OperationId,
                receipt.ContentHash);
        }

    }
}
