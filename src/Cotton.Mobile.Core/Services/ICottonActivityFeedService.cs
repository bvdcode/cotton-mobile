// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonActivityFeedService
    {
        Task<CottonActivityFeedPageSnapshot> GetPageAsync(
            Uri instanceUri,
            CottonActivityFeedQuery query,
            CancellationToken cancellationToken = default);
    }
}
