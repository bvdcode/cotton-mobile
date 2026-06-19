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

        private static int CreateNotificationId(
            Guid transferId,
            CottonLocalNotificationKind kind)
        {
            return HashCode.Combine(transferId, kind) & int.MaxValue;
        }
    }
}
