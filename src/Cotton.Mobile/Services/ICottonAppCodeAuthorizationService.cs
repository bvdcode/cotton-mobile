// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAppCodeAuthorizationService
    {
        Task<CottonSessionResult> SignInAsync(Uri instanceUri, CancellationToken cancellationToken);

        Task<CottonSessionResult> RestorePendingAsync(Uri instanceUri, CancellationToken cancellationToken);

        Task ClearPendingBestEffortAsync(string reason);
    }
}
