using Cotton.Auth;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class SecureStorageCottonTokenStore : ICottonTokenStore
    {
        private const string AccessTokenKey = "Cotton.Mobile.Auth.AccessToken";
        private const string RefreshTokenKey = "Cotton.Mobile.Auth.RefreshToken";

        private readonly ISecureStorage _secureStorage;
        private readonly ILogger<SecureStorageCottonTokenStore> _logger;

        public SecureStorageCottonTokenStore(
            ISecureStorage secureStorage,
            ILogger<SecureStorageCottonTokenStore> logger)
        {
            ArgumentNullException.ThrowIfNull(secureStorage);
            ArgumentNullException.ThrowIfNull(logger);

            _secureStorage = secureStorage;
            _logger = logger;
        }

        public async Task<TokenPairDto?> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? accessToken = await _secureStorage.GetAsync(AccessTokenKey).ConfigureAwait(false);
            string? refreshToken = await _secureStorage.GetAsync(RefreshTokenKey).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            {
                await ClearAsync(cancellationToken).ConfigureAwait(false);
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

            try
            {
                await _secureStorage.SetAsync(AccessTokenKey, tokens.AccessToken).ConfigureAwait(false);
                await _secureStorage.SetAsync(RefreshTokenKey, tokens.RefreshToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save Cotton mobile tokens; clearing local token store.");
                await ClearAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }

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
