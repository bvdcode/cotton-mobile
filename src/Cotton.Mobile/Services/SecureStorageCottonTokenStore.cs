using Cotton.Auth;
using Cotton.Sdk.Auth;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class SecureStorageCottonTokenStore : ICottonTokenStore
    {
        private const string AccessTokenKey = "Cotton.Mobile.Auth.AccessToken";
        private const string RefreshTokenKey = "Cotton.Mobile.Auth.RefreshToken";

        private readonly ISecureStorage _secureStorage;

        public SecureStorageCottonTokenStore(ISecureStorage secureStorage)
        {
            ArgumentNullException.ThrowIfNull(secureStorage);
            _secureStorage = secureStorage;
        }

        public async Task<TokenPairDto?> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? accessToken = await _secureStorage.GetAsync(AccessTokenKey).ConfigureAwait(false);
            string? refreshToken = await _secureStorage.GetAsync(RefreshTokenKey).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            return new TokenPairDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            };
        }

        public async Task SaveAsync(TokenPairDto tokens, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(tokens);
            if (string.IsNullOrWhiteSpace(tokens.AccessToken))
            {
                throw new ArgumentException("Access token is required.", nameof(tokens));
            }

            if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                throw new ArgumentException("Refresh token is required.", nameof(tokens));
            }

            await _secureStorage.SetAsync(AccessTokenKey, tokens.AccessToken).ConfigureAwait(false);
            await _secureStorage.SetAsync(RefreshTokenKey, tokens.RefreshToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _secureStorage.Remove(AccessTokenKey);
            _secureStorage.Remove(RefreshTokenKey);

            return Task.CompletedTask;
        }
    }
}
