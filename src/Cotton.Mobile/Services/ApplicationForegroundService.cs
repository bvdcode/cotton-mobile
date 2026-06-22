// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class ApplicationForegroundService : IApplicationForegroundService
    {
        private readonly Lock _gate = new();
        private readonly ILogger<ApplicationForegroundService> _logger;
        private TaskCompletionSource _nextResume = CreateResumeSource();
        private DateTimeOffset? _lastStoppedAtUtc;
        private long _resumeVersion;

        public ApplicationForegroundService(ILogger<ApplicationForegroundService> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

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
                _lastStoppedAtUtc = DateTimeOffset.UtcNow;
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
                    _logger.LogWarning(exception, "Cotton mobile stopped subscriber failed.");
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
                    _logger.LogWarning(exception, "Cotton mobile foreground subscriber failed.");
                }
            }
        }

        private static TaskCompletionSource CreateResumeSource()
        {
            return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
