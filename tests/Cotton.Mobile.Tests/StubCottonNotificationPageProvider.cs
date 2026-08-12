// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class StubCottonNotificationPageProvider(CottonNotificationPage? page) :
        ICottonNotificationPageProvider
    {
        public int CallCount { get; private set; }

        public int? RequestedPageSize { get; private set; }

        public Task<CottonNotificationPage?> GetLatestAsync(
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            RequestedPageSize = pageSize;
            return Task.FromResult(page);
        }
    }
}
