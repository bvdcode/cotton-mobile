// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonTransferNotificationFactory
    {
        private const int BackupBlockedNotificationId = 700_001;

        public static CottonLocalNotificationSnapshot CreateCompletedUpload(
            CottonTransferQueueItem transfer)
        {
            ArgumentNullException.ThrowIfNull(transfer);

            string displayName = string.IsNullOrWhiteSpace(transfer.DisplayName)
                ? "Upload"
                : transfer.DisplayName.Trim();
            string destination = transfer.Destination?.Path ?? transfer.Destination?.FolderName ?? string.Empty;
            string message = string.IsNullOrWhiteSpace(destination)
                ? $"{displayName} uploaded."
                : $"{displayName} uploaded to {destination}.";

            return new CottonLocalNotificationSnapshot(
                CreateNotificationId(transfer.Id, CottonLocalNotificationKind.TransferCompleted),
                CottonLocalNotificationKind.TransferCompleted,
                CottonNotificationChannelKind.Transfers,
                "Upload complete",
                message);
        }

        public static CottonLocalNotificationSnapshot CreateFailedUpload(
            CottonTransferQueueItem transfer)
        {
            ArgumentNullException.ThrowIfNull(transfer);

            string displayName = string.IsNullOrWhiteSpace(transfer.DisplayName)
                ? "Upload"
                : transfer.DisplayName.Trim();
            string failure = string.IsNullOrWhiteSpace(transfer.FailureMessage)
                ? "Open transfers for details."
                : transfer.FailureMessage.Trim();

            return new CottonLocalNotificationSnapshot(
                CreateNotificationId(transfer.Id, CottonLocalNotificationKind.TransferFailed),
                CottonLocalNotificationKind.TransferFailed,
                CottonNotificationChannelKind.Transfers,
                "Upload failed",
                $"{displayName}: {failure}");
        }

        public static CottonLocalNotificationSnapshot CreateBackupBlocked(
            string reason)
        {
            string message = string.IsNullOrWhiteSpace(reason)
                ? "Open camera backup for details."
                : reason.Trim();

            return new CottonLocalNotificationSnapshot(
                BackupBlockedNotificationId,
                CottonLocalNotificationKind.BackupBlocked,
                CottonNotificationChannelKind.Backup,
                "Camera backup blocked",
                message);
        }

        private static int CreateNotificationId(
            Guid transferId,
            CottonLocalNotificationKind kind)
        {
            return HashCode.Combine(transferId, kind) & int.MaxValue;
        }
    }
}
