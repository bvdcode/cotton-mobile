// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Auth;

namespace Cotton.Mobile.Services
{
    public interface ICottonRefreshTokenTransport
    {
        Task<TokenPairDto> RefreshAsync(
            Uri instanceUri,
            string refreshToken,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(
            Uri instanceUri,
            string refreshToken,
            CancellationToken cancellationToken = default);
    }
}
