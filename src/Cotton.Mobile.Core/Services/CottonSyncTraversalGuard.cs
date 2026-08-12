// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncTraversalGuard<TIdentifier>
        where TIdentifier : notnull
    {
        public const int DefaultMaximumDepth = 64;
        public const int DefaultMaximumItemCount = 100_000;

        private readonly int _maximumDepth;
        private readonly int _maximumItemCount;
        private readonly HashSet<TIdentifier> _visitedContainerIds = [];
        private int _itemCount;

        public CottonSyncTraversalGuard(
            int maximumDepth = DefaultMaximumDepth,
            int maximumItemCount = DefaultMaximumItemCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maximumDepth);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumItemCount);

            _maximumDepth = maximumDepth;
            _maximumItemCount = maximumItemCount;
        }

        public bool TryEnterContainer(TIdentifier identifier, int depth)
        {
            if (depth < 0 || depth > _maximumDepth)
            {
                throw new InvalidDataException($"Sync tree exceeds the maximum depth of {_maximumDepth}.");
            }

            return _visitedContainerIds.Add(identifier);
        }

        public void RecordItem()
        {
            if (_itemCount >= _maximumItemCount)
            {
                throw new InvalidDataException($"Sync tree exceeds the maximum item count of {_maximumItemCount}.");
            }

            _itemCount++;
        }
    }
}
