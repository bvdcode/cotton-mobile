// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class RecordingNotificationPollingService(Exception? failure = null) :
        ICottonNotificationPollingService
    {
        private readonly TaskCompletionSource _firstCheck =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CheckCount { get; private set; }

        public Task FirstCheck => _firstCheck.Task;

        public Task CheckAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckCount++;
            _firstCheck.TrySetResult();
            return failure is null
                ? Task.CompletedTask
                : Task.FromException(failure);
        }
    }
}
