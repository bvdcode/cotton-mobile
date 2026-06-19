namespace Cotton.Mobile.Services
{
    public class CottonRemotePushRegistrationCoordinator
    {
        private readonly ICottonRemotePushPlatformTokenProvider _platformTokenProvider;
        private readonly ICottonRemotePushDeviceTokenService _deviceTokenService;

        public CottonRemotePushRegistrationCoordinator(
            ICottonRemotePushPlatformTokenProvider platformTokenProvider,
            ICottonRemotePushDeviceTokenService deviceTokenService)
        {
            ArgumentNullException.ThrowIfNull(platformTokenProvider);
            ArgumentNullException.ThrowIfNull(deviceTokenService);

            _platformTokenProvider = platformTokenProvider;
            _deviceTokenService = deviceTokenService;
        }

        public async Task<CottonRemotePushRegistrationResult> RegisterCurrentAsync(
            Uri instanceUri,
            string? deviceName,
            string? appVersion,
            CancellationToken cancellationToken = default)
        {
            CottonRemotePushPlatformTokenSnapshot platformToken =
                await _platformTokenProvider.GetCurrentTokenAsync(cancellationToken).ConfigureAwait(false);
            if (!platformToken.HasToken)
            {
                return CottonRemotePushRegistrationResult.Skipped(platformToken);
            }

            var request = new CottonRemotePushDeviceTokenRegistrationRequest(
                platformToken.Provider,
                platformToken.Platform,
                platformToken.Token!,
                deviceName,
                appVersion);
            CottonRemotePushDeviceTokenSnapshot deviceToken =
                await _deviceTokenService
                    .RegisterCurrentAsync(instanceUri, request, cancellationToken)
                    .ConfigureAwait(false);
            return CottonRemotePushRegistrationResult.Registered(platformToken, deviceToken);
        }
    }
}
