// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class RecordingNotificationBackgroundScheduler(
        Exception? scheduleFailure = null,
        Exception? cancelFailure = null) : ICottonNotificationBackgroundScheduler
    {
        public int ScheduleCount { get; private set; }

        public int CancelCount { get; private set; }

        public Task ScheduleAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScheduleCount++;
            if (scheduleFailure is not null)
            {
                return Task.FromException(scheduleFailure);
            }

            return Task.CompletedTask;
        }

        public Task CancelAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CancelCount++;
            if (cancelFailure is not null)
            {
                return Task.FromException(cancelFailure);
            }

            return Task.CompletedTask;
        }
    }
}
