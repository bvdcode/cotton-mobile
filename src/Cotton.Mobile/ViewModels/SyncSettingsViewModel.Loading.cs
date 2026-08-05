// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public partial class SyncSettingsViewModel
    {
        private async Task LoadAsync()
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = "Could not inspect sync folders.";
                return;
            }

            IsBusy = true;
            try
            {
                await LoadRootsAsync(instanceUri);
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile sync roots.");
                Status = "Could not inspect sync folders.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadRootsAsync(Uri instanceUri)
        {
            ShowRoots(await LoadRootCollectionAsync(instanceUri));
        }
    }
}
