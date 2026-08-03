// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private const string InvalidUrlStatus = "Enter a valid HTTPS URL.";
        private const string ReadyStatus = "";

        private readonly ICottonSessionService _sessionService;
        private readonly ICottonInstanceStore _instanceStore;
        private readonly ICottonProfileCacheStore _profileCacheStore;
        private readonly IBrowser _browser;
        private readonly CottonMobileOptions _options;
        private readonly IUserDialogService _dialogService;
        private readonly INetworkAccessService _networkAccess;
        private readonly IMainPagePresentationService _presentationService;
        private readonly ILogger<MainPageViewModel> _logger;

        private CancellationTokenSource? _authorizationCancellation;
        private MainPageProfile? _currentProfile;
        private bool _didRestoreSession;

        public MainPageViewModel(
            ICottonSessionService sessionService,
            ICottonInstanceStore instanceStore,
            ICottonProfileCacheStore profileCacheStore,
            IBrowser browser,
            CottonMobileOptions options,
            IUserDialogService dialogService,
            INetworkAccessService networkAccess,
            IMainPagePresentationService presentationService,
            ICottonMobileApplicationMetadata applicationMetadata,
            SyncSettingsViewModel sync,
            ILogger<MainPageViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(sessionService);
            ArgumentNullException.ThrowIfNull(instanceStore);
            ArgumentNullException.ThrowIfNull(profileCacheStore);
            ArgumentNullException.ThrowIfNull(browser);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(presentationService);
            ArgumentNullException.ThrowIfNull(applicationMetadata);
            ArgumentNullException.ThrowIfNull(sync);
            ArgumentNullException.ThrowIfNull(logger);

            _sessionService = sessionService;
            _instanceStore = instanceStore;
            _profileCacheStore = profileCacheStore;
            _browser = browser;
            _options = options;
            _dialogService = dialogService;
            _networkAccess = networkAccess;
            _presentationService = presentationService;
            _logger = logger;

            Display = new MainPageDisplayState(options.DefaultInstanceUrl);
            Sync = sync;
            Sync.PropertyChanged += OnSyncPropertyChanged;
            ApplicationVersionText = CreateApplicationVersionText(applicationMetadata);

            ConnectCommand = new AsyncCommand(
                SignInAsync,
                LogUnhandledCommandException,
                () => Display.IsInputEnabled);
            CancelAuthorizationCommand = new AsyncCommand(
                CancelAuthorizationAsync,
                LogUnhandledCommandException,
                () => Display.IsCancelAuthorizationEnabled);
            LogoutCommand = new AsyncCommand(
                ConfirmLogoutAsync,
                LogUnhandledCommandException,
                () => IsLogoutEnabled);
            PrivacyPolicyCommand = new AsyncCommand(OpenPrivacyPolicyAsync, LogUnhandledCommandException);
            ShowSyncCommand = new AsyncCommand(ShowSyncAsync, LogUnhandledCommandException);
            ShowProfileCommand = new AsyncCommand(ShowProfileAsync, LogUnhandledCommandException);
        }

        public MainPageDisplayState Display { get; }

        public SyncSettingsViewModel Sync { get; }

        public string ApplicationVersionText { get; }

        public bool IsLogoutEnabled => Display.IsLogoutEnabled && !Sync.IsBusy;

        public AsyncCommand ConnectCommand { get; }

        public AsyncCommand CancelAuthorizationCommand { get; }

        public AsyncCommand LogoutCommand { get; }

        public AsyncCommand PrivacyPolicyCommand { get; }

        public AsyncCommand ShowSyncCommand { get; }

        public AsyncCommand ShowProfileCommand { get; }

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

        private async Task SignInAsync()
        {
            Uri? instanceUri = CottonServerUrl.NormalizeOptional(Display.EffectiveInstanceUrl);
            if (instanceUri is null || !CottonInstanceUri.IsSupported(instanceUri))
            {
                Display.ShowSignIn(InvalidUrlStatus);
                RefreshCommands();
                return;
            }

            Display.InstanceUrl = instanceUri.AbsoluteUri;
            using var authorizationCancellation = new CancellationTokenSource();
            _authorizationCancellation = authorizationCancellation;
            Display.ShowAuthorizationProgress();
            RefreshCommands();

            try
            {
                CottonSessionResult result = await _sessionService.SignInWithBrowserAsync(
                    instanceUri,
                    authorizationCancellation.Token);
                await ApplySessionResultAsync(result, ReadyStatus);
            }
            catch (OperationCanceledException exception) when (authorizationCancellation.IsCancellationRequested)
            {
                _logger.LogInformation(exception, "Cotton mobile browser authorization was cancelled.");
                await ClearLocalSessionBestEffortAsync("authorization cancellation");
                Display.ShowSignIn("Authorization cancelled.");
                RefreshCommands();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Cotton mobile browser authorization failed.");
                await ClearLocalSessionBestEffortAsync("authorization failure");
                Display.ShowSignIn(_presentationService.CreateAuthorizationFailureStatus(exception));
                RefreshCommands();
            }
            finally
            {
                if (ReferenceEquals(_authorizationCancellation, authorizationCancellation))
                {
                    _authorizationCancellation = null;
                }
            }
        }

        private Task CancelAuthorizationAsync()
        {
            Display.ShowAuthorizationCancelling();
            RefreshCommands();
            _authorizationCancellation?.Cancel();
            return Task.CompletedTask;
        }

        private async Task ConfirmLogoutAsync()
        {
            bool confirmed = await _dialogService.ShowConfirmationAsync(
                "Sign out?",
                "You will need to approve this device again to reconnect.",
                "Sign out",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            await LogoutAsync();
        }

        private async Task LogoutAsync()
        {
            MainPageProfile? profile = _currentProfile;
            Display.ShowLoading("Signing out…");
            RefreshCommands();

            try
            {
                await _sessionService.LogoutAsync();
                await _profileCacheStore.ClearAsync();
                ClearAuthenticatedSession();
                Display.InstanceUrl = string.Empty;
                Display.ShowSignIn("Signed out.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Cotton mobile logout failed.");
                if (profile is not null)
                {
                    Display.ShowAuthenticated(profile, "Could not sign out. Try again.");
                }
                else
                {
                    Display.ShowSignIn("Could not finish signing out. Try again.");
                }
            }
            finally
            {
                RefreshCommands();
            }
        }

        private Task ShowSyncAsync()
        {
            Display.ShowDestination(AppNavigationDestination.Sync);
            return Task.CompletedTask;
        }

        private Task ShowProfileAsync()
        {
            Display.ShowDestination(AppNavigationDestination.Profile);
            return Task.CompletedTask;
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

        private async Task OpenPrivacyPolicyAsync()
        {
            try
            {
                bool opened = await MainThread.InvokeOnMainThreadAsync(
                    () => _browser.OpenAsync(
                        _options.PrivacyPolicyUri,
                        CottonBrowserLaunchOptions.SystemPreferred()));
                if (opened)
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open the Cotton Cloud privacy policy.");
            }

            await _dialogService.ShowAlertAsync(
                "Privacy Policy",
                "Could not open the privacy policy.",
                "OK");
        }

        private void RefreshCommands()
        {
            ConnectCommand.RaiseCanExecuteChanged();
            CancelAuthorizationCommand.RaiseCanExecuteChanged();
            LogoutCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(IsLogoutEnabled));
        }

        private void OnSyncPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!string.Equals(e.PropertyName, nameof(SyncSettingsViewModel.IsBusy), StringComparison.Ordinal))
            {
                return;
            }

            LogoutCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(IsLogoutEnabled));
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile command failure.");
            if (Display.IsAuthenticatedVisible)
            {
                Display.ShowProfileStatus("Something went wrong. Try again.");
                return;
            }

            Display.ShowSignIn("Something went wrong. Try again.");
            RefreshCommands();
        }

        private static string CreateApplicationVersionText(ICottonMobileApplicationMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata.ApplicationBuild))
            {
                return $"Version {metadata.ApplicationVersion}";
            }

            return $"Version {metadata.ApplicationVersion} ({metadata.ApplicationBuild})";
        }
    }
}
