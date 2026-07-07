// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupScanResult
    {
        public CottonCameraBackupScanResult(
            IReadOnlyList<CottonCameraBackupCandidate> candidates,
            int scannedCount,
            int skippedAlreadyTrackedCount,
            int skippedByPolicyCount)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            if (scannedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scannedCount));
            }

            if (skippedAlreadyTrackedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skippedAlreadyTrackedCount));
            }

            if (skippedByPolicyCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skippedByPolicyCount));
            }

            Candidates = candidates.ToArray();
            ScannedCount = scannedCount;
            SkippedAlreadyTrackedCount = skippedAlreadyTrackedCount;
            SkippedByPolicyCount = skippedByPolicyCount;
        }

        public IReadOnlyList<CottonCameraBackupCandidate> Candidates { get; }

        public int ScannedCount { get; }

        public int SkippedAlreadyTrackedCount { get; }

        public int SkippedByPolicyCount { get; }
    }
}
