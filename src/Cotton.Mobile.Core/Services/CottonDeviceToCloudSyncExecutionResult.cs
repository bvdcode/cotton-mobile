// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncExecutionResult
    {
        public CottonDeviceToCloudSyncExecutionResult(
            int uploadedCount,
            int confirmedUploadCount,
            int refreshedCount,
            int createdFolderCount,
            int deletedLocalFileCount,
            int deletedRemoteFileCount,
            int removedManifestCount,
            int skippedCount,
            int blockedCount)
        {
            if (uploadedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(uploadedCount), "Uploaded count cannot be negative.");
            }

            if (refreshedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(refreshedCount), "Refreshed count cannot be negative.");
            }

            if (confirmedUploadCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(confirmedUploadCount),
                    "Confirmed upload count cannot be negative.");
            }

            if (createdFolderCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(createdFolderCount), "Created folder count cannot be negative.");
            }

            if (deletedRemoteFileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deletedRemoteFileCount), "Deleted remote file count cannot be negative.");
            }

            if (deletedLocalFileCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deletedLocalFileCount),
                    "Deleted local file count cannot be negative.");
            }

            if (removedManifestCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(removedManifestCount), "Removed manifest count cannot be negative.");
            }

            if (skippedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skippedCount), "Skipped count cannot be negative.");
            }

            if (blockedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockedCount), "Blocked count cannot be negative.");
            }

            UploadedCount = uploadedCount;
            ConfirmedUploadCount = confirmedUploadCount;
            RefreshedCount = refreshedCount;
            CreatedFolderCount = createdFolderCount;
            DeletedLocalFileCount = deletedLocalFileCount;
            DeletedRemoteFileCount = deletedRemoteFileCount;
            RemovedManifestCount = removedManifestCount;
            SkippedCount = skippedCount;
            BlockedCount = blockedCount;
        }

        public int UploadedCount { get; }

        public int ConfirmedUploadCount { get; }

        public int RefreshedCount { get; }

        public int CreatedFolderCount { get; }

        public int DeletedLocalFileCount { get; }

        public int DeletedRemoteFileCount { get; }

        public int RemovedManifestCount { get; }

        public int SkippedCount { get; }

        public int BlockedCount { get; }

        public bool HasAppliedChanges =>
            UploadedCount > 0
            || ConfirmedUploadCount > 0
            || RefreshedCount > 0
            || CreatedFolderCount > 0
            || DeletedLocalFileCount > 0
            || DeletedRemoteFileCount > 0
            || RemovedManifestCount > 0;

        public bool HasBlockedItems => BlockedCount > 0;
    }
}
