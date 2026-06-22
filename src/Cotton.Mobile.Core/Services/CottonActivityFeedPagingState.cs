// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonActivityFeedPagingState
    {
        private CottonActivityFeedPagingState(
            int currentPage,
            int? totalItemCount,
            bool mayHaveMore)
        {
            if (currentPage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentPage));
            }

            if (totalItemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalItemCount));
            }

            CurrentPage = currentPage;
            TotalItemCount = totalItemCount;
            MayHaveMore = mayHaveMore;
        }

        public static CottonActivityFeedPagingState Empty { get; } = new(
            currentPage: 0,
            totalItemCount: null,
            mayHaveMore: false);

        public int CurrentPage { get; }

        public int NextPage => CurrentPage + 1;

        public int? TotalItemCount { get; }

        public bool MayHaveMore { get; }

        public CottonActivityFeedPagingState ApplyRefresh(CottonActivityFeedPageSnapshot page)
        {
            ArgumentNullException.ThrowIfNull(page);

            return new CottonActivityFeedPagingState(
                page.Query.Page,
                page.TotalItemCount,
                page.MayHaveMore);
        }

        public CottonActivityFeedPagingState ApplyAppend(CottonActivityFeedPageSnapshot page)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (page.Query.Page != NextPage)
            {
                throw new ArgumentException(
                    "Activity feed pages must be appended in order.",
                    nameof(page));
            }

            return new CottonActivityFeedPagingState(
                page.Query.Page,
                page.TotalItemCount ?? TotalItemCount,
                page.MayHaveMore);
        }
    }
}
