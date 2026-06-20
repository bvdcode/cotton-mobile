namespace Cotton.Mobile.Services
{
    public static class CottonTransferHistoryCleanupPolicy
    {
        public static CottonTransferHistoryCleanupPlan CreatePlan(IEnumerable<CottonTransferQueueItem> transfers)
        {
            ArgumentNullException.ThrowIfNull(transfers);

            var retainedItems = new List<CottonTransferQueueItem>();
            int removedCount = 0;
            foreach (CottonTransferQueueItem transfer in transfers)
            {
                if (IsHistoryItem(transfer))
                {
                    removedCount++;
                    continue;
                }

                retainedItems.Add(transfer);
            }

            return new CottonTransferHistoryCleanupPlan(retainedItems, removedCount);
        }

        public static bool IsHistoryItem(CottonTransferQueueItem transfer)
        {
            ArgumentNullException.ThrowIfNull(transfer);

            return transfer.Status is CottonTransferStatus.Completed or CottonTransferStatus.Cancelled;
        }
    }
}
