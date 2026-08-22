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
                CottonSessionDiagnosticLog.TokenStoreReadFailed(_logger, exception);
                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(refreshToken))
            {
                CottonSessionDiagnosticLog.TokenStoreEmpty(_logger);
                return null;
            }

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            {
                ClearBestEffort("partial token pair");
                CottonSessionDiagnosticLog.TokenStoreIncomplete(_logger);
                return null;
            }

            CottonSessionDiagnosticLog.TokenStoreLoaded(_logger);

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
                CottonSessionDiagnosticLog.TokenStoreSaved(_logger);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonLog.Warning(_logger, "Failed to save Cotton mobile tokens; clearing local token store.", exception);
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

            CottonSessionDiagnosticLog.TokenStoreCleared(_logger);

            return Task.CompletedTask;
        }

        private void ClearBestEffort(string reason)
        {
            List<Exception> failures = [];
            RemoveTokenKey(AccessTokenKey, failures);
            RemoveTokenKey(RefreshTokenKey, failures);
            foreach (Exception exception in failures)
            {
                CottonLog.WarningWithContext(
                    _logger,
                    "Failed to clear Cotton mobile tokens.",
                    reason,
                    exception);
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
