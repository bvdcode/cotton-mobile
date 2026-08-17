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
            int? totalItemCount,
            CottonSyncTransferSnapshot? transfer)
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

            if (stage is CottonSyncProgressStage.ApplyingChanges or CottonSyncProgressStage.UploadingFile
                && !totalItemCount.HasValue)
            {
                throw new ArgumentException("Sync change progress requires a total item count.", nameof(totalItemCount));
            }

            if (stage == CottonSyncProgressStage.CheckingCloud
                && (completedItemCount != 0 || totalItemCount.HasValue))
            {
                throw new ArgumentException("Cloud checking progress cannot contain item counts.");
            }

            if (stage == CottonSyncProgressStage.ScanningDevice && totalItemCount.HasValue)
            {
                throw new ArgumentException("Device scanning does not have a known total item count.");
            }

            bool requiresTransfer = stage == CottonSyncProgressStage.UploadingFile;
            if (requiresTransfer != (transfer is not null))
            {
                throw new ArgumentException("Only file upload progress can contain transfer details.", nameof(transfer));
            }

            RootId = rootId;
            Stage = stage;
            CompletedItemCount = completedItemCount;
            TotalItemCount = totalItemCount;
            Transfer = transfer;
        }

        public Guid RootId { get; }

        public CottonSyncProgressStage Stage { get; }

        public int CompletedItemCount { get; }

        public int? TotalItemCount { get; }

        public CottonSyncTransferSnapshot? Transfer { get; }

        public static CottonSyncProgressSnapshot ScanningDevice(Guid rootId)
        {
            return new CottonSyncProgressSnapshot(
                rootId,
                CottonSyncProgressStage.ScanningDevice,
                completedItemCount: 0,
                totalItemCount: null,
                transfer: null);
        }

        public static CottonSyncProgressSnapshot ScanningDevice(
            Guid rootId,
            int scannedItemCount)
        {
            return new CottonSyncProgressSnapshot(
                rootId,
                CottonSyncProgressStage.ScanningDevice,
                scannedItemCount,
                totalItemCount: null,
                transfer: null);
        }

        public static CottonSyncProgressSnapshot CheckingCloud(Guid rootId)
        {
            return new CottonSyncProgressSnapshot(
                rootId,
                CottonSyncProgressStage.CheckingCloud,
                completedItemCount: 0,
                totalItemCount: null,
                transfer: null);
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
                totalItemCount,
                transfer: null);
        }

        public static CottonSyncProgressSnapshot UploadingFile(
            Guid rootId,
            int completedItemCount,
            int totalItemCount,
            CottonSyncTransferSnapshot transfer)
        {
            ArgumentNullException.ThrowIfNull(transfer);
            return new CottonSyncProgressSnapshot(
                rootId,
                CottonSyncProgressStage.UploadingFile,
                completedItemCount,
                totalItemCount,
                transfer);
        }
    }
}
