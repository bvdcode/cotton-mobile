// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
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

        public async Task LoadAsync(
            ISyncSettingsViewState state,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.InstanceUri is null)
            {
                state.Status = AppResources.SyncFoldersInspectFailed;
                return;
            }

            state.IsBusy = true;
            try
            {
                state.ShowRoots(await _rootProvider.LoadAsync(state, cancellationToken));
                state.Status = null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                state.Status = null;
                throw;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to load Cotton mobile sync roots.", exception);
                state.Status = AppResources.SyncFoldersInspectFailed;
            }
            finally
            {
                state.IsBusy = false;
            }
        }
    }
}
