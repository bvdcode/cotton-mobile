namespace Cotton.Mobile.Services
{
    public interface IApplicationForegroundService
    {
        Task WaitForNextResumeAsync(CancellationToken cancellationToken);

        void NotifyResumed();
    }
}
