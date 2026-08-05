// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal static class CottonDeviceToCloudSyncPlanItemFactory
    {
        public static CottonDeviceToCloudSyncPlanItem CreateLocalProblem(
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

        public static CottonDeviceToCloudSyncPlanItem CreateRequiredFolder(
            CottonDeviceToCloudLocalItemSnapshot localFolder,
            IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath)
        {
            if (!remoteByPath.TryGetValue(
                localFolder.RelativePath,
                out CottonDeviceToCloudRemoteItemSnapshot? remoteFolder))
            {
                return CreateLocal(CottonDeviceToCloudSyncActionKind.CreateRemoteFolder, localFolder);
            }

            return CreateLocal(
                CottonDeviceToCloudSyncActionKind.KeepExistingFolder,
                localFolder,
                remoteFolder.Entry.Id);
        }

        public static CottonDeviceToCloudSyncPlanItem CreateParentConflict(
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

        public static CottonDeviceToCloudSyncPlanItem CreateRemoteConflict(
            CottonDeviceToCloudLocalItemSnapshot localFile,
            CottonDeviceToCloudRemoteItemSnapshot remoteItem)
        {
            return CreateLocal(
                CottonDeviceToCloudSyncActionKind.RemotePathConflict,
                localFile,
                remoteItem.Entry.Id,
                remoteItem.Entry.ETag);
        }

        public static CottonDeviceToCloudSyncPlanItem CreateReceiptConflict(
            CottonUploadReceiptSnapshot receipt,
            CottonDeviceToCloudRemoteItemSnapshot remoteItem)
        {
            return CreateReceipt(
                CottonDeviceToCloudSyncActionKind.RemotePathConflict,
                receipt,
                remoteItem.Entry.Id,
                remoteItem.Entry.ETag);
        }

        public static CottonDeviceToCloudSyncPlanItem CreateLocal(
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

        public static CottonDeviceToCloudSyncPlanItem CreateReceipt(
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
