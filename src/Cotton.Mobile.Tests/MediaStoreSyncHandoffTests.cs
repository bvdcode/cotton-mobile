// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MediaStoreSyncHandoffTests
    {
        [Fact]
        public async Task TriggerIsRearmedOnlyAfterUploadWorkIsPersisted()
        {
            TaskCompletionSource persisted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            RecordingAutomaticSyncBackgroundScheduler scheduler = new()
            {
                MediaStoreSchedule = token => persisted.Task.WaitAsync(token),
            };
            CottonMediaStoreSyncHandoff handoff = new(scheduler);

            Task queued = handoff.QueueAsync(TestContext.Current.CancellationToken);

            Assert.False(queued.IsCompleted);
            Assert.Equal(0, scheduler.RescheduleMediaStoreCount);
            persisted.SetResult();
            await queued;
            Assert.Equal(1, scheduler.RescheduleMediaStoreCount);
        }

        [Fact]
        public async Task FailedPersistenceDoesNotAcknowledgeContentEvent()
        {
            RecordingAutomaticSyncBackgroundScheduler scheduler = new()
            {
                MediaStoreSchedule = _ => Task.FromException(new IOException("Persistence failed.")),
            };
            CottonMediaStoreSyncHandoff handoff = new(scheduler);

            await Assert.ThrowsAsync<IOException>(() => handoff.QueueAsync(TestContext.Current.CancellationToken));

            Assert.Equal(0, scheduler.RescheduleMediaStoreCount);
        }

        [Fact]
        public async Task CancelledPersistenceDoesNotAcknowledgeContentEvent()
        {
            TaskCompletionSource persisted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenSource cancellation = new();
            RecordingAutomaticSyncBackgroundScheduler scheduler = new()
            {
                MediaStoreSchedule = token => persisted.Task.WaitAsync(token),
            };
            CottonMediaStoreSyncHandoff handoff = new(scheduler);
            Task queued = handoff.QueueAsync(cancellation.Token);

            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);

            Assert.Equal(0, scheduler.RescheduleMediaStoreCount);
        }

        [Fact]
        public async Task CancellationAfterPersistenceStillRearmsTrigger()
        {
            using CancellationTokenSource cancellation = new();
            RecordingAutomaticSyncBackgroundScheduler scheduler = new()
            {
                MediaStoreSchedule = _ => cancellation.CancelAsync(),
            };
            CottonMediaStoreSyncHandoff handoff = new(scheduler);

            await handoff.QueueAsync(cancellation.Token);

            Assert.True(cancellation.IsCancellationRequested);
            Assert.Equal(1, scheduler.RescheduleMediaStoreCount);
        }
    }
}
