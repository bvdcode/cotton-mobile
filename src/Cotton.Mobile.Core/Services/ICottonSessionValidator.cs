// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Auth;

namespace Cotton.Mobile.Services
{
    public interface ICottonSessionValidator
    {
        Task<UserDto> ValidateAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
