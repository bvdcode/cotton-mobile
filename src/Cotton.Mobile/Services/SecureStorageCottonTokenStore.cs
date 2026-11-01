// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;
using Cotton.Auth;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class SecureStorageCottonTokenStore : ICottonTokenStore, IDisposable
    {
        private const string TokenPairKey = "Cotton.Mobile.Auth.TokenPair";
        private const string LegacyAccessTokenKey = "Cotton.Mobile.Auth.AccessToken";
        private const string LegacyRefreshTokenKey = "Cotton.Mobile.Auth.RefreshToken";

        private readonly SemaphoreSlim _gate = new(1, 1);
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
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                string? value = await ReadValueAsync(TokenPairKey).ConfigureAwait(false);
                TokenPairDto? tokens = value is null
                    ? await ReadAndMigrateLegacyPairAsync(cancellationToken).ConfigureAwait(false)
                    : DeserializePair(value);
                cancellationToken.ThrowIfCancellationRequested();
                if (tokens is not null)
                {
                    CottonSessionDiagnosticLog.TokenStoreLoaded(_logger);
                }

                return tokens;
            }
            finally
            {
                _gate.Release();
            }
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

            string value = JsonSerializer.Serialize(tokens, CottonTokenPairJsonContext.Default.TokenPairDto);
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await WriteValueAsync(value).ConfigureAwait(false);
                CottonSessionDiagnosticLog.TokenStoreSaved(_logger);
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                RemoveKeys(LegacyAccessTokenKey, LegacyRefreshTokenKey);
                RemoveKeys(TokenPairKey);
                CottonSessionDiagnosticLog.TokenStoreCleared(_logger);
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            _gate.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task<TokenPairDto?> ReadAndMigrateLegacyPairAsync(CancellationToken cancellationToken)
        {
            string? accessToken = await ReadValueAsync(LegacyAccessTokenKey).ConfigureAwait(false);
            string? refreshToken = await ReadValueAsync(LegacyRefreshTokenKey).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(refreshToken))
            {
                CottonSessionDiagnosticLog.TokenStoreEmpty(_logger);
                return null;
            }

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            {
                RemoveKeys(LegacyAccessTokenKey, LegacyRefreshTokenKey);
                CottonSessionDiagnosticLog.TokenStoreIncomplete(_logger);
                return null;
            }

            TokenPairDto tokens = new() { AccessToken = accessToken, RefreshToken = refreshToken };
            string value = JsonSerializer.Serialize(tokens, CottonTokenPairJsonContext.Default.TokenPairDto);
            await WriteValueAsync(value).ConfigureAwait(false);
            RemoveKeys(LegacyAccessTokenKey, LegacyRefreshTokenKey);
            CottonSessionDiagnosticLog.TokenStoreMigrated(_logger);
            return tokens;
        }

        private async Task<string?> ReadValueAsync(string key)
        {
            try
            {
                return await _secureStorage.GetAsync(key).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonSessionDiagnosticLog.TokenStoreReadFailed(_logger, exception);
                throw;
            }
        }

        private TokenPairDto DeserializePair(string value)
        {
            try
            {
                TokenPairDto? tokens = JsonSerializer.Deserialize(value, CottonTokenPairJsonContext.Default.TokenPairDto);
                if (tokens is null || string.IsNullOrWhiteSpace(tokens.AccessToken)
                    || string.IsNullOrWhiteSpace(tokens.RefreshToken))
                {
                    throw new InvalidDataException("Stored token pair is incomplete.");
                }

                return tokens;
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException)
            {
                CottonSessionDiagnosticLog.TokenStoreReadFailed(_logger, exception);
                throw;
            }
        }

        private async Task WriteValueAsync(string value)
        {
            try
            {
                await _secureStorage.SetAsync(TokenPairKey, value).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonLog.Warning(_logger, "Failed to save Cotton mobile token pair.", exception);
                throw;
            }
        }

        private void RemoveKeys(params string[] keys)
        {
            List<Exception> failures = [];
            foreach (string key in keys)
            {
                try
                {
                    _secureStorage.Remove(key);
                }
                catch (Exception exception)
                {
                    CottonLog.WarningWithContext(_logger, "Failed to clear Cotton mobile token value.", key, exception);
                    failures.Add(exception);
                }
            }

            if (failures.Count == 1)
            {
                throw new InvalidOperationException("Failed to clear a Cotton mobile token value.", failures[0]);
            }

            if (failures.Count > 1)
            {
                throw new AggregateException("Failed to clear Cotton mobile token values.", failures);
            }
        }
    }
}
