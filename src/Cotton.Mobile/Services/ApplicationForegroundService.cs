namespace Cotton.Mobile.Services
{
    public class ApplicationForegroundService : IApplicationForegroundService
    {
        private readonly Lock _gate = new();
        private TaskCompletionSource _nextResume = CreateResumeSource();

        public event EventHandler? Resumed;

        public Task WaitForNextResumeAsync(CancellationToken cancellationToken)
        {
            Task resumeTask;
            lock (_gate)
            {
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
