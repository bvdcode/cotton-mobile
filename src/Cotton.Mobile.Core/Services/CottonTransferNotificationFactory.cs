namespace Cotton.Mobile.Services
{
    public static class CottonTransferNotificationFactory
    {
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
                ? "Open Transfers for details."
                : transfer.FailureMessage.Trim();

            return new CottonLocalNotificationSnapshot(
                CreateNotificationId(transfer.Id, CottonLocalNotificationKind.TransferFailed),
                CottonLocalNotificationKind.TransferFailed,
                CottonNotificationChannelKind.Transfers,
                "Upload failed",
                $"{displayName}: {failure}");
        }

        private static int CreateNotificationId(
            Guid transferId,
            CottonLocalNotificationKind kind)
        {
            return HashCode.Combine(transferId, kind) & int.MaxValue;
        }
    }
}
