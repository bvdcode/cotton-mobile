namespace Cotton.Mobile.Services
{
    public class CottonSelectedMediaTransferEnqueueResult
    {
        public CottonSelectedMediaTransferEnqueueResult(
            int selectedCount,
            CottonTransferDestinationSnapshot? destination,
            IReadOnlyList<CottonTransferQueueItem> queuedTransfers)
        {
            if (selectedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedCount), "Selected media count cannot be negative.");
            }

            ArgumentNullException.ThrowIfNull(queuedTransfers);
            if (queuedTransfers.Count > selectedCount)
            {
                throw new ArgumentException("Queued media count cannot exceed selected count.", nameof(queuedTransfers));
            }

            SelectedCount = selectedCount;
            Destination = destination;
            QueuedTransfers = queuedTransfers;
        }

        public int SelectedCount { get; }

        public CottonTransferDestinationSnapshot? Destination { get; }

        public IReadOnlyList<CottonTransferQueueItem> QueuedTransfers { get; }

        public int QueuedCount => QueuedTransfers.Count;

        public bool HasQueuedTransfers => QueuedCount > 0;

        public string? FirstQueuedDisplayName => QueuedTransfers.FirstOrDefault()?.DisplayName;
    }
}
