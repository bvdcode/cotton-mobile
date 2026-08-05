// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public partial class MainPageViewModel
    {
        public async Task RestoreSessionOnceAsync()
        {
            if (_didRestoreSession)
            {
                return;
            }

            _didRestoreSession = true;
            await RestoreSessionAsync();
        }

        private async Task RestoreSessionAsync()
        {
            Display.ShowLoading(string.Empty);
            RefreshCommands();

            Uri? rememberedInstanceUri = await GetRememberedInstanceBestEffortAsync();
            if (rememberedInstanceUri is not null)
            {
                Display.InstanceUrl = rememberedInstanceUri.AbsoluteUri;
            }

            try
            {
                CottonSessionResult result = await _sessionService.RestoreAsync();
                await ApplySessionResultAsync(result, ReadyStatus);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to restore Cotton mobile session.");
                if (rememberedInstanceUri is not null
                    && await TryShowCachedSessionAsync(rememberedInstanceUri))
                {
                    return;
                }

                await RestoreStoredInstanceUrlBestEffortAsync();
                string status = _networkAccess.HasInternetAccess
                    ? "Session restore failed. Sign in again."
                    : "Offline. Reconnect to restore your session.";
                Display.ShowSignIn(status);
                RefreshCommands();
            }
        }

        private async Task ApplySessionResultAsync(CottonSessionResult result, string unauthenticatedStatus)
        {
            ArgumentNullException.ThrowIfNull(result);

            if (result.IsAuthenticated && result.InstanceUri is not null && result.User is not null)
            {
                MainPageProfile profile = _presentationService.CreateProfile(result.InstanceUri, result.User);
                await SaveCachedProfileBestEffortAsync(result.InstanceUri, profile);
                await ShowAuthenticatedSessionAsync(result.InstanceUri, profile, status: null);
                return;
            }

            ClearAuthenticatedSession();
            if (result.InstanceUri is not null)
            {
                Display.InstanceUrl = result.InstanceUri.AbsoluteUri;
            }

            Display.ShowSignIn(_presentationService.ResolveStatusMessage(result, unauthenticatedStatus));
            RefreshCommands();
        }

        private async Task ShowAuthenticatedSessionAsync(
            Uri instanceUri,
            MainPageProfile profile,
            string? status)
        {
            _currentProfile = profile;
            Display.InstanceUrl = instanceUri.AbsoluteUri;
            Display.ShowAuthenticated(profile, status);
            RefreshCommands();
            await Sync.LoadForInstanceAsync(instanceUri, profile.AccountScopeKey);
        }

        private async Task<bool> TryShowCachedSessionAsync(Uri instanceUri)
        {
            MainPageProfile? profile;
            try
            {
                profile = await _profileCacheStore.GetAsync(instanceUri);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load the cached Cotton mobile profile.");
                return false;
            }

            if (profile is null)
            {
                return false;
            }

            string status = _networkAccess.HasInternetAccess
                ? "Could not verify the session. Some actions may be unavailable."
                : "Offline. Sync is available again after reconnecting.";
            await ShowAuthenticatedSessionAsync(instanceUri, profile, status);
            return true;
        }

        private async Task<Uri?> GetRememberedInstanceBestEffortAsync()
        {
            try
            {
                return await _sessionService.GetRememberedSessionInstanceAsync();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Failed to read the remembered Cotton mobile session.");
                return null;
            }
        }

        private async Task RestoreStoredInstanceUrlBestEffortAsync()
        {
            try
            {
                Uri? instanceUri = await _instanceStore.GetAsync();
                if (instanceUri is not null)
                {
                    Display.InstanceUrl = instanceUri.AbsoluteUri;
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to restore the Cotton mobile instance URL.");
            }
        }

        private async Task SaveCachedProfileBestEffortAsync(Uri instanceUri, MainPageProfile profile)
        {
            try
            {
                await _profileCacheStore.SaveAsync(instanceUri, profile);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save the Cotton mobile profile cache.");
            }
        }

        private async Task ClearLocalSessionBestEffortAsync(string reason)
        {
            try
            {
                await _sessionService.ClearLocalSessionAsync();
                await _profileCacheStore.ClearAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to clear the Cotton mobile session after {Reason}.",
                    reason);
            }

            ClearAuthenticatedSession();
        }

        private void ClearAuthenticatedSession()
        {
            _currentProfile = null;
            Sync.Clear();
        }
    }
}
