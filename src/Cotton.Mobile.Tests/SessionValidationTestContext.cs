using Cotton.Mobile.Services;
using Cotton.Sdk.Auth;

namespace Cotton.Mobile.Tests
{
    internal class SessionValidationTestContext(
        SessionValidationHttpMessageHandler handler,
        HttpClient httpClient,
        InMemoryCottonTokenStore tokenStore,
        CottonSessionValidator validator) : IDisposable
    {
        public SessionValidationHttpMessageHandler Handler { get; } = handler;

        public InMemoryCottonTokenStore TokenStore { get; } = tokenStore;

        public CottonSessionValidator Validator { get; } = validator;

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
