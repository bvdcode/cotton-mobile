// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonSyncProgressText
    {
        private const double BytesPerKilobyte = 1024;
        private const double BytesPerMegabyte = BytesPerKilobyte * 1024;
        private const double BytesPerGigabyte = BytesPerMegabyte * 1024;

        public static string Create(CottonSyncProgressSnapshot progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            return progress.Stage switch
            {
                CottonSyncProgressStage.ScanningDevice when progress.CompletedItemCount == 1 =>
                    CoreResources.ScanningOneFileStatus,
                CottonSyncProgressStage.ScanningDevice when progress.CompletedItemCount > 1 => CoreResources.Format(
                    CoreResources.ScanningFilesFormat,
                    progress.CompletedItemCount),
                CottonSyncProgressStage.ScanningDevice => CoreResources.ScanningDeviceStatus,
                CottonSyncProgressStage.CheckingCloud => CoreResources.CheckingCloudStatus,
                CottonSyncProgressStage.ApplyingChanges when progress.TotalItemCount > 0 => CoreResources.Format(
                    CoreResources.ApplyingChangesFormat,
                    progress.CompletedItemCount,
                    progress.TotalItemCount.Value),
                CottonSyncProgressStage.ApplyingChanges => CoreResources.FinishingSyncStatus,
                CottonSyncProgressStage.UploadingFile => CreateTransferText(
                    progress.Transfer
                        ?? throw new InvalidOperationException("Uploading progress requires transfer details.")),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(progress),
                    progress.Stage,
                    "Sync progress stage is not supported."),
            };
        }

        private static string CreateTransferText(CottonSyncTransferSnapshot transfer)
        {
            string transferred = FormatBytes(transfer.TransferredBytes);
            if (transfer.TotalBytes.HasValue && transfer.BytesPerSecond.HasValue)
            {
                return CoreResources.Format(
                    CoreResources.UploadingFileWithTotalAndSpeedFormat,
                    transfer.ItemName,
                    transferred,
                    FormatBytes(transfer.TotalBytes.Value),
                    FormatBytes(transfer.BytesPerSecond.Value));
            }

            if (transfer.TotalBytes.HasValue)
            {
                return CoreResources.Format(
                    CoreResources.UploadingFileWithTotalFormat,
                    transfer.ItemName,
                    transferred,
                    FormatBytes(transfer.TotalBytes.Value));
            }

            if (transfer.BytesPerSecond.HasValue)
            {
                return CoreResources.Format(
                    CoreResources.UploadingFileWithSpeedFormat,
                    transfer.ItemName,
                    transferred,
                    FormatBytes(transfer.BytesPerSecond.Value));
            }

            return CoreResources.Format(
                CoreResources.UploadingFileFormat,
                transfer.ItemName,
                transferred);
        }

        private static string FormatBytes(double bytes)
        {
            if (bytes >= BytesPerGigabyte)
            {
                return CoreResources.Format(CoreResources.GigabytesFormat, bytes / BytesPerGigabyte);
            }

            if (bytes >= BytesPerMegabyte)
            {
                return CoreResources.Format(CoreResources.MegabytesFormat, bytes / BytesPerMegabyte);
            }

            if (bytes >= BytesPerKilobyte)
            {
                return CoreResources.Format(CoreResources.KilobytesFormat, bytes / BytesPerKilobyte);
            }

            return CoreResources.Format(CoreResources.BytesFormat, bytes);
        }
    }
}
