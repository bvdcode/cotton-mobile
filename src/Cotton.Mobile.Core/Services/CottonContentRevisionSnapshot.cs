// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonContentRevisionSnapshot
    {
        public CottonContentRevisionSnapshot(
            string localSourceId,
            long generation,
            string contentHash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localSourceId);
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(generation), "Content generation cannot be negative.");
            }

            LocalSourceId = localSourceId.Trim();
            Generation = generation;
            ContentHash = CottonContentHash.NormalizeSha256(contentHash, nameof(contentHash));
        }

        public string LocalSourceId { get; }

        public long Generation { get; }

        public string ContentHash { get; }
    }
}
