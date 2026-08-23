// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Auth;
using Cotton.Sdk.Auth;

namespace Cotton.Mobile.Services
{
    public class CottonAccessTokenStore(ICottonTokenStore tokenStore) : ICottonTokenStore
    {
        private readonly ICottonTokenStore _tokenStore =
            tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));

        public async Task<TokenPairDto?> GetAsync(CancellationToken cancellationToken = default)
        {
            TokenPairDto? tokens = await _tokenStore.GetAsync(cancellationToken).ConfigureAwait(false);
            return tokens is null
                ? null
                : new TokenPairDto
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = string.Empty,
                };
        }

        public Task SaveAsync(
            TokenPairDto tokens,
            CancellationToken cancellationToken = default)
        {
            return _tokenStore.SaveAsync(tokens, cancellationToken);
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            return _tokenStore.ClearAsync(cancellationToken);
        }
    }
}
