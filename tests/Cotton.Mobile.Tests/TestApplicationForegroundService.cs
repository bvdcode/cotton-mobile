// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class TestApplicationForegroundService : IApplicationForegroundService
    {
        private long _resumeVersion;

        public event EventHandler? Resumed;

        public event EventHandler? Stopped;

        public long CurrentResumeVersion => _resumeVersion;

        public DateTimeOffset? LastStoppedAtUtc { get; private set; }

        public bool IsForeground { get; private set; }

        public Task WaitForNextResumeAsync(long resumeVersionCheckpoint, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public void NotifyStopped()
        {
            IsForeground = false;
            LastStoppedAtUtc = DateTimeOffset.UtcNow;
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyResumed()
        {
            IsForeground = true;
            _resumeVersion++;
            Resumed?.Invoke(this, EventArgs.Empty);
        }
    }
}
