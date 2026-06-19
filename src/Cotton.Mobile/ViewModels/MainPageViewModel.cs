using System.ComponentModel;
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
        private const string OfflineAuthorizationPendingStatus = "Offline. Reconnect to finish authorization.";
        private const string ReadyStatus = "Ready to connect.";
        private const string AccountCancelAction = "Cancel";
        private const string AccountBackupAction = "Camera Backup";
        private const string AccountCaptureInboxAction = "Capture Inbox";
        private const string AccountDiagnosticsAction = "Diagnostics";
        private const string AccountFeedbackAction = "Send feedback";
        private const string AccountLogoutAction = "Log out";
        private const string AccountPrivacyPolicyAction = "Privacy";
        private const string AccountStorageAction = "Storage";
        private const string AccountTransfersAction = "Transfers";
        private const string LogoutConfirmationTitle = "Log out?";
        private const string LogoutConfirmationMessage =
            "You will need to sign in again. Cached files on this device will be removed.";

        private readonly ICottonSessionService _sessionService;
        private readonly ICottonInstanceStore _instanceStore;
        private readonly IBrowser _browser;
        private readonly CottonMobileOptions _options;
        private readonly IUserDialogService _dialogService;
        private readonly IFeedbackService _feedbackService;
        private readonly IDiagnosticsPageService _diagnosticsPageService;
        private readonly IStorageManagementService _storageManagementService;
        private readonly IStorageSettingsPageService _storageSettingsPageService;
        private readonly ITransfersPageService _transfersPageService;
        private readonly IBackupSetupPageService _backupSetupPageService;
        private readonly ICaptureInboxPageService _captureInboxPageService;
        private readonly ICottonShareLaunchState _shareLaunchState;
        private readonly ICottonTransferQueueRestoreCoordinator _transferQueueRestoreCoordinator;
        private readonly ICottonAndroidBackgroundTransferCoordinator _backgroundTransferCoordinator;
        private readonly IAndroidApiLevelProvider _androidApiLevelProvider;
        private readonly ICottonTransferActivitySignal _transferActivitySignal;
        private readonly IScreenReaderService _screenReader;
        private readonly INetworkAccessService _networkAccess;
        private readonly IApplicationForegroundService _foregroundService;
        private readonly MainPageFileBrowserController _fileBrowser;
        private readonly IMainPagePresentationService _presentationService;
        private readonly ILogger<MainPageViewModel> _logger;

        private CancellationTokenSource? _authorizationCancellation;
        private bool _didRestoreSession;
        private bool _isSessionRestoreInProgress;
        private bool _isSessionRestoreRetryQueued;
        private bool _isCaptureInboxOpenQueued;
        private bool _shouldRetrySessionRestoreWhenOnline;
        private bool _shouldRetrySessionRestoreOnResume;

        public MainPageViewModel(
            ICottonSessionService sessionService,
            ICottonInstanceStore instanceStore,
            IBrowser browser,
            CottonMobileOptions options,
            IUserDialogService dialogService,
            IFeedbackService feedbackService,
            IDiagnosticsPageService diagnosticsPageService,
            IStorageManagementService storageManagementService,
            IStorageSettingsPageService storageSettingsPageService,
            ITransfersPageService transfersPageService,
            IBackupSetupPageService backupSetupPageService,
            ICaptureInboxPageService captureInboxPageService,
            ICottonShareLaunchState shareLaunchState,
            ICottonTransferQueueRestoreCoordinator transferQueueRestoreCoordinator,
            ICottonAndroidBackgroundTransferCoordinator backgroundTransferCoordinator,
            IAndroidApiLevelProvider androidApiLevelProvider,
            ICottonTransferActivitySignal transferActivitySignal,
            IScreenReaderService screenReader,
            ICottonFileBrowserService fileBrowserService,
            ICottonFileUploadService fileUploadService,
            ICottonFolderContentCache folderContentCache,
            IFileBrowserPreferenceStore fileBrowserPreferenceStore,
            IFileUploadPickerService fileUploadPickerService,
            IPhotoUploadPickerService photoUploadPickerService,
            IVideoUploadPickerService videoUploadPickerService,
            IUploadDestinationPickerPageService uploadDestinationPickerPageService,
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
            ArgumentNullException.ThrowIfNull(instanceStore);
            ArgumentNullException.ThrowIfNull(browser);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(feedbackService);
            ArgumentNullException.ThrowIfNull(diagnosticsPageService);
            ArgumentNullException.ThrowIfNull(storageManagementService);
            ArgumentNullException.ThrowIfNull(storageSettingsPageService);
            ArgumentNullException.ThrowIfNull(transfersPageService);
            ArgumentNullException.ThrowIfNull(backupSetupPageService);
            ArgumentNullException.ThrowIfNull(captureInboxPageService);
            ArgumentNullException.ThrowIfNull(shareLaunchState);
            ArgumentNullException.ThrowIfNull(transferQueueRestoreCoordinator);
            ArgumentNullException.ThrowIfNull(backgroundTransferCoordinator);
            ArgumentNullException.ThrowIfNull(androidApiLevelProvider);
            ArgumentNullException.ThrowIfNull(transferActivitySignal);
            ArgumentNullException.ThrowIfNull(screenReader);
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(fileUploadService);
            ArgumentNullException.ThrowIfNull(folderContentCache);
            ArgumentNullException.ThrowIfNull(fileBrowserPreferenceStore);
            ArgumentNullException.ThrowIfNull(fileUploadPickerService);
            ArgumentNullException.ThrowIfNull(photoUploadPickerService);
            ArgumentNullException.ThrowIfNull(videoUploadPickerService);
            ArgumentNullException.ThrowIfNull(uploadDestinationPickerPageService);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(filePreviewService);
            ArgumentNullException.ThrowIfNull(thumbnailProvider);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(foregroundService);
            ArgumentNullException.ThrowIfNull(fileBrowserLogger);
            ArgumentNullException.ThrowIfNull(presentationService);
            ArgumentNullException.ThrowIfNull(logger);

            _sessionService = sessionService;
            _instanceStore = instanceStore;
            _browser = browser;
            _options = options;
            _dialogService = dialogService;
            _feedbackService = feedbackService;
            _diagnosticsPageService = diagnosticsPageService;
            _storageManagementService = storageManagementService;
            _storageSettingsPageService = storageSettingsPageService;
            _transfersPageService = transfersPageService;
            _backupSetupPageService = backupSetupPageService;
            _captureInboxPageService = captureInboxPageService;
            _shareLaunchState = shareLaunchState;
            _transferQueueRestoreCoordinator = transferQueueRestoreCoordinator;
            _backgroundTransferCoordinator = backgroundTransferCoordinator;
            _androidApiLevelProvider = androidApiLevelProvider;
            _transferActivitySignal = transferActivitySignal;
            _screenReader = screenReader;
            _networkAccess = networkAccess;
            _foregroundService = foregroundService;
            _presentationService = presentationService;
            _logger = logger;

            Display = new MainPageDisplayState(options.DefaultInstanceUrl);
            _fileBrowser = new MainPageFileBrowserController(
                Display,
                fileBrowserService,
                fileUploadService,
                folderContentCache,
                fileBrowserPreferenceStore,
                fileUploadPickerService,
                photoUploadPickerService,
                videoUploadPickerService,
                uploadDestinationPickerPageService,
                fileInteractionService,
                filePreviewService,
                thumbnailProvider,
                networkAccess,
                foregroundService,
                dialogService,
                this,
                fileBrowserLogger);
            _storageManagementService.DownloadedFilesCleared += StorageManagementService_DownloadedFilesCleared;
            _shareLaunchState.ShareStaged += ShareLaunchState_ShareStaged;
            _transferActivitySignal.TransferActivityChanged += TransferActivitySignal_TransferActivityChanged;
            ConnectCommand = new AsyncCommand(SignInAsync, LogUnhandledCommandException, () => Display.IsInputEnabled);
            CancelAuthorizationCommand = new AsyncCommand(
                CancelAuthorizationAsync,
                LogUnhandledCommandException,
                () => Display.IsCancelAuthorizationEnabled);
            AccountCommand = new AsyncCommand(
                ShowAccountActionsAsync,
                LogUnhandledCommandException,
                () => Display.IsAccountActionEnabled);
            LogoutCommand = new AsyncCommand(ConfirmLogoutAsync, LogUnhandledCommandException, () => Display.IsLogoutEnabled);
            PrivacyPolicyCommand = new AsyncCommand(OpenPrivacyPolicyAsync, LogUnhandledCommandException);
            RefreshFilesCommand = new AsyncCommand(
                _fileBrowser.RefreshAsync,
                LogUnhandledCommandException,
                () => Display.IsFileBrowserChromeEnabled);
            NavigateFilesUpCommand = new AsyncCommand(
                _fileBrowser.NavigateUpAsync,
                LogUnhandledCommandException,
                () => Display.IsFileUpButtonEnabled);
            ActivateFileBrowserEntryCommand = new AsyncCommand<CottonFileBrowserEntry>(
                _fileBrowser.ActivateEntryAsync,
                LogUnhandledCommandException,
                _ => Display.IsFileBrowserChromeEnabled);
            ShowFileBrowserEntryActionsCommand = new AsyncCommand<CottonFileBrowserEntry>(
                _fileBrowser.ShowEntryActionsAsync,
                LogUnhandledCommandException,
                _ => Display.IsFileBrowserChromeEnabled);
            ShowFileAddActionsCommand = new AsyncCommand(
                _fileBrowser.ShowAddActionsAsync,
                LogUnhandledCommandException,
                () => Display.IsFileBrowserChromeEnabled);
            CancelFileActionCommand = new AsyncCommand(
                _fileBrowser.CancelFileActionAsync,
                LogUnhandledCommandException,
                () => Display.CanCancelFileAction);
            RetryFileActionCommand = new AsyncCommand(
                _fileBrowser.RetryFileActionAsync,
                LogUnhandledCommandException,
                () => Display.CanRetryFileAction);
            ToggleFileSearchCommand = new AsyncCommand(
                _fileBrowser.ToggleFileSearchAsync,
                LogUnhandledCommandException,
                () => Display.IsFileBrowserChromeEnabled);
            ShowFileViewActionsCommand = new AsyncCommand(
                _fileBrowser.ShowViewActionsAsync,
                LogUnhandledCommandException,
                () => Display.IsFileBrowserChromeEnabled);
            ShowFileSortActionsCommand = new AsyncCommand(
                _fileBrowser.ShowSortActionsAsync,
                LogUnhandledCommandException,
                () => Display.IsFileBrowserChromeEnabled);
            OpenTransfersCommand = new AsyncCommand(
                OpenTransfersAsync,
                LogUnhandledCommandException,
                () => Display.IsProfileVisible);
            Display.PropertyChanged += Display_PropertyChanged;
            _networkAccess.InternetAccessRestored += NetworkAccess_InternetAccessRestored;
            _foregroundService.Resumed += ForegroundService_Resumed;
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

        public AsyncCommand ShowFileAddActionsCommand { get; }

        public AsyncCommand CancelFileActionCommand { get; }

        public AsyncCommand RetryFileActionCommand { get; }

        public AsyncCommand ToggleFileSearchCommand { get; }

        public AsyncCommand ShowFileViewActionsCommand { get; }

        public AsyncCommand ShowFileSortActionsCommand { get; }

        public AsyncCommand OpenTransfersCommand { get; }

        public async Task<bool> RestoreSessionOnceAsync()
        {
            if (_didRestoreSession)
            {
                return false;
            }

            _didRestoreSession = true;
            await RestoreSessionAsync();
            return true;
        }

        public async Task RefreshTransferActivityAsync()
        {
            if (!Display.IsProfileVisible)
            {
                return;
            }

            Uri? instanceUri = ResolveInstanceUri();
            if (instanceUri is null)
            {
                return;
            }

            IReadOnlyList<CottonTransferQueueItem> restoredTransfers =
                await RestoreTransferQueueBestEffortAsync(instanceUri);
            Display.ShowTransferActivity(CottonTransferActivityIndicator.Create(restoredTransfers));
        }

        public async Task HandleFileBrowserSessionExpiredAsync(Uri? instanceUri)
        {
            ClearSessionRestoreRetry();
            await ClearLocalSessionAndCachedStateAsync("session expiration");

            _fileBrowser.Clear();
            Display.InstanceUrl = instanceUri?.AbsoluteUri ?? _options.DefaultInstanceUrl;
            ShowSignIn("Session expired. Sign in again.");
        }

        private async Task RestoreSessionAsync()
        {
            if (_isSessionRestoreInProgress)
            {
                return;
            }

            _isSessionRestoreInProgress = true;

            try
            {
                ShowLoading("Restoring session...");
                _shouldRetrySessionRestoreWhenOnline = false;
                _shouldRetrySessionRestoreOnResume = false;
                CottonSessionResult result = await _sessionService.RestoreAsync();
                await ApplySessionResultAsync(result, ReadyStatus);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to restore Cotton mobile session.");
                await RestoreStoredInstanceUrlBestEffortAsync("session restore failure");
                if (!_networkAccess.HasInternetAccess)
                {
                    _shouldRetrySessionRestoreWhenOnline = true;
                    _shouldRetrySessionRestoreOnResume = true;
                    ShowSignIn("Offline. Reconnect to restore your session.");
                }
                else
                {
                    _shouldRetrySessionRestoreWhenOnline = true;
                    _shouldRetrySessionRestoreOnResume = true;
                    ShowSignIn("Session restore failed. Sign in again.");
                }
            }
            finally
            {
                _isSessionRestoreInProgress = false;
                QueuePendingCaptureInboxOpen("session restore finished");
            }
        }

        private async Task RestoreStoredInstanceUrlBestEffortAsync(string reason)
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
                _logger.LogWarning(exception, "Failed to restore Cotton mobile instance URL after {Reason}.", reason);
            }
        }

        private async Task SignInAsync()
        {
            ClearSessionRestoreRetry();
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
                await ClearLocalSessionAndCachedStateAsync("authorization cancellation");
                ShowSignIn("Authorization cancelled.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Cotton mobile browser authorization failed.");
                await ClearLocalSessionAndCachedStateAsync("authorization failure");
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
            ClearSessionRestoreRetry();
            _fileBrowser.CancelActiveWork();
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
            if (!Display.IsAccountActionEnabled)
            {
                return;
            }

            string profileName = Display.ProfileName;
            string? profileEmail = Display.ProfileEmail;
            string profileInstance = Display.ProfileInstance;
            string instanceUrl = Display.InstanceUrl;
            string accountTitle = string.IsNullOrWhiteSpace(Display.ProfileSummary)
                ? Display.ProfileName
                : $"{Display.ProfileName}{Environment.NewLine}{Display.ProfileSummary}";
            string? action = await _dialogService.ShowActionSheetAsync(
                accountTitle,
                AccountCancelAction,
                AccountLogoutAction,
                AccountBackupAction,
                AccountCaptureInboxAction,
                AccountTransfersAction,
                AccountStorageAction,
                AccountDiagnosticsAction,
                AccountFeedbackAction,
                AccountPrivacyPolicyAction);

            if (!IsSameProfileContext(profileName, profileEmail, profileInstance, instanceUrl))
            {
                return;
            }

            switch (action)
            {
                case AccountLogoutAction:
                    await ConfirmLogoutAsync();
                    break;
                case AccountPrivacyPolicyAction:
                    await OpenPrivacyPolicyAsync();
                    break;
                case AccountTransfersAction:
                    await OpenTransfersAsync();
                    break;
                case AccountBackupAction:
                    await OpenBackupSetupAsync();
                    break;
                case AccountCaptureInboxAction:
                    await OpenCaptureInboxAsync();
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

        private bool IsSameProfileContext(
            string profileName,
            string? profileEmail,
            string profileInstance,
            string instanceUrl)
        {
            return Display.IsProfileVisible
                && string.Equals(Display.ProfileName, profileName, StringComparison.Ordinal)
                && string.Equals(Display.ProfileEmail, profileEmail, StringComparison.Ordinal)
                && string.Equals(Display.ProfileInstance, profileInstance, StringComparison.Ordinal)
                && string.Equals(Display.InstanceUrl, instanceUrl, StringComparison.Ordinal);
        }

        private async Task ConfirmLogoutAsync()
        {
            if (!Display.IsLogoutEnabled)
            {
                return;
            }

            bool confirmed = await _dialogService.ShowConfirmationAsync(
                LogoutConfirmationTitle,
                LogoutConfirmationMessage,
                AccountLogoutAction,
                AccountCancelAction);
            if (!confirmed)
            {
                return;
            }

            await LogoutAsync();
        }

        private void StorageManagementService_DownloadedFilesCleared(object? sender, EventArgs e)
        {
            _ = RunDownloadedFilesClearedAsync();
        }

        private async Task RunDownloadedFilesClearedAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (Display.IsProfileVisible)
                    {
                        _fileBrowser.RefreshLocalFileMarkersAfterStorageChange();
                    }
                });
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to clear Cotton mobile local file markers after storage cleanup.");
            }
        }

        private void NetworkAccess_InternetAccessRestored(object? sender, EventArgs e)
        {
            if (!_shouldRetrySessionRestoreWhenOnline && !_shouldRetrySessionRestoreOnResume)
            {
                return;
            }

            QueueSessionRestoreRetry("internet access restored");
        }

        private void ForegroundService_Resumed(object? sender, EventArgs e)
        {
            if (!_shouldRetrySessionRestoreOnResume)
            {
                return;
            }

            QueueSessionRestoreRetry("foreground resume");
        }

        private void QueueSessionRestoreRetry(string reason)
        {
            if (_isSessionRestoreRetryQueued)
            {
                return;
            }

            _isSessionRestoreRetryQueued = true;
            _ = RunQueuedSessionRestoreRetryAsync(reason);
        }

        private async Task RunQueuedSessionRestoreRetryAsync(string reason)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => RetrySessionRestoreAfterSignalAsync(reason));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to queue Cotton mobile session restore retry after {Reason}.",
                    reason);
                _isSessionRestoreRetryQueued = false;
            }
        }

        private async Task RetrySessionRestoreAfterSignalAsync(string reason)
        {
            try
            {
                if ((!_shouldRetrySessionRestoreWhenOnline && !_shouldRetrySessionRestoreOnResume)
                    || _isSessionRestoreInProgress
                    || !Display.IsSignInVisible
                    || !_networkAccess.HasInternetAccess)
                {
                    return;
                }

                _logger.LogInformation("Retrying Cotton mobile session restore after {Reason}.", reason);
                await RestoreSessionAsync();
            }
            finally
            {
                _isSessionRestoreRetryQueued = false;
            }
        }

        private void ClearSessionRestoreRetry()
        {
            _shouldRetrySessionRestoreWhenOnline = false;
            _shouldRetrySessionRestoreOnResume = false;
            _isSessionRestoreRetryQueued = false;
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

        private async Task OpenTransfersAsync()
        {
            Uri? instanceUri = ResolveInstanceUri();
            if (instanceUri is null)
            {
                await _dialogService.ShowAlertAsync(
                    "Transfers",
                    "Could not open transfers for this instance.",
                    "OK");
                return;
            }

            try
            {
                await _transfersPageService.OpenAsync(instanceUri);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton mobile transfers page.");
                await _dialogService.ShowAlertAsync(
                    "Transfers",
                    "Could not inspect transfers.",
                    "OK");
            }
        }

        private async Task OpenBackupSetupAsync()
        {
            Uri? instanceUri = ResolveInstanceUri();
            if (instanceUri is null)
            {
                await _dialogService.ShowAlertAsync(
                    "Camera Backup",
                    "Could not open camera backup for this instance.",
                    "OK");
                return;
            }

            try
            {
                await _backupSetupPageService.OpenAsync(instanceUri);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton mobile camera backup setup page.");
                await _dialogService.ShowAlertAsync(
                    "Camera Backup",
                    "Could not open camera backup setup.",
                    "OK");
            }
        }

        private async Task OpenCaptureInboxAsync()
        {
            await TryOpenCaptureInboxAsync(showAlertOnFailure: true);
        }

        private async Task<bool> TryOpenCaptureInboxAsync(bool showAlertOnFailure)
        {
            Uri? instanceUri = ResolveInstanceUri();
            if (instanceUri is null)
            {
                if (showAlertOnFailure)
                {
                    await _dialogService.ShowAlertAsync(
                        "Capture Inbox",
                        "Could not open captured items for this instance.",
                        "OK");
                }

                return false;
            }

            try
            {
                await _captureInboxPageService.OpenAsync(instanceUri);
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton mobile capture inbox page.");
                if (showAlertOnFailure)
                {
                    await _dialogService.ShowAlertAsync(
                        "Capture Inbox",
                        "Could not inspect captured items.",
                        "OK");
                }

                return false;
            }
        }

        private void ShareLaunchState_ShareStaged(object? sender, EventArgs e)
        {
            QueuePendingCaptureInboxOpen("share launch");
        }

        private void TransferActivitySignal_TransferActivityChanged(object? sender, EventArgs e)
        {
            _ = RunTransferActivityRefreshAfterSignalAsync();
        }

        private async Task RunTransferActivityRefreshAfterSignalAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(RefreshTransferActivityAsync);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to refresh Cotton mobile transfer activity after queue change.");
            }
        }

        private void QueuePendingCaptureInboxOpen(string reason)
        {
            if (_isCaptureInboxOpenQueued || _shareLaunchState.PendingShareLaunchCount == 0)
            {
                return;
            }

            _isCaptureInboxOpenQueued = true;
            _ = RunQueuedCaptureInboxOpenAsync(reason);
        }

        private async Task RunQueuedCaptureInboxOpenAsync(string reason)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => OpenPendingCaptureInboxAfterShareAsync(reason));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to queue Cotton mobile Capture Inbox open after {Reason}.",
                    reason);
                _isCaptureInboxOpenQueued = false;
            }
        }

        private async Task OpenPendingCaptureInboxAfterShareAsync(string reason)
        {
            try
            {
                if (_isSessionRestoreInProgress || !Display.IsProfileVisible)
                {
                    return;
                }

                if (_shareLaunchState.PendingShareLaunchCount == 0)
                {
                    return;
                }

                _logger.LogInformation("Opening Cotton mobile Capture Inbox after {Reason}.", reason);
                if (await TryOpenCaptureInboxAsync(showAlertOnFailure: false))
                {
                    _shareLaunchState.TryConsumePendingShareLaunch();
                }
            }
            finally
            {
                _isCaptureInboxOpenQueued = false;
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
                bool opened = await MainThread.InvokeOnMainThreadAsync(
                    () => _browser.OpenAsync(
                        _options.PrivacyPolicyUri,
                        CottonBrowserLaunchOptions.SystemPreferred()));
                if (!opened)
                {
                    await ShowPrivacyPolicyUnavailableAsync();
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton Cloud privacy policy.");
                await ShowPrivacyPolicyUnavailableAsync();
            }
        }

        private Task ShowPrivacyPolicyUnavailableAsync()
        {
            return _dialogService.ShowAlertAsync(
                "Privacy Policy",
                "Could not open the privacy policy.",
                "OK");
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
                FormatFileViewMode(Display.FileViewMode),
                FormatFileSortMode(Display.FileSortMode),
                Display.IsFileSearchActive,
                Display.FilesStatus,
                _networkAccess.HasInternetAccess);
        }

        private static string FormatFileViewMode(CottonFileBrowserViewMode viewMode)
        {
            return viewMode switch
            {
                CottonFileBrowserViewMode.List => "List",
                CottonFileBrowserViewMode.Tiles => "Tiles",
                _ => viewMode.ToString(),
            };
        }

        private static string FormatFileSortMode(CottonFileBrowserSortMode sortMode)
        {
            return sortMode switch
            {
                CottonFileBrowserSortMode.Name => "A-Z",
                CottonFileBrowserSortMode.Updated => "Newest",
                CottonFileBrowserSortMode.Type => "Type",
                CottonFileBrowserSortMode.Size => "Size",
                _ => sortMode.ToString(),
            };
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
            if (instanceUri is null || !CottonInstanceUri.IsSupported(instanceUri))
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
                ClearSessionRestoreRetry();
                MainPageProfile profile = _presentationService.CreateProfile(result.InstanceUri, result.User);
                ShowProfile(profile);
                IReadOnlyList<CottonTransferQueueItem> restoredTransfers =
                    await RestoreTransferQueueBestEffortAsync(result.InstanceUri);
                Display.ShowTransferActivity(CottonTransferActivityIndicator.Create(restoredTransfers));
                await ResumeQueuedBackgroundTransferBestEffortAsync(result.InstanceUri);
                await _fileBrowser.InitializeAsync(result.InstanceUri);
                RefreshCommands();
                QueuePendingCaptureInboxOpen("authenticated session");
                return;
            }

            if (result.Status == CottonSessionResultStatus.AuthorizationPending)
            {
                _shouldRetrySessionRestoreWhenOnline = true;
                _shouldRetrySessionRestoreOnResume = true;
            }

            if (ShouldClearLocalSessionAndCachedState(result))
            {
                await ClearLocalSessionAndCachedStateAsync("session invalidation");
            }

            _fileBrowser.Clear();
            ShowSignIn(ResolveSessionStatusMessage(result, unauthenticatedStatus));
        }

        private string ResolveSessionStatusMessage(CottonSessionResult result, string unauthenticatedStatus)
        {
            if (result.Status == CottonSessionResultStatus.AuthorizationPending
                && !_networkAccess.HasInternetAccess)
            {
                return OfflineAuthorizationPendingStatus;
            }

            return _presentationService.ResolveStatusMessage(result, unauthenticatedStatus);
        }

        private static bool ShouldClearLocalSessionAndCachedState(CottonSessionResult result)
        {
            return result.InstanceUri is not null
                && result.Status is CottonSessionResultStatus.Unauthenticated
                    or CottonSessionResultStatus.AuthorizationDenied
                    or CottonSessionResultStatus.AuthorizationExpired
                    or CottonSessionResultStatus.AuthorizationNotFound
                    or CottonSessionResultStatus.BrowserUnavailable
                    or CottonSessionResultStatus.TimedOut
                    or CottonSessionResultStatus.AuthorizationFailed
                    or CottonSessionResultStatus.SessionExpired;
        }

        private async Task<IReadOnlyList<CottonTransferQueueItem>> RestoreTransferQueueBestEffortAsync(Uri instanceUri)
        {
            try
            {
                return await _transferQueueRestoreCoordinator.RestoreAsync(instanceUri);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to restore Cotton mobile transfer queue.");
                return [];
            }
        }

        private async Task ResumeQueuedBackgroundTransferBestEffortAsync(Uri instanceUri)
        {
            try
            {
                CottonAndroidBackgroundTransferScheduleResult result =
                    await _backgroundTransferCoordinator.ScheduleNextQueuedUploadAsync(
                        instanceUri,
                        _androidApiLevelProvider.CurrentApiLevel);
                if (result.IsScheduled)
                {
                    _logger.LogInformation(
                        "Rescheduled Cotton mobile queued upload {TransferId} with Android background host {Host}.",
                        result.Request?.TransferId,
                        result.Request?.Host);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to resume Cotton mobile queued background upload.");
            }
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
            RefreshFilesCommand.RaiseCanExecuteChanged();
            NavigateFilesUpCommand.RaiseCanExecuteChanged();
            ActivateFileBrowserEntryCommand.RaiseCanExecuteChanged();
            ShowFileBrowserEntryActionsCommand.RaiseCanExecuteChanged();
            ShowFileAddActionsCommand.RaiseCanExecuteChanged();
            CancelFileActionCommand.RaiseCanExecuteChanged();
            RetryFileActionCommand.RaiseCanExecuteChanged();
            ToggleFileSearchCommand.RaiseCanExecuteChanged();
            ShowFileViewActionsCommand.RaiseCanExecuteChanged();
            ShowFileSortActionsCommand.RaiseCanExecuteChanged();
            OpenTransfersCommand.RaiseCanExecuteChanged();
        }

        private void Display_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainPageDisplayState.IsInputEnabled):
                    ConnectCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.IsCancelAuthorizationEnabled):
                    CancelAuthorizationCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.IsAccountActionEnabled):
                    AccountCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.IsLogoutEnabled):
                    LogoutCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.IsProfileVisible):
                    RefreshFilesCommand.RaiseCanExecuteChanged();
                    OpenTransfersCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.IsFileUpButtonEnabled):
                    NavigateFilesUpCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.IsFileBrowserChromeEnabled):
                    RefreshFilesCommand.RaiseCanExecuteChanged();
                    ActivateFileBrowserEntryCommand.RaiseCanExecuteChanged();
                    ShowFileBrowserEntryActionsCommand.RaiseCanExecuteChanged();
                    ShowFileAddActionsCommand.RaiseCanExecuteChanged();
                    ToggleFileSearchCommand.RaiseCanExecuteChanged();
                    ShowFileViewActionsCommand.RaiseCanExecuteChanged();
                    ShowFileSortActionsCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.CanCancelFileAction):
                    CancelFileActionCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.CanRetryFileAction):
                    RetryFileActionCommand.RaiseCanExecuteChanged();
                    break;
            }
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile main-page command exception.");
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
