namespace Cotton.Mobile.Services
{
    public class ApplicationForegroundService : IApplicationForegroundService
    {
        private readonly Lock _gate = new();
        private TaskCompletionSource _nextResume = CreateResumeSource();
        private long _resumeVersion;

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
            Resumed?.Invoke(this, EventArgs.Empty);
        }

        private static TaskCompletionSource CreateResumeSource()
        {
            return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
