// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupDestinationStorageEstimate
    {
        public static CottonCameraBackupDestinationStorageEstimate Empty { get; } = new(
            pendingCount: 0,
            knownSizeBytes: 0,
            unknownSizeCount: 0);

        public CottonCameraBackupDestinationStorageEstimate(
            int pendingCount,
            long knownSizeBytes,
            int unknownSizeCount)
        {
            if (pendingCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pendingCount), "Pending count cannot be negative.");
            }

            if (knownSizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(knownSizeBytes), "Known size cannot be negative.");
            }

            if (unknownSizeCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(unknownSizeCount), "Unknown size count cannot be negative.");
            }

            if (unknownSizeCount > pendingCount)
            {
                throw new ArgumentOutOfRangeException(nameof(unknownSizeCount), "Unknown size count cannot exceed pending count.");
            }

            PendingCount = pendingCount;
            KnownSizeBytes = knownSizeBytes;
            UnknownSizeCount = unknownSizeCount;
        }

        public int PendingCount { get; }

        public long KnownSizeBytes { get; }

        public int UnknownSizeCount { get; }

        public int KnownSizeCount => PendingCount - UnknownSizeCount;

        public bool HasPendingItems => PendingCount > 0;

        public bool HasUnknownSizes => UnknownSizeCount > 0;

        public static CottonCameraBackupDestinationStorageEstimate Create(
            CottonCameraBackupScanResult scanResult)
        {
            ArgumentNullException.ThrowIfNull(scanResult);

            long knownSizeBytes = 0;
            int unknownSizeCount = 0;
            foreach (CottonCameraBackupCandidate candidate in scanResult.Candidates)
            {
                if (candidate.Identity.SizeBytes is long sizeBytes)
                {
                    checked
                    {
                        knownSizeBytes += sizeBytes;
                    }
                }
                else
                {
                    unknownSizeCount++;
                }
            }

            return new CottonCameraBackupDestinationStorageEstimate(
                scanResult.Candidates.Count,
                knownSizeBytes,
                unknownSizeCount);
        }
    }
}
