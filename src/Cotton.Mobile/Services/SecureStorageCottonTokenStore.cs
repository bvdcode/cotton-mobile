// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

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

            string? accessToken;
            string? refreshToken;
            try
            {
                accessToken = await _secureStorage.GetAsync(AccessTokenKey).ConfigureAwait(false);
                refreshToken = await _secureStorage.GetAsync(RefreshTokenKey).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Failed to read Cotton mobile tokens; clearing local token store.");
                ClearBestEffort("token read failure");
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            {
                ClearBestEffort("partial token pair");
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
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Failed to save Cotton mobile tokens; clearing local token store.");
                ClearBestEffort("token save failure");
                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Exception> failures = [];
            RemoveTokenKey(AccessTokenKey, failures);
            RemoveTokenKey(RefreshTokenKey, failures);
            if (failures.Count == 1)
            {
                throw new InvalidOperationException("Failed to clear one Cotton mobile token.", failures[0]);
            }

            if (failures.Count > 1)
            {
                throw new AggregateException("Failed to clear Cotton mobile tokens.", failures);
            }

            return Task.CompletedTask;
        }

        private void ClearBestEffort(string reason)
        {
            List<Exception> failures = [];
            RemoveTokenKey(AccessTokenKey, failures);
            RemoveTokenKey(RefreshTokenKey, failures);
            foreach (Exception exception in failures)
            {
                _logger.LogWarning(exception, "Failed to clear Cotton mobile tokens after {Reason}.", reason);
            }
        }

        private void RemoveTokenKey(string key, List<Exception> failures)
        {
            try
            {
                _secureStorage.Remove(key);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
    }
}
