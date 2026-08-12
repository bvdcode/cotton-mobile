// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class ApplicationForegroundService(
        ILogger<ApplicationForegroundService> logger,
        TimeProvider timeProvider) : IApplicationForegroundService
    {
        private readonly Lock _gate = new();
        private readonly ILogger<ApplicationForegroundService> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        private TaskCompletionSource _nextResume = CreateResumeSource();
        private DateTimeOffset? _lastStoppedAtUtc;
        private bool _isForeground;
        private long _resumeVersion;

        public event EventHandler? Resumed;

        public event EventHandler? Stopped;

        public long CurrentResumeVersion
        {
            get
            {
                lock (_gate)
                {
                    return _resumeVersion;
                }
            }
        }

        public DateTimeOffset? LastStoppedAtUtc
        {
            get
            {
                lock (_gate)
                {
                    return _lastStoppedAtUtc;
                }
            }
        }

        public bool IsForeground
        {
            get
            {
                lock (_gate)
                {
                    return _isForeground;
                }
            }
        }

        public Task WaitForNextResumeAsync(long resumeVersionCheckpoint, CancellationToken cancellationToken)
        {
            Task resumeTask;
            lock (_gate)
            {
                if (_resumeVersion > resumeVersionCheckpoint)
                {
                    return Task.CompletedTask;
                }

                resumeTask = _nextResume.Task;
            }

            return resumeTask.WaitAsync(cancellationToken);
        }

        public void NotifyStopped()
        {
            lock (_gate)
            {
                _lastStoppedAtUtc = _timeProvider.GetUtcNow();
                _isForeground = false;
            }

            NotifyStoppedSubscribers();
        }

        public void NotifyResumed()
        {
            TaskCompletionSource resume;
            lock (_gate)
            {
                resume = _nextResume;
                _nextResume = CreateResumeSource();
                _isForeground = true;
                _resumeVersion++;
            }

            resume.TrySetResult();
            NotifyResumedSubscribers();
        }

        private void NotifyStoppedSubscribers()
        {
            EventHandler? handlers = Stopped;
            if (handlers is null)
            {
                return;
            }

            foreach (EventHandler handler in handlers.GetInvocationList().Cast<EventHandler>())
            {
                try
                {
                    handler.Invoke(this, EventArgs.Empty);
                }
                catch (Exception exception)
                {
                    CottonLog.Warning(_logger, "Cotton mobile stopped subscriber failed.", exception);
                }
            }
        }

        private void NotifyResumedSubscribers()
        {
            EventHandler? handlers = Resumed;
            if (handlers is null)
            {
                return;
            }

            foreach (EventHandler handler in handlers.GetInvocationList().Cast<EventHandler>())
            {
                try
                {
                    handler.Invoke(this, EventArgs.Empty);
                }
                catch (Exception exception)
                {
                    CottonLog.Warning(_logger, "Cotton mobile foreground subscriber failed.", exception);
                }
            }
        }

        private static TaskCompletionSource CreateResumeSource()
        {
            return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
