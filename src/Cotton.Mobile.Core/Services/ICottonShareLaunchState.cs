namespace Cotton.Mobile.Services
{
    public interface ICottonShareLaunchState
    {
        event EventHandler? ShareStaged;

        int PendingShareLaunchCount { get; }

        void NotifyShareStaged();

        bool TryConsumePendingShareLaunch();
    }
}
