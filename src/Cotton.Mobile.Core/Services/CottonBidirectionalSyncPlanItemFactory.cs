// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal static class CottonBidirectionalSyncPlanItemFactory
    {
        public static CottonBidirectionalSyncPlanItem CreateLocalProblem(
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

        public static CottonBidirectionalSyncPlanItem CreateLocal(
            CottonBidirectionalSyncActionKind action,
            CottonFileBrowserEntryType targetType,
            string displayName,
            string relativePath,
            Guid? cloudItemId,
            string? expectedRemoteETag,
            CottonDeviceToCloudLocalItemSnapshot localItem,
            DateTime? remoteUpdatedAtUtc,
            string? remoteContentHash = null)
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
                localItem.LocalSourceId,
                localItem.ContentHash,
                remoteContentHash);
        }

        public static CottonBidirectionalSyncPlanItem CreateManifest(
            CottonBidirectionalSyncActionKind action,
            CottonSyncedFileSnapshot manifestItem,
            string? expectedRemoteETag,
            DateTime? remoteUpdatedAtUtc,
            string? remoteContentHash = null)
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
                manifestItem.ContentType,
                localSourceId: null,
                manifestItem.ContentHash,
                remoteContentHash);
        }

        public static CottonBidirectionalSyncPlanItem CreateRemote(
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
                entry.ContentType,
                localSourceId: null,
                localContentHash: null,
                entry.ContentHash);
        }
    }
}
