namespace Cotton.Mobile.Services
{
    public interface IApplicationForegroundService
    {
        event EventHandler? Resumed;

        Task WaitForNextResumeAsync(CancellationToken cancellationToken);

        void NotifyResumed();
    }
}
