// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
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
                CottonLog.Warning(_logger, "Failed to restore Cotton mobile session.", exception);
                MainPageSessionState? cachedState = await TryCreateCachedStateAsync(rememberedInstanceUri);
                if (cachedState is not null)
                {
                    return cachedState;
                }

                Uri? storedInstanceUri = await GetStoredInstanceBestEffortAsync();
                string status = _networkAccess.HasInternetAccess
                    ? AppResources.SessionRestoreFailed
                    : AppResources.SessionRestoreOffline;
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
                CottonLog.Information(_logger, "Cotton mobile browser authorization was cancelled.", exception);
                return await CompleteAuthorizationCancellationAsync(signInInstanceUrl);
            }
            catch (Exception exception)
            {
                CottonLog.Error(_logger, "Cotton mobile browser authorization failed.", exception);
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
                return MainPageSessionState.SignedOut(string.Empty, AppResources.SignedOutStatus);
            }
            catch (Exception exception)
            {
                CottonLog.Error(_logger, "Cotton mobile logout failed.", exception);
                if (currentProfile is not null)
                {
                    if (currentInstanceUri is not null)
                    {
                        return MainPageSessionState.Authenticated(
                            currentInstanceUri,
                            currentProfile,
                            AppResources.SignOutFailed,
                            reloadSync: false);
                    }
                }

                return MainPageSessionState.SignedOut(
                    string.Empty,
                    AppResources.SignOutCompletionFailed);
            }
        }

        private async Task<MainPageSessionState> CreateStateAsync(
            CottonSessionResult result,
            string unauthenticatedStatus,
            Uri? rememberedInstanceUri)
        {
            if (result.IsAuthenticated && result.InstanceUri is not null && result.User is not null)
            {
                MainPageProfile profile = _presentationService.CreateProfile(result.InstanceUri, result.User);
                await SaveCachedProfileBestEffortAsync(result.InstanceUri, profile);
                return MainPageSessionState.Authenticated(result.InstanceUri, profile, status: null);
            }

            string instanceUrl = (result.InstanceUri ?? rememberedInstanceUri)?.AbsoluteUri ?? string.Empty;
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
                    ? AppResources.SessionVerificationFailed
                    : AppResources.SessionOffline;
                return MainPageSessionState.Authenticated(instanceUri, profile, status);
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to load the cached Cotton mobile profile.", exception);
                return null;
            }
        }

        private async Task<MainPageSessionState> CompleteAuthorizationCancellationAsync(string instanceUrl)
        {
            await ClearLocalSessionBestEffortAsync("authorization cancellation");
            return MainPageSessionState.SignedOut(instanceUrl, AppResources.AuthorizationCancelled);
        }

        private async Task<Uri?> GetRememberedInstanceBestEffortAsync()
        {
            try
            {
                return await _sessionService.GetRememberedSessionInstanceAsync();
            }
            catch (Exception exception)
            {
                CottonLog.Debug(_logger, "Failed to read the remembered Cotton mobile session.", exception);
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
                CottonLog.Warning(_logger, "Failed to restore the Cotton mobile instance URL.", exception);
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
                CottonLog.Warning(_logger, "Failed to save the Cotton mobile profile cache.", exception);
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
                CottonLog.WarningWithContext(
                    _logger,
                    "Failed to clear the Cotton mobile session.",
                    reason,
                    exception);
            }
        }
    }
}
