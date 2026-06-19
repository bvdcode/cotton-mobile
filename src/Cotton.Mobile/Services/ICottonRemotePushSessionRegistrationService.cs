namespace Cotton.Mobile.Services
{
    public interface ICottonRemotePushSessionRegistrationService
    {
        Task RegisterCurrentSessionBestEffortAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task RevokeCurrentSessionBestEffortAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
