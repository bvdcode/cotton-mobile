// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonDeviceToCloudSyncItemPlanner
    {
        private readonly CottonSyncRootSnapshot _root;
        private readonly CottonDeviceToCloudSyncIndex _index;

        public CottonDeviceToCloudSyncItemPlanner(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncIndex index)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(index);

            _root = root;
            _index = index;
        }

        public CottonDeviceToCloudSyncPlanItem CreateLocalFileItem(
            CottonDeviceToCloudLocalItemSnapshot localFile)
        {
            if (string.IsNullOrWhiteSpace(localFile.LocalSourceId))
            {
                return CottonDeviceToCloudSyncPlanItemFactory.CreateLocal(
                    CottonDeviceToCloudSyncActionKind.BlockedLocalSource,
                    localFile);
            }

            if (!_index.ReceiptsBySource.TryGetValue(
                localFile.LocalSourceId,
                out CottonUploadReceiptSnapshot? receipt))
            {
                return CreateUntrackedLocalFileItem(localFile);
            }

            if (!receipt.MatchesLocalVersion(localFile)
                || !string.Equals(receipt.RelativePath, localFile.RelativePath, StringComparison.Ordinal))
            {
                return CottonDeviceToCloudSyncPlanItemFactory.CreateReceipt(
                    CottonDeviceToCloudSyncActionKind.PendingLocalVersionChanged,
                    receipt);
            }

            if (receipt.IsUploaded)
            {
                return CreateUploadedReceiptItem(receipt);
            }

            if (_index.RemoteByOperation.TryGetValue(
                receipt.OperationId,
                out CottonDeviceToCloudRemoteItemSnapshot? uploadedRemote))
            {
                return CreatePendingConfirmationItem(receipt, uploadedRemote);
            }

            if (_index.RemoteByPath.TryGetValue(
                receipt.RelativePath,
                out CottonDeviceToCloudRemoteItemSnapshot? pendingConflict))
            {
                return CottonDeviceToCloudSyncPlanItemFactory.CreateReceiptConflict(
                    receipt,
                    pendingConflict);
            }

            return CottonDeviceToCloudSyncPlanItemFactory.CreateReceipt(
                CottonDeviceToCloudSyncActionKind.UploadNewFile,
                receipt);
        }

        private CottonDeviceToCloudSyncPlanItem CreateUntrackedLocalFileItem(
            CottonDeviceToCloudLocalItemSnapshot localFile)
        {
            if (!_index.RemoteByPath.TryGetValue(
                    localFile.RelativePath,
                    out CottonDeviceToCloudRemoteItemSnapshot? remoteItem))
            {
                return CottonDeviceToCloudSyncPlanItemFactory.CreateLocal(
                    CottonDeviceToCloudSyncActionKind.UploadNewFile,
                    localFile);
            }

            return MatchesLocalContent(localFile, remoteItem.Entry)
                ? CottonDeviceToCloudSyncPlanItemFactory.CreateLocal(
                    CottonDeviceToCloudSyncActionKind.KeepExistingFile,
                    localFile,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag)
                : CottonDeviceToCloudSyncPlanItemFactory.CreateRemoteConflict(localFile, remoteItem);
        }

        private static bool MatchesLocalContent(
            CottonDeviceToCloudLocalItemSnapshot localFile,
            CottonFileBrowserEntry remoteFile)
        {
            return remoteFile.Type == CottonFileBrowserEntryType.File
                && (!localFile.SizeBytes.HasValue || remoteFile.SizeBytes == localFile.SizeBytes)
                && localFile.ContentHash is not null
                && string.Equals(localFile.ContentHash, remoteFile.ContentHash, StringComparison.Ordinal);
        }

        private static CottonDeviceToCloudSyncPlanItem CreatePendingConfirmationItem(
            CottonUploadReceiptSnapshot receipt,
            CottonDeviceToCloudRemoteItemSnapshot remoteItem)
        {
            if (remoteItem.Entry.Type != CottonFileBrowserEntryType.File
                || !string.Equals(remoteItem.RelativePath, receipt.RelativePath, StringComparison.Ordinal))
            {
                return CottonDeviceToCloudSyncPlanItemFactory.CreateReceiptConflict(receipt, remoteItem);
            }

            if ((receipt.SizeBytes.HasValue && remoteItem.Entry.SizeBytes != receipt.SizeBytes)
                || receipt.ContentHash is null
                || !string.Equals(remoteItem.Entry.ContentHash, receipt.ContentHash, StringComparison.Ordinal))
            {
                return CottonDeviceToCloudSyncPlanItemFactory.CreateReceipt(
                    CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision,
                    receipt,
                    remoteItem.Entry.Id,
                    remoteItem.Entry.ETag);
            }

            if (string.IsNullOrWhiteSpace(remoteItem.Entry.ETag))
            {
                return CottonDeviceToCloudSyncPlanItemFactory.CreateReceipt(
                    CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision,
                    receipt,
                    remoteItem.Entry.Id,
                    expectedRemoteETag: null);
            }

            return CottonDeviceToCloudSyncPlanItemFactory.CreateReceipt(
                CottonDeviceToCloudSyncActionKind.ConfirmPendingUpload,
                receipt,
                remoteItem.Entry.Id,
                remoteItem.Entry.ETag);
        }

        private CottonDeviceToCloudSyncPlanItem CreateUploadedReceiptItem(
            CottonUploadReceiptSnapshot receipt)
        {
            if (!_root.DeletesOriginalsAfterUpload)
            {
                return CottonDeviceToCloudSyncPlanItemFactory.CreateReceipt(
                    CottonDeviceToCloudSyncActionKind.KeepExistingFile,
                    receipt);
            }

            if (!_index.RemoteByPath.TryGetValue(
                    receipt.RelativePath,
                    out CottonDeviceToCloudRemoteItemSnapshot? remoteItem)
                || !MatchesUploadedRevision(receipt, remoteItem.Entry))
            {
                return CottonDeviceToCloudSyncPlanItemFactory.CreateReceipt(
                    CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision,
                    receipt);
            }

            return CottonDeviceToCloudSyncPlanItemFactory.CreateReceipt(
                CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile,
                receipt);
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
    }
}
