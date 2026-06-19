namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupTransferEnqueueResult
    {
        public CottonCameraBackupTransferEnqueueResult(
            int scannedCount,
            int queuedCount,
            int skippedExistingTransferCount,
            int missingStreamCount,
            bool missingDestination,
            IReadOnlyList<CottonTransferQueueItem> queuedTransfers)
        {
            ArgumentNullException.ThrowIfNull(queuedTransfers);
            if (scannedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scannedCount));
            }

            if (queuedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queuedCount));
            }

            if (skippedExistingTransferCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skippedExistingTransferCount));
            }

            if (missingStreamCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(missingStreamCount));
            }

            ScannedCount = scannedCount;
            QueuedCount = queuedCount;
            SkippedExistingTransferCount = skippedExistingTransferCount;
            MissingStreamCount = missingStreamCount;
            MissingDestination = missingDestination;
            QueuedTransfers = queuedTransfers;
        }

        public int ScannedCount { get; }

        public int QueuedCount { get; }

        public int SkippedExistingTransferCount { get; }

        public int MissingStreamCount { get; }

        public bool MissingDestination { get; }

        public IReadOnlyList<CottonTransferQueueItem> QueuedTransfers { get; }

        public bool HasQueuedTransfers => QueuedCount > 0;
    }
}
