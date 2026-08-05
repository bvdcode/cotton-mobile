// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class MainPageSessionCoordinator
    {
        private readonly ICottonSessionService _sessionService;
        private readonly ICottonInstanceStore _instanceStore;
        private readonly ICottonProfileCacheStore _profileCacheStore;
        private readonly INetworkAccessService _networkAccess;
        private readonly IMainPagePresentationService _presentationService;
        private readonly ILogger<MainPageSessionCoordinator> _logger;

        public MainPageSessionCoordinator(
            ICottonSessionService sessionService,
            ICottonInstanceStore instanceStore,
            ICottonProfileCacheStore profileCacheStore,
            INetworkAccessService networkAccess,
            IMainPagePresentationService presentationService,
            CottonMobileOptions options,
            ILogger<MainPageSessionCoordinator> logger)
        {
            ArgumentNullException.ThrowIfNull(sessionService);
            ArgumentNullException.ThrowIfNull(instanceStore);
            ArgumentNullException.ThrowIfNull(profileCacheStore);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(presentationService);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _sessionService = sessionService;
            _instanceStore = instanceStore;
            _profileCacheStore = profileCacheStore;
            _networkAccess = networkAccess;
            _presentationService = presentationService;
            DefaultInstanceUrl = options.DefaultInstanceUrl;
            _logger = logger;
        }

        public string DefaultInstanceUrl { get; }

        public async Task<MainPageSessionState> RestoreAsync()
        {
            Uri? rememberedInstanceUri = await GetRememberedInstanceBestEffortAsync();
            try
            {
                CottonSessionResult result = await _sessionService.RestoreAsync();
                return await CreateStateAsync(result, string.Empty, rememberedInstanceUri);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to restore Cotton mobile session.");
                MainPageSessionState? cachedState = await TryCreateCachedStateAsync(rememberedInstanceUri);
                if (cachedState is not null)
                {
                    return cachedState;
                }

                Uri? storedInstanceUri = await GetStoredInstanceBestEffortAsync();
                string status = _networkAccess.HasInternetAccess
                    ? "Session restore failed. Sign in again."
                    : "Offline. Reconnect to restore your session.";
                return MainPageSessionState.SignedOut(
                    (storedInstanceUri ?? rememberedInstanceUri)?.AbsoluteUri ?? string.Empty,
                    status);
            }
        }

        public async Task<MainPageSessionState> SignInAsync(
            Uri instanceUri,
            string signInInstanceUrl,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(signInInstanceUrl);

            try
            {
                CottonSessionResult result = await _sessionService.SignInWithBrowserAsync(
                    instanceUri,
                    cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return await CompleteAuthorizationCancellationAsync(signInInstanceUrl);
                }

                return await CreateStateAsync(result, string.Empty, instanceUri);
            }
            catch (Exception exception) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(exception, "Cotton mobile browser authorization was cancelled.");
                return await CompleteAuthorizationCancellationAsync(signInInstanceUrl);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Cotton mobile browser authorization failed.");
                await ClearLocalSessionBestEffortAsync("authorization failure");
                return MainPageSessionState.SignedOut(
                    signInInstanceUrl,
                    _presentationService.CreateAuthorizationFailureStatus(exception));
            }
        }

        public async Task<MainPageSessionState> LogoutAsync(
            MainPageProfile? currentProfile,
            Uri? currentInstanceUri)
        {
            try
            {
                await _sessionService.LogoutAsync();
                await _profileCacheStore.ClearAsync();
                return MainPageSessionState.SignedOut(string.Empty, "Signed out.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Cotton mobile logout failed.");
                if (currentProfile is not null)
                {
                    if (currentInstanceUri is not null)
                    {
                        return MainPageSessionState.Authenticated(
                            currentInstanceUri,
                            currentProfile,
                            "Could not sign out. Try again.",
                            reloadSync: false);
                    }
                }

                return MainPageSessionState.SignedOut(
                    string.Empty,
                    "Could not finish signing out. Try again.");
            }
        }

        private async Task<MainPageSessionState> CreateStateAsync(
            CottonSessionResult result,
            string unauthenticatedStatus,
            Uri? fallbackInstanceUri)
        {
            if (result.IsAuthenticated && result.InstanceUri is not null && result.User is not null)
            {
                MainPageProfile profile = _presentationService.CreateProfile(result.InstanceUri, result.User);
                await SaveCachedProfileBestEffortAsync(result.InstanceUri, profile);
                return MainPageSessionState.Authenticated(result.InstanceUri, profile, status: null);
            }

            string instanceUrl = (result.InstanceUri ?? fallbackInstanceUri)?.AbsoluteUri ?? string.Empty;
            return MainPageSessionState.SignedOut(
                instanceUrl,
                _presentationService.ResolveStatusMessage(result, unauthenticatedStatus));
        }

        private async Task<MainPageSessionState?> TryCreateCachedStateAsync(Uri? instanceUri)
        {
            if (instanceUri is null)
            {
                return null;
            }

            try
            {
                MainPageProfile? profile = await _profileCacheStore.GetAsync(instanceUri);
                if (profile is null)
                {
                    return null;
                }

                string status = _networkAccess.HasInternetAccess
                    ? "Could not verify the session. Some actions may be unavailable."
                    : "Offline. Sync is available again after reconnecting.";
                return MainPageSessionState.Authenticated(instanceUri, profile, status);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load the cached Cotton mobile profile.");
                return null;
            }
        }

        private async Task<MainPageSessionState> CompleteAuthorizationCancellationAsync(string instanceUrl)
        {
            await ClearLocalSessionBestEffortAsync("authorization cancellation");
            return MainPageSessionState.SignedOut(instanceUrl, "Authorization cancelled.");
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

        private async Task<Uri?> GetStoredInstanceBestEffortAsync()
        {
            try
            {
                return await _instanceStore.GetAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to restore the Cotton mobile instance URL.");
                return null;
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
        }
    }
}
