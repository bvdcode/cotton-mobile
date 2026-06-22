// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface IApplicationForegroundService
    {
        event EventHandler? Resumed;

        event EventHandler? Stopped;

        long CurrentResumeVersion { get; }

        DateTimeOffset? LastStoppedAtUtc { get; }

        Task WaitForNextResumeAsync(long resumeVersionCheckpoint, CancellationToken cancellationToken);

        void NotifyStopped();

        void NotifyResumed();
    }
}
