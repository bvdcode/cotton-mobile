using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonClientFactory : ICottonClientFactory
    {
        private readonly ICottonTokenStore _tokenStore;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ICottonMobileApplicationMetadata _metadata;

        public CottonClientFactory(
            ICottonTokenStore tokenStore,
            ILoggerFactory loggerFactory,
            ICottonMobileApplicationMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(tokenStore);
            ArgumentNullException.ThrowIfNull(loggerFactory);
            ArgumentNullException.ThrowIfNull(metadata);

            _tokenStore = tokenStore;
            _loggerFactory = loggerFactory;
            _metadata = metadata;
        }

        public ICottonCloudClient Create(Uri instanceUri)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));

            var options = new CottonSdkOptions
            {
                BaseAddress = instanceUri,
                DeviceName = _metadata.DeviceName,
                UserAgent = _metadata.UserAgent,
            };

            return new CottonCloudClient(new HttpClient(), _tokenStore, options, _loggerFactory);
        }
    }
}
