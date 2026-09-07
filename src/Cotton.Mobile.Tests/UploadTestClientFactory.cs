using Cotton.Mobile.Services;
using Cotton.Sdk;
using Cotton.Sdk.Auth;

namespace Cotton.Mobile.Tests
{
    internal class UploadTestClientFactory(HttpClient httpClient) : ICottonClientFactory
    {
        public ICottonCloudClient Create(Uri instanceUri)
        {
            return new CottonCloudClient(
                httpClient,
                new InMemoryCottonTokenStore(),
                new CottonSdkOptions { BaseAddress = instanceUri });
        }
    }
}
