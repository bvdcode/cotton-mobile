// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class MediaOriginalRestoreScheduler : ICottonAutomaticSyncBackgroundScheduler
    {
        public List<Guid> Roots { get; } = [];

        public Task ScheduleRootRetriesAsync(IReadOnlyCollection<Guid> rootIds, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Roots.AddRange(rootIds);
            return Task.CompletedTask;
        }

        public Task ScheduleAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RescheduleMediaStoreTriggerAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CancelAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
