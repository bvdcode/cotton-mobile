// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonContentRevisionSnapshot
    {
        public CottonContentRevisionSnapshot(
            string localSourceId,
            long generation,
            string contentHash,
            long? sizeBytes = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localSourceId);
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(generation), "Content generation cannot be negative.");
            }

            if (sizeBytes is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Content size cannot be negative.");
            }

            LocalSourceId = localSourceId.Trim();
            Generation = generation;
            ContentHash = CottonContentHash.NormalizeSha256(contentHash, nameof(contentHash));
            SizeBytes = sizeBytes;
        }

        public string LocalSourceId { get; }

        public long Generation { get; }

        public string ContentHash { get; }

        public long? SizeBytes { get; }
    }
}
