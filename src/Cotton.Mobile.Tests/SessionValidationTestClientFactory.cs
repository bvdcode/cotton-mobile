using Cotton.Mobile.Services;
using Cotton.Sdk;
using Cotton.Sdk.Auth;

namespace Cotton.Mobile.Tests
{
    internal class SessionValidationTestClientFactory(
        HttpClient httpClient,
        ICottonTokenStore tokenStore) : ICottonClientFactory
    {
        public ICottonCloudClient Create(Uri instanceUri)
        {
            return new CottonCloudClient(
                httpClient,
                tokenStore,
                new CottonSdkOptions { BaseAddress = instanceUri });
        }
    }
}
