// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncTransferSnapshot
    {
        public CottonSyncTransferSnapshot(
            string itemName,
            int uploadNumber,
            int uploadCount,
            long transferredBytes,
            long? totalBytes,
            double? bytesPerSecond)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                throw new ArgumentException("Sync transfer item name is required.", nameof(itemName));
            }

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(uploadNumber);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(uploadCount);
            if (uploadNumber > uploadCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(uploadNumber),
                    "Upload number cannot exceed the upload count.");
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
            UploadNumber = uploadNumber;
            UploadCount = uploadCount;
            TransferredBytes = transferredBytes;
            TotalBytes = totalBytes;
            BytesPerSecond = bytesPerSecond;
        }

        public string ItemName { get; }

        public int UploadNumber { get; }

        public int UploadCount { get; }

        public long TransferredBytes { get; }

        public long? TotalBytes { get; }

        public double? BytesPerSecond { get; }
    }
}
