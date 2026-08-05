// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsRootProvider(SyncRootManager rootManager)
    {
        private readonly SyncRootManager _rootManager = rootManager
            ?? throw new ArgumentNullException(nameof(rootManager));

        public Task<SyncRootCollectionSnapshot> LoadAsync(ISyncSettingsViewState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            Uri instanceUri = state.InstanceUri
                ?? throw new InvalidOperationException("Sync instance is not configured.");
            string accountScopeKey = state.AccountScopeKey
                ?? throw new InvalidOperationException("Sync account is not configured.");
            return _rootManager.LoadAsync(instanceUri, accountScopeKey);
        }

        public Task<SyncRootCollectionSnapshot> LoadAsync(Uri instanceUri, string accountScopeKey)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);
            return _rootManager.LoadAsync(instanceUri, accountScopeKey);
        }
    }
}
