// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Commands;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class MainPageViewModel : ObservableObject
    {
        private readonly MainPageSessionCoordinator _sessionCoordinator;
        private readonly MainPageUserInteractionService _userInteractionService;
        private readonly ILogger<MainPageViewModel> _logger;

        private CancellationTokenSource? _authorizationCancellation;
        private MainPageProfile? _currentProfile;
        private bool _didRestoreSession;

        public MainPageViewModel(
            MainPageSessionCoordinator sessionCoordinator,
            MainPageUserInteractionService userInteractionService,
            ICottonMobileApplicationMetadata applicationMetadata,
            SyncSettingsViewModel sync,
            ILogger<MainPageViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(sessionCoordinator);
            ArgumentNullException.ThrowIfNull(userInteractionService);
            ArgumentNullException.ThrowIfNull(applicationMetadata);
            ArgumentNullException.ThrowIfNull(sync);
            ArgumentNullException.ThrowIfNull(logger);

            _sessionCoordinator = sessionCoordinator;
            _userInteractionService = userInteractionService;
            _logger = logger;

            Display = new MainPageDisplayState(sessionCoordinator.DefaultInstanceUrl);
            Sync = sync;
            Sync.PropertyChanged += OnSyncPropertyChanged;
            ApplicationVersionText = CreateApplicationVersionText(applicationMetadata);

            ConnectCommand = new AsyncRelayCommand(
                () => AsyncCommandExecution.RunAsync(SignInAsync, LogUnhandledCommandException),
                () => Display.IsInputEnabled);
            CancelAuthorizationCommand = new AsyncRelayCommand(
                () => AsyncCommandExecution.RunAsync(CancelAuthorizationAsync, LogUnhandledCommandException),
                () => Display.IsCancelAuthorizationEnabled);
            LogoutCommand = new AsyncRelayCommand(
                () => AsyncCommandExecution.RunAsync(ConfirmLogoutAsync, LogUnhandledCommandException),
                () => IsLogoutEnabled);
            PrivacyPolicyCommand = new AsyncRelayCommand(
                () => AsyncCommandExecution.RunAsync(
                    _userInteractionService.OpenPrivacyPolicyAsync,
                    LogUnhandledCommandException));
            ShowSyncCommand = new AsyncRelayCommand(
                () => AsyncCommandExecution.RunAsync(ShowSyncAsync, LogUnhandledCommandException));
            ShowProfileCommand = new AsyncRelayCommand(
                () => AsyncCommandExecution.RunAsync(ShowProfileAsync, LogUnhandledCommandException));
        }

        public MainPageDisplayState Display { get; }

        public SyncSettingsViewModel Sync { get; }

        public string ApplicationVersionText { get; }

        public bool IsLogoutEnabled => Display.IsLogoutEnabled && !Sync.IsBusy;

        public IAsyncRelayCommand ConnectCommand { get; }

        public IAsyncRelayCommand CancelAuthorizationCommand { get; }

        public IAsyncRelayCommand LogoutCommand { get; }

        public IAsyncRelayCommand PrivacyPolicyCommand { get; }

        public IAsyncRelayCommand ShowSyncCommand { get; }

        public IAsyncRelayCommand ShowProfileCommand { get; }

        public async Task RestoreSessionOnceAsync()
        {
            if (_didRestoreSession)
            {
                return;
            }

            _didRestoreSession = true;
            Display.ShowLoading(string.Empty);
            RefreshCommands();
            MainPageSessionState state = await _sessionCoordinator.RestoreAsync();
            await ApplySessionStateAsync(state);
        }

        private async Task SignInAsync()
        {
            string instanceUrlInput = Display.InstanceUrl;
            Uri? instanceUri = CottonServerUrl.NormalizeOptional(Display.EffectiveInstanceUrl);
            if (instanceUri is null || !CottonInstanceUri.IsSupported(instanceUri))
            {
                Display.ShowSignIn(AppResources.InvalidServerUrl);
                RefreshCommands();
                return;
            }

            string signInInstanceUrl = string.IsNullOrWhiteSpace(instanceUrlInput)
                ? string.Empty
                : instanceUri.AbsoluteUri;
            Display.InstanceUrl = signInInstanceUrl;
            using CancellationTokenSource authorizationCancellation = new();
            _authorizationCancellation = authorizationCancellation;
            Display.ShowAuthorizationProgress();
            RefreshCommands();

            try
            {
                MainPageSessionState state = await _sessionCoordinator.SignInAsync(
                    instanceUri,
                    signInInstanceUrl,
                    authorizationCancellation.Token);
                await ApplySessionStateAsync(state);
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
            CancellationTokenSource? authorizationCancellation = _authorizationCancellation;
            if (authorizationCancellation is null)
            {
                return Task.CompletedTask;
            }

            Display.ShowAuthorizationCancelling();
            RefreshCommands();
            authorizationCancellation.Cancel();
            return Task.CompletedTask;
        }

        private async Task ConfirmLogoutAsync()
        {
            if (!await _userInteractionService.ConfirmSignOutAsync())
            {
                return;
            }

            MainPageProfile? profile = _currentProfile;
            Uri? instanceUri = CottonServerUrl.NormalizeOptional(Display.InstanceUrl);
            Display.ShowLoading(AppResources.SigningOut);
            RefreshCommands();
            MainPageSessionState state = await _sessionCoordinator.LogoutAsync(profile, instanceUri);
            await ApplySessionStateAsync(state);
        }

        private async Task ApplySessionStateAsync(MainPageSessionState state)
        {
            _currentProfile = state.Profile;
            Display.InstanceUrl = state.InstanceUrl;
            if (state.IsAuthenticated)
            {
                MainPageProfile profile = state.Profile
                    ?? throw new InvalidOperationException("Authenticated session requires a profile.");
                Display.ShowAuthenticated(profile, state.Status);
                RefreshCommands();
                if (state.ReloadSync)
                {
                    await Sync.LoadForInstanceAsync(state.InstanceUriValue, profile.AccountScopeKey);
                }

                return;
            }

            Sync.Clear();
            Display.ShowSignIn(state.Status);
            RefreshCommands();
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

        private void RefreshCommands()
        {
            ConnectCommand.NotifyCanExecuteChanged();
            CancelAuthorizationCommand.NotifyCanExecuteChanged();
            LogoutCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsLogoutEnabled));
        }

        private void OnSyncPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!string.Equals(e.PropertyName, nameof(SyncSettingsViewModel.IsBusy), StringComparison.Ordinal))
            {
                return;
            }

            LogoutCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsLogoutEnabled));
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile command failure.");
            if (Display.IsAuthenticatedVisible)
            {
                Display.ShowProfileStatus(AppResources.UnexpectedError);
                return;
            }

            Display.ShowSignIn(AppResources.UnexpectedError);
            RefreshCommands();
        }

        private static string CreateApplicationVersionText(ICottonMobileApplicationMetadata metadata)
        {
            return AppResources.CreateVersionText(
                metadata.ApplicationVersion,
                metadata.ApplicationBuild);
        }
    }
}
