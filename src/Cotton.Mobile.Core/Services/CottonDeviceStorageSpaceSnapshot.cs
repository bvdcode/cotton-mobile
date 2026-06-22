// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceStorageSpaceSnapshot
    {
        private CottonDeviceStorageSpaceSnapshot(
            long? availableBytes,
            long? totalBytes,
            string? unavailableReason)
        {
            if (availableBytes.HasValue && availableBytes.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(availableBytes), "Available bytes cannot be negative.");
            }

            if (totalBytes.HasValue && totalBytes.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalBytes), "Total bytes cannot be negative.");
            }

            if (availableBytes.HasValue && totalBytes.HasValue && availableBytes.Value > totalBytes.Value)
            {
                throw new ArgumentOutOfRangeException(nameof(availableBytes), "Available bytes cannot exceed total bytes.");
            }

            AvailableBytes = availableBytes;
            TotalBytes = totalBytes;
            UnavailableReason = string.IsNullOrWhiteSpace(unavailableReason)
                ? null
                : unavailableReason.Trim();
        }

        public long? AvailableBytes { get; }

        public long? TotalBytes { get; }

        public string? UnavailableReason { get; }

        public bool HasAvailableSpace => AvailableBytes.HasValue;

        public static CottonDeviceStorageSpaceSnapshot Available(long availableBytes, long? totalBytes = null)
        {
            return new CottonDeviceStorageSpaceSnapshot(availableBytes, totalBytes, unavailableReason: null);
        }

        public static CottonDeviceStorageSpaceSnapshot Unavailable(string reason)
        {
            return new CottonDeviceStorageSpaceSnapshot(
                availableBytes: null,
                totalBytes: null,
                string.IsNullOrWhiteSpace(reason) ? "Free device space is unavailable." : reason);
        }
    }
}
