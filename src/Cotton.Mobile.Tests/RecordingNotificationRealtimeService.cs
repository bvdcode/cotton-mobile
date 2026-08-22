// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class RecordingNotificationRealtimeService(Exception? startFailure = null) :
        ICottonNotificationRealtimeService
    {
        private readonly TaskCompletionSource _firstStart = CreateCompletionSource();
        private readonly TaskCompletionSource _secondStart = CreateCompletionSource();
        private readonly TaskCompletionSource _stopEntered = CreateCompletionSource();
        private readonly TaskCompletionSource _stopRelease = CreateCompletionSource();

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public int DisposeCount { get; private set; }

        public Uri? StartedInstanceUri { get; private set; }

        public bool BlockStop { get; set; }

        public Task FirstStart => _firstStart.Task;

        public Task SecondStart => _secondStart.Task;

        public Task StopEntered => _stopEntered.Task;

        public Task StartAsync(Uri instanceUri, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();

            StartedInstanceUri = instanceUri;
            StartCount++;
            if (StartCount == 1)
            {
                _firstStart.TrySetResult();
            }
            else if (StartCount == 2)
            {
                _secondStart.TrySetResult();
            }

            return startFailure is null
                ? Task.CompletedTask
                : Task.FromException(startFailure);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopCount++;
            _stopEntered.TrySetResult();
            if (BlockStop)
            {
                await _stopRelease.Task.WaitAsync(cancellationToken);
            }
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        public void ReleaseStop()
        {
            _stopRelease.TrySetResult();
        }

        private static TaskCompletionSource CreateCompletionSource()
        {
            return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
