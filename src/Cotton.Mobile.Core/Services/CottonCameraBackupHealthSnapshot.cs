// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupHealthSnapshot
    {
        public static CottonCameraBackupHealthSnapshot Empty { get; } = new(0, 0, 0, 0);

        public CottonCameraBackupHealthSnapshot(
            int pendingCount,
            int uploadedCount,
            int failedCount,
            int blockedCount)
        {
            if (pendingCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pendingCount));
            }

            if (uploadedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(uploadedCount));
            }

            if (failedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(failedCount));
            }

            if (blockedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockedCount));
            }

            PendingCount = pendingCount;
            UploadedCount = uploadedCount;
            FailedCount = failedCount;
            BlockedCount = blockedCount;
        }

        public int PendingCount { get; }

        public int UploadedCount { get; }

        public int FailedCount { get; }

        public int BlockedCount { get; }

        public bool HasActivity => PendingCount > 0 || UploadedCount > 0 || FailedCount > 0 || BlockedCount > 0;
    }
}
