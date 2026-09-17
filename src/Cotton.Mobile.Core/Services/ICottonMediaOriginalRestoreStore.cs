// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonMediaOriginalRestoreStore
    {
        Task<CottonMediaOriginalRestoreState> LoadAsync(CottonSyncRootSnapshot root, CancellationToken cancellationToken = default);

        Task<CottonMediaOriginalRestoreState> UpdateAsync(
            CottonSyncRootSnapshot root,
            Func<CottonMediaOriginalRestoreState, CottonMediaOriginalRestoreState> update,
            CancellationToken cancellationToken = default);
    }
}
