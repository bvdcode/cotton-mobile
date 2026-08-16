// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonMediaAlbumSnapshot
    {
        public CottonMediaAlbumSnapshot(long id, string displayName, int itemCount)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Media album display name is required.", nameof(displayName));
            }

            if (itemCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount), "Media album item count must be positive.");
            }

            Id = id;
            DisplayName = displayName.Trim();
            ItemCount = itemCount;
        }

        public long Id { get; }

        public string DisplayName { get; }

        public int ItemCount { get; }
    }
}
