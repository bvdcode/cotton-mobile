// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class StubCottonNotificationBatchProvider(CottonNotificationBatch? batch) :
        ICottonNotificationBatchProvider
    {
        public int CallCount { get; private set; }

        public CottonNotificationCursor? RequestedCursor { get; private set; }

        public int? RequestedDetailLimit { get; private set; }

        public Task<CottonNotificationBatch?> GetAsync(
            CottonNotificationCursor? cursor,
            int detailLimit,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            RequestedCursor = cursor;
            RequestedDetailLimit = detailLimit;
            return Task.FromResult(batch);
        }
    }
}
