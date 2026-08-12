// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class InMemoryCottonNotificationCursorStore(CottonNotificationCursor? cursor) :
        ICottonNotificationCursorStore
    {
        public CottonNotificationCursor? Cursor { get; private set; } = cursor;

        public int SaveCount { get; private set; }

        public Task<CottonNotificationCursor?> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Cursor);
        }

        public Task SaveAsync(
            CottonNotificationCursor cursor,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Cursor = cursor;
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Cursor = null;
            return Task.CompletedTask;
        }
    }
}
