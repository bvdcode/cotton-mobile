// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonShareTransferEnqueueResult
    {
        public CottonShareTransferEnqueueResult(
            int queuedCount,
            int remainingCaptureCount,
            IReadOnlyList<CottonTransferQueueItem> queuedTransfers)
        {
            ArgumentNullException.ThrowIfNull(queuedTransfers);
            if (queuedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queuedCount));
            }

            if (remainingCaptureCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingCaptureCount));
            }

            QueuedCount = queuedCount;
            RemainingCaptureCount = remainingCaptureCount;
            QueuedTransfers = queuedTransfers;
        }

        public int QueuedCount { get; }

        public int RemainingCaptureCount { get; }

        public IReadOnlyList<CottonTransferQueueItem> QueuedTransfers { get; }

        public bool HasQueuedTransfers => QueuedCount > 0;

        public string StatusText => QueuedCount == 1
            ? "Queued 1 upload."
            : $"Queued {QueuedCount:N0} uploads.";
    }
}
