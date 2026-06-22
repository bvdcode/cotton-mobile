// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonSessionService
    {
        Task<CottonSessionResult> RestoreAsync(CancellationToken cancellationToken = default);

        Task<CottonSessionResult> SignInWithBrowserAsync(Uri instanceUri, CancellationToken cancellationToken = default);

        Task LogoutAsync(CancellationToken cancellationToken = default);

        Task ClearLocalSessionAsync(CancellationToken cancellationToken = default);
    }
}
