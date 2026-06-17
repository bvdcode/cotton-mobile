using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.ViewModels
{
    public class MainPageViewModel : IFileBrowserSessionHandler
    {
        private const string InvalidUrlStatus = "Enter a valid HTTPS URL.";
        private const string ReadyStatus = "Ready to connect.";
        private const string AccountCancelAction = "Cancel";
        private const string AccountDiagnosticsAction = "Diagnostics";
        private const string AccountFeedbackAction = "Send feedback";
        private const string AccountLogoutAction = "Log out";
        private const string AccountPrivacyPolicyAction = "Privacy";
        private const string AccountStorageAction = "Storage";

        private readonly ICottonSessionService _sessionService;
        private readonly IBrowser _browser;
        private readonly CottonMobileOptions _options;
        private readonly IUserDialogService _dialogService;
        private readonly IFeedbackService _feedbackService;
        private readonly IDiagnosticsPageService _diagnosticsPageService;
        private readonly IStorageManagementService _storageManagementService;
        private readonly IStorageSettingsPageService _storageSettingsPageService;
        private readonly IScreenReaderService _screenReader;
        private readonly INetworkAccessService _networkAccess;
        private readonly MainPageFileBrowserController _fileBrowser;
        private readonly IMainPagePresentationService _presentationService;
        private readonly ILogger<MainPageViewModel> _logger;

        private CancellationTokenSource? _authorizationCancellation;
        private bool _didRestoreSession;

        public MainPageViewModel(
            ICottonSessionService sessionService,
            IBrowser browser,
            CottonMobileOptions options,
            IUserDialogService dialogService,
            IFeedbackService feedbackService,
            IDiagnosticsPageService diagnosticsPageService,
            IStorageManagementService storageManagementService,
            IStorageSettingsPageService storageSettingsPageService,
            IScreenReaderService screenReader,
            ICottonFileBrowserService fileBrowserService,
            IFileBrowserPreferenceStore fileBrowserPreferenceStore,
            IFileInteractionService fileInteractionService,
            IFilePreviewService filePreviewService,
            IFileThumbnailProvider thumbnailProvider,
            INetworkAccessService networkAccess,
            IApplicationForegroundService foregroundService,
            ILogger<MainPageFileBrowserController> fileBrowserLogger,
            IMainPagePresentationService presentationService,
            ILogger<MainPageViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(sessionService);
            ArgumentNullException.ThrowIfNull(browser);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(feedbackService);
            ArgumentNullException.ThrowIfNull(diagnosticsPageService);
            ArgumentNullException.ThrowIfNull(storageManagementService);
            ArgumentNullException.ThrowIfNull(storageSettingsPageService);
            ArgumentNullException.ThrowIfNull(screenReader);
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(fileBrowserPreferenceStore);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(filePreviewService);
            ArgumentNullException.ThrowIfNull(thumbnailProvider);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(foregroundService);
            ArgumentNullException.ThrowIfNull(fileBrowserLogger);
            ArgumentNullException.ThrowIfNull(presentationService);
            ArgumentNullException.ThrowIfNull(logger);

            _sessionService = sessionService;
            _browser = browser;
            _options = options;
            _dialogService = dialogService;
            _feedbackService = feedbackService;
            _diagnosticsPageService = diagnosticsPageService;
            _storageManagementService = storageManagementService;
            _storageSettingsPageService = storageSettingsPageService;
            _screenReader = screenReader;
            _networkAccess = networkAccess;
            _presentationService = presentationService;
            _logger = logger;

            Display = new MainPageDisplayState(options.DefaultInstanceUrl);
            _fileBrowser = new MainPageFileBrowserController(
                Display,
                fileBrowserService,
                fileBrowserPreferenceStore,
                fileInteractionService,
                filePreviewService,
                thumbnailProvider,
                networkAccess,
                foregroundService,
                dialogService,
                this,
                fileBrowserLogger);
            _storageManagementService.DownloadedFilesCleared += StorageManagementService_DownloadedFilesCleared;
            ConnectCommand = new AsyncCommand(SignInAsync, () => Display.IsInputEnabled);
            CancelAuthorizationCommand = new AsyncCommand(CancelAuthorizationAsync, () => Display.IsCancelAuthorizationEnabled);
            AccountCommand = new AsyncCommand(ShowAccountActionsAsync, () => Display.IsProfileVisible);
            LogoutCommand = new AsyncCommand(LogoutAsync, () => Display.IsLogoutEnabled);
            PrivacyPolicyCommand = new AsyncCommand(OpenPrivacyPolicyAsync);
            RefreshFilesCommand = new AsyncCommand(_fileBrowser.RefreshAsync);
            NavigateFilesUpCommand = new AsyncCommand(_fileBrowser.NavigateUpAsync, () => Display.CanNavigateFilesUp);
            ActivateFileBrowserEntryCommand = new AsyncCommand<CottonFileBrowserEntry>(_fileBrowser.ActivateEntryAsync);
            ShowFileBrowserEntryActionsCommand = new AsyncCommand<CottonFileBrowserEntry>(_fileBrowser.ShowEntryActionsAsync);
            CancelFileActionCommand = new AsyncCommand(_fileBrowser.CancelFileActionAsync, () => Display.CanCancelFileAction);
            RetryFileActionCommand = new AsyncCommand(_fileBrowser.RetryFileActionAsync, () => Display.CanRetryFileAction);
            ToggleFileSearchCommand = new AsyncCommand(ToggleFileSearchAsync);
            ShowFileViewActionsCommand = new AsyncCommand(_fileBrowser.ShowViewActionsAsync);
            ShowFileSortActionsCommand = new AsyncCommand(_fileBrowser.ShowSortActionsAsync);
        }

        public MainPageDisplayState Display { get; }

        public AsyncCommand ConnectCommand { get; }

        public AsyncCommand CancelAuthorizationCommand { get; }

        public AsyncCommand AccountCommand { get; }

        public AsyncCommand LogoutCommand { get; }

        public AsyncCommand PrivacyPolicyCommand { get; }

        public AsyncCommand RefreshFilesCommand { get; }

        public AsyncCommand NavigateFilesUpCommand { get; }

        public AsyncCommand<CottonFileBrowserEntry> ActivateFileBrowserEntryCommand { get; }

        public AsyncCommand<CottonFileBrowserEntry> ShowFileBrowserEntryActionsCommand { get; }

        public AsyncCommand CancelFileActionCommand { get; }

        public AsyncCommand RetryFileActionCommand { get; }

        public AsyncCommand ToggleFileSearchCommand { get; }

        public AsyncCommand ShowFileViewActionsCommand { get; }

        public AsyncCommand ShowFileSortActionsCommand { get; }

        public async Task RestoreSessionOnceAsync()
        {
            if (_didRestoreSession)
            {
                return;
            }

            _didRestoreSession = true;
            await RestoreSessionAsync();
        }

        public async Task HandleFileBrowserSessionExpiredAsync(Uri? instanceUri)
        {
            await ClearLocalSessionAndCachedStateAsync("session expiration");

            _fileBrowser.Clear();
            Display.InstanceUrl = instanceUri?.AbsoluteUri ?? _options.DefaultInstanceUrl;
            ShowSignIn("Session expired. Sign in again.");
        }

        private async Task RestoreSessionAsync()
        {
            ShowLoading("Restoring session...");

            try
            {
                CottonSessionResult result = await _sessionService.RestoreAsync();
                await ApplySessionResultAsync(result, ReadyStatus);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to restore Cotton mobile session.");
                ShowSignIn("Session restore failed. Sign in again.");
            }
        }

        private async Task SignInAsync()
        {
            Uri? instanceUri = ResolveInstanceUri();
            if (instanceUri is null)
            {
                ShowSignIn(InvalidUrlStatus);
                return;
            }

            Display.InstanceUrl = instanceUri.AbsoluteUri;

            using var authorizationCancellation = new CancellationTokenSource();
            _authorizationCancellation = authorizationCancellation;
            ShowAuthorizationProgress(instanceUri);

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
                ShowSignIn("Authorization cancelled.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Cotton mobile browser authorization failed.");
                ShowSignIn(_presentationService.CreateAuthorizationFailureStatus(exception));
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

        private async Task LogoutAsync()
        {
            ShowLoading("Signing out...");

            try
            {
                await _sessionService.LogoutAsync();
                await ClearCachedSensitiveStateAsync("logout");
                _fileBrowser.Clear();
                Display.InstanceUrl = _options.DefaultInstanceUrl;
                ShowSignIn("Signed out.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Cotton mobile logout failed.");
                ShowProfileError("Logout failed. Try again.");
            }
        }

        private async Task ShowAccountActionsAsync()
        {
            if (!Display.IsProfileVisible)
            {
                return;
            }

            string accountTitle = string.IsNullOrWhiteSpace(Display.ProfileSummary)
                ? Display.ProfileName
                : $"{Display.ProfileName}{Environment.NewLine}{Display.ProfileSummary}";
            string? action = await _dialogService.ShowActionSheetAsync(
                accountTitle,
                AccountCancelAction,
                AccountLogoutAction,
                AccountStorageAction,
                AccountDiagnosticsAction,
                AccountFeedbackAction,
                AccountPrivacyPolicyAction);

            switch (action)
            {
                case AccountLogoutAction:
                    await LogoutAsync();
                    break;
                case AccountPrivacyPolicyAction:
                    await OpenPrivacyPolicyAsync();
                    break;
                case AccountStorageAction:
                    await OpenStorageAsync();
                    break;
                case AccountDiagnosticsAction:
                    await OpenDiagnosticsAsync();
                    break;
                case AccountFeedbackAction:
                    await OpenFeedbackAsync();
                    break;
            }
        }

        private Task ToggleFileSearchAsync()
        {
            Display.ToggleFileSearch();
            return Task.CompletedTask;
        }

        private void StorageManagementService_DownloadedFilesCleared(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(_fileBrowser.ClearLocalFileMarkers);
        }

        private async Task ClearLocalSessionAndCachedStateAsync(string reason)
        {
            try
            {
                await _sessionService.ClearLocalSessionAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to clear Cotton mobile local session during {Reason}.", reason);
            }

            await ClearCachedSensitiveStateAsync(reason);
        }

        private async Task ClearCachedSensitiveStateAsync(string reason)
        {
            try
            {
                await _storageManagementService.ClearAllCachedFilesAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to clear Cotton mobile cached files during {Reason}.", reason);
            }
        }

        private async Task OpenStorageAsync()
        {
            try
            {
                await _storageSettingsPageService.OpenAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton mobile storage page.");
                await _dialogService.ShowAlertAsync(
                    "Storage",
                    "Could not inspect app storage.",
                    "OK");
            }
        }

        private async Task OpenDiagnosticsAsync()
        {
            try
            {
                await _diagnosticsPageService.OpenAsync(CreateDiagnosticsContext());
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton mobile diagnostics page.");
                await _dialogService.ShowAlertAsync(
                    "Diagnostics",
                    "Could not inspect app diagnostics.",
                    "OK");
            }
        }

        private async Task OpenPrivacyPolicyAsync()
        {
            try
            {
                await _browser.OpenAsync(
                    _options.PrivacyPolicyUri,
                    CottonBrowserLaunchOptions.SystemPreferred());
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton Cloud privacy policy.");
                await _dialogService.ShowAlertAsync(
                    "Privacy Policy",
                    "Could not open the privacy policy.",
                    "OK");
            }
        }

        private async Task OpenFeedbackAsync()
        {
            FeedbackContext? context = null;

            try
            {
                context = await CreateFeedbackContextAsync();
                FeedbackDeliveryResult result = await _feedbackService.OpenFeedbackAsync(context);
                if (result == FeedbackDeliveryResult.CopiedToClipboard)
                {
                    await ShowFeedbackCopiedAsync();
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton Cloud feedback composer.");
                if (context is null)
                {
                    await ShowFeedbackUnavailableAsync();
                    return;
                }

                await CopyFeedbackAfterComposerFailureAsync(context);
            }
        }

        private async Task CopyFeedbackAfterComposerFailureAsync(FeedbackContext context)
        {
            try
            {
                await _feedbackService.CopyFeedbackAsync(context);
                await ShowFeedbackCopiedAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to copy Cotton Cloud feedback details.");
                await ShowFeedbackUnavailableAsync();
            }
        }

        private async Task<FeedbackContext> CreateFeedbackContextAsync()
        {
            CottonDiagnosticsContext diagnostics = CreateDiagnosticsContext();
            CottonStorageSummary? storageSummary = await TryCreateFeedbackStorageSummaryAsync();
            return new FeedbackContext(
                diagnostics.InstanceUrl,
                diagnostics.ProfileName,
                diagnostics.Screen,
                diagnostics.FileLocation,
                diagnostics.VisibleFileCount,
                diagnostics.TotalFileCount,
                diagnostics.FileViewMode,
                diagnostics.FileSortMode,
                diagnostics.IsFileSearchActive,
                diagnostics.FilesStatus,
                diagnostics.HasInternetAccess,
                storageSummary);
        }

        private CottonDiagnosticsContext CreateDiagnosticsContext()
        {
            return new CottonDiagnosticsContext(
                Display.InstanceUrl,
                Display.ProfileName,
                CreateFeedbackScreenName(),
                CreateFeedbackFileLocation(),
                Display.VisibleFileEntryCount,
                Display.TotalFileEntryCount,
                Display.FileViewMode.ToString(),
                Display.FileSortMode.ToString(),
                Display.IsFileSearchActive,
                Display.FilesStatus,
                _networkAccess.HasInternetAccess);
        }

        private async Task<CottonStorageSummary?> TryCreateFeedbackStorageSummaryAsync()
        {
            try
            {
                return await _storageManagementService.GetSummaryAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to collect Cotton mobile storage summary for feedback.");
                return null;
            }
        }

        private string CreateFeedbackScreenName()
        {
            if (Display.IsLoadingVisible)
            {
                return "Loading";
            }

            if (Display.IsSignInVisible)
            {
                return "Sign in";
            }

            if (Display.IsAuthorizationProgressVisible)
            {
                return "Browser authorization";
            }

            if (Display.IsProfileVisible)
            {
                return "File browser";
            }

            return "Unknown";
        }

        private string CreateFeedbackFileLocation()
        {
            if (!Display.IsProfileVisible)
            {
                return "Not browsing files";
            }

            return Display.CanNavigateFilesUp ? "Nested folder" : "Root folder";
        }

        private Task ShowFeedbackUnavailableAsync()
        {
            return _dialogService.ShowAlertAsync(
                "Feedback",
                $"Could not open an email app. Send feedback to {_options.SupportEmail}.",
                "OK");
        }

        private Task ShowFeedbackCopiedAsync()
        {
            return _dialogService.ShowAlertAsync(
                "Feedback",
                $"Could not open an email app. Feedback details were copied. Send them to {_options.SupportEmail}.",
                "OK");
        }

        private Uri? ResolveInstanceUri()
        {
            Uri? instanceUri = CottonServerUrl.NormalizeOptional(Display.InstanceUrl);
            if (instanceUri is null
                || !string.Equals(instanceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(instanceUri.Host))
            {
                return null;
            }

            return instanceUri;
        }

        private async Task ApplySessionResultAsync(CottonSessionResult result, string unauthenticatedStatus)
        {
            if (result.InstanceUri is not null)
            {
                Display.InstanceUrl = result.InstanceUri.AbsoluteUri;
            }

            if (result.IsAuthenticated && result.InstanceUri is not null && result.User is not null)
            {
                MainPageProfile profile = _presentationService.CreateProfile(result.InstanceUri, result.User);
                ShowProfile(profile);
                await _fileBrowser.InitializeAsync(result.InstanceUri);
                RefreshCommands();
                return;
            }

            if (ShouldClearLocalSessionAndCachedState(result))
            {
                await ClearLocalSessionAndCachedStateAsync("session invalidation");
            }

            _fileBrowser.Clear();
            ShowSignIn(_presentationService.ResolveStatusMessage(result, unauthenticatedStatus));
        }

        private static bool ShouldClearLocalSessionAndCachedState(CottonSessionResult result)
        {
            return result.Status == CottonSessionResultStatus.SessionExpired
                || (result.Status == CottonSessionResultStatus.Unauthenticated && result.InstanceUri is not null);
        }

        private void ShowLoading(string message)
        {
            Display.ShowLoading(message);
            RefreshCommands();
            _screenReader.Announce(message);
        }

        private void ShowSignIn(string? status)
        {
            Display.ShowSignIn(status);
            RefreshCommands();
            AnnounceStatus(status);
        }

        private void ShowAuthorizationProgress(Uri instanceUri)
        {
            Display.ShowAuthorizationProgress(instanceUri);
            RefreshCommands();
            _screenReader.Announce("Waiting for browser approval.");
        }

        private void ShowProfile(MainPageProfile profile)
        {
            Display.ShowProfile(profile);
            RefreshCommands();
            _screenReader.Announce("Signed in.");
        }

        private void ShowProfileError(string status)
        {
            Display.ShowProfileError(status);
            RefreshCommands();
            _screenReader.Announce(status);
        }

        private void RefreshCommands()
        {
            ConnectCommand.RaiseCanExecuteChanged();
            CancelAuthorizationCommand.RaiseCanExecuteChanged();
            AccountCommand.RaiseCanExecuteChanged();
            LogoutCommand.RaiseCanExecuteChanged();
            NavigateFilesUpCommand.RaiseCanExecuteChanged();
            ActivateFileBrowserEntryCommand.RaiseCanExecuteChanged();
            ShowFileBrowserEntryActionsCommand.RaiseCanExecuteChanged();
            CancelFileActionCommand.RaiseCanExecuteChanged();
            RetryFileActionCommand.RaiseCanExecuteChanged();
            ToggleFileSearchCommand.RaiseCanExecuteChanged();
            ShowFileViewActionsCommand.RaiseCanExecuteChanged();
            ShowFileSortActionsCommand.RaiseCanExecuteChanged();
        }

        private void AnnounceStatus(string? status)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                _screenReader.Announce(status);
            }
        }
    }
}
