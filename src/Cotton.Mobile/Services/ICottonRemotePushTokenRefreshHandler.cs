namespace Cotton.Mobile.Services
{
    public interface ICottonRemotePushTokenRefreshHandler
    {
        Task HandleNewTokenAsync(
            string token,
            CancellationToken cancellationToken = default);
    }
}
