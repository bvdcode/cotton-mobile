// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncTransferSnapshot
    {
        public CottonSyncTransferSnapshot(
            string itemName,
            long transferredBytes,
            long? totalBytes,
            double? bytesPerSecond)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                throw new ArgumentException("Sync transfer item name is required.", nameof(itemName));
            }

            ArgumentOutOfRangeException.ThrowIfNegative(transferredBytes);
            if (totalBytes.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(totalBytes.Value);
            }

            if (bytesPerSecond.HasValue && bytesPerSecond.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bytesPerSecond),
                    "Sync transfer speed must be positive.");
            }

            ItemName = itemName.Trim();
            TransferredBytes = transferredBytes;
            TotalBytes = totalBytes;
            BytesPerSecond = bytesPerSecond;
        }

        public string ItemName { get; }

        public long TransferredBytes { get; }

        public long? TotalBytes { get; }

        public double? BytesPerSecond { get; }
    }
}
