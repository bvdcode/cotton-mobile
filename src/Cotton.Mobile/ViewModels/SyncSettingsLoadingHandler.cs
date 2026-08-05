// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsLoadingHandler(
        SyncSettingsRootProvider rootProvider,
        ILogger<SyncSettingsLoadingHandler> logger)
    {
        private readonly SyncSettingsRootProvider _rootProvider = rootProvider
            ?? throw new ArgumentNullException(nameof(rootProvider));
        private readonly ILogger<SyncSettingsLoadingHandler> _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));

        public async Task LoadAsync(ISyncSettingsViewState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.InstanceUri is null)
            {
                state.Status = "Could not inspect sync folders.";
                return;
            }

            state.IsBusy = true;
            try
            {
                state.ShowRoots(await _rootProvider.LoadAsync(state));
                state.Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile sync roots.");
                state.Status = "Could not inspect sync folders.";
            }
            finally
            {
                state.IsBusy = false;
            }
        }
    }
}
