using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class ApplicationForegroundService : IApplicationForegroundService
    {
        private readonly Lock _gate = new();
        private readonly ILogger<ApplicationForegroundService> _logger;
        private TaskCompletionSource _nextResume = CreateResumeSource();
        private long _resumeVersion;

        public ApplicationForegroundService(ILogger<ApplicationForegroundService> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public event EventHandler? Resumed;

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
