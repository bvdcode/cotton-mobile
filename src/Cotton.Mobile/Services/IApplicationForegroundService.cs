namespace Cotton.Mobile.Services
{
    public interface IApplicationForegroundService
    {
        event EventHandler? Resumed;

        long CurrentResumeVersion { get; }

        Task WaitForNextResumeAsync(long resumeVersionCheckpoint, CancellationToken cancellationToken);

        void NotifyResumed();
    }
}
