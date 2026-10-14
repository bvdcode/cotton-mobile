using Cotton.Auth;
using Cotton.Sdk.Auth;

namespace Cotton.Mobile.Tests
{
    internal class CancelingSessionTokenStore(
        InMemoryCottonTokenStore inner,
        CancellationTokenSource cancellation) : ICottonTokenStore
    {
        private bool _cancelBeforeSave = true;

        public Task<TokenPairDto?> GetAsync(CancellationToken cancellationToken = default)
        {
            return inner.GetAsync(cancellationToken);
        }

        public async Task SaveAsync(TokenPairDto tokens, CancellationToken cancellationToken = default)
        {
            if (_cancelBeforeSave)
            {
                _cancelBeforeSave = false;
                await cancellation.CancelAsync();
            }

            await inner.SaveAsync(tokens, cancellationToken);
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            return inner.ClearAsync(cancellationToken);
        }
    }
}
