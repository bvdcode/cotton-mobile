// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonSyncRootSetupDraftStore
    {
        Task<CottonSyncRootSetupDraft?> LoadAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(
            CottonSyncRootSetupDraft draft,
            CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
