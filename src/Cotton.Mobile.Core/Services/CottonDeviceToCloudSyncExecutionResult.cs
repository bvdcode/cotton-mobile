// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncExecutionResult
    {
        public CottonDeviceToCloudSyncExecutionResult(
            int uploadedCount,
            int confirmedUploadCount,
            int createdFolderCount,
            int deletedLocalFileCount,
            int skippedCount,
            int blockedCount)
        {
            if (uploadedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(uploadedCount), "Uploaded count cannot be negative.");
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

            if (deletedLocalFileCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deletedLocalFileCount),
                    "Deleted local file count cannot be negative.");
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
            CreatedFolderCount = createdFolderCount;
            DeletedLocalFileCount = deletedLocalFileCount;
            SkippedCount = skippedCount;
            BlockedCount = blockedCount;
        }

        public int UploadedCount { get; }

        public int ConfirmedUploadCount { get; }

        public int CreatedFolderCount { get; }

        public int DeletedLocalFileCount { get; }

        public int SkippedCount { get; }

        public int BlockedCount { get; }

        public bool HasAppliedChanges =>
            UploadedCount > 0
            || ConfirmedUploadCount > 0
            || CreatedFolderCount > 0
            || DeletedLocalFileCount > 0;

        public bool HasBlockedItems => BlockedCount > 0;
    }
}
