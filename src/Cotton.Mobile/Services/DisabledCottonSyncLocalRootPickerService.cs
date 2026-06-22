// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class DisabledCottonSyncLocalRootPickerService : ICottonSyncLocalRootPickerService
    {
        public bool IsAvailable => false;

        public Task<CottonSyncLocalRootSnapshot?> PickUserSelectedDocumentTreeAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<CottonSyncLocalRootSnapshot?>(null);
        }
    }
}
