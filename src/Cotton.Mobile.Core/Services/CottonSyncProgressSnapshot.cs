// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncProgressSnapshot
    {
        private CottonSyncProgressSnapshot(
            Guid rootId,
            CottonSyncProgressStage stage,
            int completedItemCount,
            int? totalItemCount)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            if (!Enum.IsDefined(stage))
            {
                throw new ArgumentOutOfRangeException(nameof(stage), "Sync progress stage is not supported.");
            }

            if (completedItemCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedItemCount),
                    "Completed sync item count cannot be negative.");
            }

            if (totalItemCount.HasValue && totalItemCount.Value < completedItemCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalItemCount),
                    "Total sync item count cannot be less than the completed count.");
            }

            if (stage == CottonSyncProgressStage.ApplyingChanges && !totalItemCount.HasValue)
            {
                throw new ArgumentException("Applying sync changes requires a total item count.", nameof(totalItemCount));
            }

            if (stage != CottonSyncProgressStage.ApplyingChanges
                && (completedItemCount != 0 || totalItemCount.HasValue))
            {
                throw new ArgumentException("Indeterminate sync stages cannot contain item counts.");
            }

            RootId = rootId;
            Stage = stage;
            CompletedItemCount = completedItemCount;
            TotalItemCount = totalItemCount;
        }

        public Guid RootId { get; }

        public CottonSyncProgressStage Stage { get; }

        public int CompletedItemCount { get; }

        public int? TotalItemCount { get; }

        public static CottonSyncProgressSnapshot ScanningDevice(Guid rootId)
        {
            return new CottonSyncProgressSnapshot(
                rootId,
                CottonSyncProgressStage.ScanningDevice,
                completedItemCount: 0,
                totalItemCount: null);
        }

        public static CottonSyncProgressSnapshot CheckingCloud(Guid rootId)
        {
            return new CottonSyncProgressSnapshot(
                rootId,
                CottonSyncProgressStage.CheckingCloud,
                completedItemCount: 0,
                totalItemCount: null);
        }

        public static CottonSyncProgressSnapshot ApplyingChanges(
            Guid rootId,
            int completedItemCount,
            int totalItemCount)
        {
            return new CottonSyncProgressSnapshot(
                rootId,
                CottonSyncProgressStage.ApplyingChanges,
                completedItemCount,
                totalItemCount);
        }
    }
}
