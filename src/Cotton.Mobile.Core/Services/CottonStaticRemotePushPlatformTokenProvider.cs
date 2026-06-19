namespace Cotton.Mobile.Services
{
    public class CottonStaticRemotePushPlatformTokenProvider : ICottonRemotePushPlatformTokenProvider
    {
        private readonly CottonRemotePushPlatformTokenSnapshot _snapshot;

        public CottonStaticRemotePushPlatformTokenProvider(CottonRemotePushPlatformTokenSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            _snapshot = snapshot;
        }

        public Task<CottonRemotePushPlatformTokenSnapshot> GetCurrentTokenAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_snapshot);
        }
    }
}
