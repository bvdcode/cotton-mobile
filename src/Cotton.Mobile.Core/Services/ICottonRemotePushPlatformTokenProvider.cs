namespace Cotton.Mobile.Services
{
    public interface ICottonRemotePushPlatformTokenProvider
    {
        Task<CottonRemotePushPlatformTokenSnapshot> GetCurrentTokenAsync(
            CancellationToken cancellationToken = default);
    }
}
