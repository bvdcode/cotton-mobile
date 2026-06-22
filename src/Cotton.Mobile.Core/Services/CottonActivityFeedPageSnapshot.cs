// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonActivityFeedPageSnapshot
    {
        public CottonActivityFeedPageSnapshot(
            CottonActivityFeedQuery query,
            IReadOnlyList<CottonActivityFeedItemSnapshot> items)
            : this(query, items, totalItemCount: null)
        {
        }

        public CottonActivityFeedPageSnapshot(
            CottonActivityFeedQuery query,
            IReadOnlyList<CottonActivityFeedItemSnapshot> items,
            int? totalItemCount)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(items);
            if (totalItemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalItemCount));
            }

            Query = query;
            Items = items.ToArray();
            TotalItemCount = totalItemCount;
        }

        public CottonActivityFeedQuery Query { get; }

        public IReadOnlyList<CottonActivityFeedItemSnapshot> Items { get; }

        public int? TotalItemCount { get; }

        public bool IsEmpty => Items.Count == 0;

        public bool MayHaveMore
        {
            get
            {
                if (!TotalItemCount.HasValue)
                {
                    return Items.Count == Query.PageSize;
                }

                int loadedItemCount = ((Query.Page - 1) * Query.PageSize) + Items.Count;
                return loadedItemCount < TotalItemCount.Value;
            }
        }
    }
}
