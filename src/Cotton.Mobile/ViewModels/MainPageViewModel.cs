using System.ComponentModel;
using System.Net;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.ViewModels
{
    public class MainPageViewModel : IFileBrowserSessionHandler, ICottonCurrentSessionRevocationHandler
    {
        private const string InvalidUrlStatus = "Enter a valid HTTPS URL.";
        private const string OfflineAuthorizationPendingStatus = "Offline. Reconnect to finish authorization.";
        private const string OfflineCachedSessionStatus =
            "Offline. Showing saved files until your session can refresh.";
        private const string ReadyStatus = "Ready to connect.";
        private const string AccountCancelAction = "Cancel";
        private const string AccountActivityAction = "Activity";
        private const string AccountDiagnosticsAction = "Diagnostics";
        private const string AccountFeedbackAction = "Send feedback";
        private const string AccountLogoutAction = "Log out";
        private const string AccountNotificationsAction = "Notifications";
        private const string AccountPrivacyPolicyAction = "Privacy";
        private const string AccountResetFileLinksAction = "Reset file links";
        private const string AccountSecurityAction = "Security";
        private const string AccountResetFileLinksConfirmationAction = "Reset file links";
        private const string AccountStorageAction = "Storage";
        private const string AccountSyncAction = "Sync";
        private const string LogoutConfirmationTitle = "Log out?";
        private const string LogoutConfirmationClearCacheMessage =
            "You will need to sign in again. Cached files on this device will be removed.";
        private const string LogoutConfirmationKeepCacheMessage =
            "You will need to sign in again. Cached files on this device will stay here.";
        private const string ResetFileLinksConfirmationTitle = "Reset file links?";
        private const string ResetFileLinksConfirmationMessage =
            "Existing public file links for this account will stop working. Folder links stay active.";

        private readonly ICottonSessionService _sessionService;
        private readonly ICottonInstanceStore _instanceStore;
        private readonly ICottonProfileCacheStore _profileCacheStore;
        private readonly IBrowser _browser;
        private readonly CottonMobileOptions _options;
        private readonly IUserDialogService _dialogService;
        private readonly IFeedbackService _feedbackService;
        private readonly IDiagnosticsPageService _diagnosticsPageService;
        private readonly IStorageManagementService _storageManagementService;
        private readonly ICottonLogoutCacheCleanupSettingsStore _logoutCleanupSettingsStore;
        private readonly IStorageSettingsPageService _storageSettingsPageService;
        private readonly ISyncSettingsPageService _syncSettingsPageService;
        private readonly ISecuritySettingsPageService _securitySettingsPageService;
        private readonly ITransfersPageService _transfersPageService;
        private readonly IBackupSetupPageService _backupSetupPageService;
        private readonly ICaptureInboxPageService _captureInboxPageService;
        private readonly INotificationSettingsPageService _notificationSettingsPageService;
        private readonly IActivityFeedPageService _activityFeedPageService;
        private readonly ICottonShareLaunchState _shareLaunchState;
        private readonly ICottonNotificationLaunchState _notificationLaunchState;
        private readonly ICottonTransferQueueRestoreCoordinator _transferQueueRestoreCoordinator;
        private readonly ICottonAndroidBackgroundTransferCoordinator _backgroundTransferCoordinator;
        private readonly ICottonAndroidBackgroundSyncCoordinator _backgroundSyncCoordinator;
        private readonly IAndroidApiLevelProvider _androidApiLevelProvider;
        private readonly ICottonTransferActivitySignal _transferActivitySignal;
        private readonly IScreenReaderService _screenReader;
        private readonly INetworkAccessService _networkAccess;
        private readonly IApplicationForegroundService _foregroundService;
        private readonly ICottonCloudShareLinkService _cloudShareLinkService;
        private readonly ICottonAccountSessionService _accountSessionService;
        private readonly ICottonRemotePushSessionRegistrationService _remotePushRegistrationService;
        private readonly CottonCloudToDeviceSyncCoordinator _cloudToDeviceSyncCoordinator;
        private readonly MainPageFileBrowserController _fileBrowser;
        private readonly IMainPagePresentationService _presentationService;
        private readonly ILogger<MainPageViewModel> _logger;

        private CancellationTokenSource? _authorizationCancellation;
        private CancellationTokenSource? _cloudToDeviceSyncRestoreCancellation;
        private bool _didRestoreSession;
        private bool _isSessionRestoreInProgress;
        private bool _isSessionRestoreRetryQueued;
        private bool _isCaptureInboxOpenQueued;
        private bool _isNotificationOpenQueued;
        private bool _isCloudToDeviceSyncRestoreInProgress;
        private bool _shouldRetrySessionRestoreWhenOnline;
        private bool _shouldRetrySessionRestoreOnResume;
        private bool _isShowingCachedOfflineSession;

        public MainPageViewModel(
            ICottonSessionService sessionService,
            ICottonInstanceStore instanceStore,
            ICottonProfileCacheStore profileCacheStore,
            IBrowser browser,
            CottonMobileOptions options,
            IUserDialogService dialogService,
            IFeedbackService feedbackService,
            IDiagnosticsPageService diagnosticsPageService,
            IStorageManagementService storageManagementService,
            ICottonLogoutCacheCleanupSettingsStore logoutCleanupSettingsStore,
            IStorageSettingsPageService storageSettingsPageService,
            ISyncSettingsPageService syncSettingsPageService,
            ISecuritySettingsPageService securitySettingsPageService,
            ITransfersPageService transfersPageService,
            IBackupSetupPageService backupSetupPageService,
            ICaptureInboxPageService captureInboxPageService,
            INotificationSettingsPageService notificationSettingsPageService,
            IActivityFeedPageService activityFeedPageService,
            ICottonShareLaunchState shareLaunchState,
            ICottonNotificationLaunchState notificationLaunchState,
            ICottonTransferQueueRestoreCoordinator transferQueueRestoreCoordinator,
            ICottonAndroidBackgroundTransferCoordinator backgroundTransferCoordinator,
            ICottonAndroidBackgroundSyncCoordinator backgroundSyncCoordinator,
            IAndroidApiLevelProvider androidApiLevelProvider,
            ICottonTransferActivitySignal transferActivitySignal,
            IScreenReaderService screenReader,
            ICottonFileBrowserService fileBrowserService,
            ICottonFileUploadService fileUploadService,
            ICottonFolderContentCache folderContentCache,
            ICottonOfflineFilePinStore offlineFilePinStore,
            IFileBrowserPreferenceStore fileBrowserPreferenceStore,
            IFileUploadPickerService fileUploadPickerService,
            IDocumentScanService documentScanService,
            IPhotoUploadPickerService photoUploadPickerService,
            IVideoUploadPickerService videoUploadPickerService,
            IUploadDestinationPickerPageService uploadDestinationPickerPageService,
            IFileInteractionService fileInteractionService,
            IFilePreviewService filePreviewService,
            ICottonCloudShareLinkService cloudShareLinkService,
            ICottonAccountSessionService accountSessionService,
            ICottonRemotePushSessionRegistrationService remotePushRegistrationService,
            ICloudShareLinkInteractionService cloudShareLinkInteractionService,
            IFileThumbnailProvider thumbnailProvider,
            CottonCloudToDeviceSyncRootSetupService cloudToDeviceSyncRootSetupService,
            CottonDeviceToCloudSyncRootSetupService deviceToCloudSyncRootSetupService,
            CottonBidirectionalSyncRootSetupService bidirectionalSyncRootSetupService,
            ICottonSyncLocalRootPickerService syncLocalRootPickerService,
            CottonCloudToDeviceSyncCoordinator cloudToDeviceSyncCoordinator,
            CottonDeviceToCloudSyncCoordinator deviceToCloudSyncCoordinator,
            CottonBidirectionalSyncCoordinator bidirectionalSyncCoordinator,
            INetworkAccessService networkAccess,
            IApplicationForegroundService foregroundService,
            IDeviceStorageSpaceService deviceStorageSpaceService,
            ILogger<MainPageFileBrowserController> fileBrowserLogger,
            IMainPagePresentationService presentationService,
            ILogger<MainPageViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(sessionService);
            ArgumentNullException.ThrowIfNull(instanceStore);
            ArgumentNullException.ThrowIfNull(profileCacheStore);
            ArgumentNullException.ThrowIfNull(browser);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(feedbackService);
            ArgumentNullException.ThrowIfNull(diagnosticsPageService);
            ArgumentNullException.ThrowIfNull(storageManagementService);
            ArgumentNullException.ThrowIfNull(logoutCleanupSettingsStore);
            ArgumentNullException.ThrowIfNull(storageSettingsPageService);
            ArgumentNullException.ThrowIfNull(syncSettingsPageService);
            ArgumentNullException.ThrowIfNull(securitySettingsPageService);
            ArgumentNullException.ThrowIfNull(transfersPageService);
            ArgumentNullException.ThrowIfNull(backupSetupPageService);
            ArgumentNullException.ThrowIfNull(captureInboxPageService);
            ArgumentNullException.ThrowIfNull(notificationSettingsPageService);
            ArgumentNullException.ThrowIfNull(activityFeedPageService);
            ArgumentNullException.ThrowIfNull(shareLaunchState);
            ArgumentNullException.ThrowIfNull(notificationLaunchState);
            ArgumentNullException.ThrowIfNull(transferQueueRestoreCoordinator);
            ArgumentNullException.ThrowIfNull(backgroundTransferCoordinator);
            ArgumentNullException.ThrowIfNull(backgroundSyncCoordinator);
            ArgumentNullException.ThrowIfNull(androidApiLevelProvider);
            ArgumentNullException.ThrowIfNull(transferActivitySignal);
            ArgumentNullException.ThrowIfNull(screenReader);
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(fileUploadService);
            ArgumentNullException.ThrowIfNull(folderContentCache);
            ArgumentNullException.ThrowIfNull(offlineFilePinStore);
            ArgumentNullException.ThrowIfNull(fileBrowserPreferenceStore);
            ArgumentNullException.ThrowIfNull(fileUploadPickerService);
            ArgumentNullException.ThrowIfNull(documentScanService);
            ArgumentNullException.ThrowIfNull(photoUploadPickerService);
            ArgumentNullException.ThrowIfNull(videoUploadPickerService);
            ArgumentNullException.ThrowIfNull(uploadDestinationPickerPageService);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(filePreviewService);
            ArgumentNullException.ThrowIfNull(cloudShareLinkService);
            ArgumentNullException.ThrowIfNull(accountSessionService);
            ArgumentNullException.ThrowIfNull(remotePushRegistrationService);
            ArgumentNullException.ThrowIfNull(cloudShareLinkInteractionService);
            ArgumentNullException.ThrowIfNull(thumbnailProvider);
            ArgumentNullException.ThrowIfNull(cloudToDeviceSyncRootSetupService);
            ArgumentNullException.ThrowIfNull(deviceToCloudSyncRootSetupService);
            ArgumentNullException.ThrowIfNull(bidirectionalSyncRootSetupService);
            ArgumentNullException.ThrowIfNull(syncLocalRootPickerService);
            ArgumentNullException.ThrowIfNull(cloudToDeviceSyncCoordinator);
            ArgumentNullException.ThrowIfNull(deviceToCloudSyncCoordinator);
            ArgumentNullException.ThrowIfNull(bidirectionalSyncCoordinator);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(foregroundService);
            ArgumentNullException.ThrowIfNull(deviceStorageSpaceService);
            ArgumentNullException.ThrowIfNull(fileBrowserLogger);
            ArgumentNullException.ThrowIfNull(presentationService);
            ArgumentNullException.ThrowIfNull(logger);

            _sessionService = sessionService;
            _instanceStore = instanceStore;
            _profileCacheStore = profileCacheStore;
            _browser = browser;
            _options = options;
            _dialogService = dialogService;
            _feedbackService = feedbackService;
            _diagnosticsPageService = diagnosticsPageService;
            _storageManagementService = storageManagementService;
            _logoutCleanupSettingsStore = logoutCleanupSettingsStore;
            _storageSettingsPageService = storageSettingsPageService;
            _syncSettingsPageService = syncSettingsPageService;
            _securitySettingsPageService = securitySettingsPageService;
            _transfersPageService = transfersPageService;
            _backupSetupPageService = backupSetupPageService;
            _captureInboxPageService = captureInboxPageService;
            _notificationSettingsPageService = notificationSettingsPageService;
            _activityFeedPageService = activityFeedPageService;
            _shareLaunchState = shareLaunchState;
            _notificationLaunchState = notificationLaunchState;
            _transferQueueRestoreCoordinator = transferQueueRestoreCoordinator;
            _backgroundTransferCoordinator = backgroundTransferCoordinator;
            _backgroundSyncCoordinator = backgroundSyncCoordinator;
            _androidApiLevelProvider = androidApiLevelProvider;
            _transferActivitySignal = transferActivitySignal;
            _screenReader = screenReader;
            _networkAccess = networkAccess;
            _foregroundService = foregroundService;
            _cloudShareLinkService = cloudShareLinkService;
            _accountSessionService = accountSessionService;
            _remotePushRegistrationService = remotePushRegistrationService;
            _cloudToDeviceSyncCoordinator = cloudToDeviceSyncCoordinator;
            _presentationService = presentationService;
            _logger = logger;

            Display = new MainPageDisplayState(options.DefaultInstanceUrl);
            _fileBrowser = new MainPageFileBrowserController(
                Display,
                fileBrowserService,
                fileUploadService,
                folderContentCache,
                offlineFilePinStore,
                fileBrowserPreferenceStore,
                fileUploadPickerService,
                documentScanService,
                photoUploadPickerService,
                videoUploadPickerService,
                uploadDestinationPickerPageService,
                fileInteractionService,
                filePreviewService,
                cloudShareLinkService,
                cloudShareLinkInteractionService,
                thumbnailProvider,
                cloudToDeviceSyncRootSetupService,
                deviceToCloudSyncRootSetupService,
                bidirectionalSyncRootSetupService,
                syncLocalRootPickerService,
                cloudToDeviceSyncCoordinator,
                deviceToCloudSyncCoordinator,
                bidirectionalSyncCoordinator,
                networkAccess,
                foregroundService,
                deviceStorageSpaceService,
                dialogService,
                this,
                fileBrowserLogger);
            _storageManagementService.DownloadedFilesCleared += StorageManagementService_DownloadedFilesCleared;
            _shareLaunchState.ShareStaged += ShareLaunchState_ShareStaged;
            _notificationLaunchState.NotificationLaunchRequested += NotificationLaunchState_NotificationLaunchRequested;
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
                () => Display.CanRefreshFiles);
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
            BeginFileSelectionCommand = new AsyncCommand<CottonFileBrowserEntry>(
                _fileBrowser.BeginEntrySelectionAsync,
                LogUnhandledCommandException,
                _ => Display.IsFileBrowserChromeEnabled);
            ClearFileSelectionCommand = new AsyncCommand(
                _fileBrowser.ClearSelectionAsync,
                LogUnhandledCommandException,
                () => Display.IsFileSelectionActive);
            ShowFileSelectionActionsCommand = new AsyncCommand(
                _fileBrowser.ShowSelectionActionsAsync,
                LogUnhandledCommandException,
                () => Display.IsFileSelectionActive);
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
            OpenCaptureInboxCommand = new AsyncCommand(
                OpenCaptureInboxAsync,
                LogUnhandledCommandException,
                () => Display.IsProfileVisible);
            OpenBackupSetupCommand = new AsyncCommand(
                OpenBackupSetupAsync,
                LogUnhandledCommandException,
                () => Display.IsProfileVisible);
            OpenSyncSettingsCommand = new AsyncCommand(
                OpenSyncAsync,
                LogUnhandledCommandException,
                () => Display.IsProfileVisible);
            OpenNotificationsCommand = new AsyncCommand(
                OpenNotificationsAsync,
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

        public AsyncCommand<CottonFileBrowserEntry> BeginFileSelectionCommand { get; }

        public AsyncCommand ClearFileSelectionCommand { get; }

        public AsyncCommand ShowFileSelectionActionsCommand { get; }

        public AsyncCommand ShowFileAddActionsCommand { get; }

        public AsyncCommand CancelFileActionCommand { get; }

        public AsyncCommand RetryFileActionCommand { get; }

        public AsyncCommand ToggleFileSearchCommand { get; }

        public AsyncCommand ShowFileViewActionsCommand { get; }

        public AsyncCommand ShowFileSortActionsCommand { get; }

        public AsyncCommand OpenTransfersCommand { get; }

        public AsyncCommand OpenCaptureInboxCommand { get; }

        public AsyncCommand OpenBackupSetupCommand { get; }

        public AsyncCommand OpenSyncSettingsCommand { get; }

        public AsyncCommand OpenNotificationsCommand { get; }

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
            ShowTransferActivityIndicators(restoredTransfers);
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
                    if (await TryShowCachedOfflineSessionAsync())
                    {
                        return;
                    }

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
                QueuePendingNotificationOpen("session restore finished");
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

        private async Task<bool> TryShowCachedOfflineSessionAsync()
        {
            try
            {
                Uri? instanceUri = await _instanceStore.GetAsync();
                if (instanceUri is null || !await _fileBrowser.HasCachedRootAsync(instanceUri))
                {
                    return false;
                }

                MainPageProfile profile =
                    await _profileCacheStore.GetAsync(instanceUri)
                    ?? CreateFallbackOfflineProfile(instanceUri);

                Display.InstanceUrl = instanceUri.AbsoluteUri;
                Display.ShowProfile(profile);
                Display.ShowProfileStatus(OfflineCachedSessionStatus);
                _isShowingCachedOfflineSession = true;

                IReadOnlyList<CottonTransferQueueItem> restoredTransfers =
                    await RestoreTransferQueueBestEffortAsync(instanceUri);
                ShowTransferActivityIndicators(restoredTransfers);

                if (!await _fileBrowser.InitializeCachedRootAsync(instanceUri))
                {
                    _fileBrowser.Clear();
                    _isShowingCachedOfflineSession = false;
                    return false;
                }

                RefreshCommands();
                QueuePendingCaptureInboxOpen("cached offline session");
                QueuePendingNotificationOpen("cached offline session");
                AnnounceStatus(OfflineCachedSessionStatus);
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to show Cotton mobile cached offline session.");
                _fileBrowser.Clear();
                _isShowingCachedOfflineSession = false;
                return false;
            }
        }

        private static MainPageProfile CreateFallbackOfflineProfile(Uri instanceUri)
        {
            string authority = instanceUri.IsDefaultPort
                ? instanceUri.Host
                : instanceUri.Authority;
            string path = instanceUri.AbsolutePath.TrimEnd('/');
            string instance = string.IsNullOrWhiteSpace(path) || string.Equals(path, "/", StringComparison.Ordinal)
                ? authority
                : $"{authority}{path}";
            return new MainPageProfile("Saved session", null, instance);
        }

        private async Task SaveCachedProfileBestEffortAsync(Uri instanceUri, MainPageProfile profile)
        {
            try
            {
                await _profileCacheStore.SaveAsync(instanceUri, profile);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save Cotton mobile cached profile.");
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
            CancelCloudToDeviceSyncRestore();
            ShowLoading("Signing out...");

            try
            {
                Uri? instanceUri = ResolveInstanceUri();
                if (instanceUri is not null)
                {
                    await _remotePushRegistrationService.RevokeCurrentSessionBestEffortAsync(instanceUri);
                }

                await _sessionService.LogoutAsync();
                if (await ShouldClearCachedFilesOnLogoutAsync())
                {
                    await ClearCachedSensitiveStateAsync("logout");
                }

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

        public async Task RevokeCurrentSessionAsync(
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

            ClearSessionRestoreRetry();
            _fileBrowser.CancelActiveWork();
            CancelCloudToDeviceSyncRestore();
            ShowLoading("Revoking session...");

            try
            {
                Uri? instanceUri = ResolveInstanceUri();
                if (instanceUri is null)
                {
                    throw new InvalidOperationException("A signed-in Cotton instance is required.");
                }

                await _remotePushRegistrationService.RevokeCurrentSessionBestEffortAsync(
                    instanceUri,
                    cancellationToken);
                await _accountSessionService.RevokeSessionAsync(
                    instanceUri,
                    sessionId,
                    cancellationToken);
                await ClearLocalSessionAndProfileAsync("current session revocation");
                if (await ShouldClearCachedFilesOnLogoutAsync())
                {
                    await ClearCachedSensitiveStateAsync("current session revocation");
                }

                _fileBrowser.Clear();
                Display.InstanceUrl = _options.DefaultInstanceUrl;
                ShowSignIn("Current session revoked. Sign in again.");
                await ReturnToSignedOutRootAsync();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Cotton mobile current session revocation failed.");
                ShowProfileError("Session revoke failed. Try again.");
                throw;
            }
        }

        private async Task<bool> ShouldClearCachedFilesOnLogoutAsync()
        {
            CottonLogoutCacheCleanupSettings settings =
                await GetLogoutCacheCleanupSettingsAsync("logout");
            return settings.ClearCachedFilesOnLogout;
        }

        private async Task<CottonLogoutCacheCleanupSettings> GetLogoutCacheCleanupSettingsAsync(string reason)
        {
            try
            {
                return await _logoutCleanupSettingsStore.GetAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to inspect Cotton mobile logout cache cleanup settings during {Reason}.",
                    reason);
                return CottonLogoutCacheCleanupSettings.Default;
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
                AccountStorageAction,
                AccountSyncAction,
                AccountSecurityAction,
                AccountNotificationsAction,
                AccountActivityAction,
                AccountResetFileLinksAction,
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
                case AccountStorageAction:
                    await OpenStorageAsync();
                    break;
                case AccountSyncAction:
                    await OpenSyncAsync();
                    break;
                case AccountSecurityAction:
                    await OpenSecurityAsync();
                    break;
                case AccountNotificationsAction:
                    await OpenNotificationsAsync();
                    break;
                case AccountActivityAction:
                    await OpenActivityAsync();
                    break;
                case AccountResetFileLinksAction:
                    await ResetFileLinksAsync(profileName, profileEmail, profileInstance, instanceUrl);
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

            CottonLogoutCacheCleanupSettings logoutCleanupSettings =
                await GetLogoutCacheCleanupSettingsAsync("logout confirmation");
            bool confirmed = await _dialogService.ShowConfirmationAsync(
                LogoutConfirmationTitle,
                logoutCleanupSettings.ClearCachedFilesOnLogout
                    ? LogoutConfirmationClearCacheMessage
                    : LogoutConfirmationKeepCacheMessage,
                AccountLogoutAction,
                AccountCancelAction);
            if (!confirmed)
            {
                return;
            }

            await LogoutAsync();
        }

        private async Task ResetFileLinksAsync(
            string profileName,
            string? profileEmail,
            string profileInstance,
            string instanceUrl)
        {
            if (!IsSameProfileContext(profileName, profileEmail, profileInstance, instanceUrl))
            {
                return;
            }

            Uri? instanceUri = CottonServerUrl.NormalizeOptional(instanceUrl);
            if (instanceUri is null || !CottonInstanceUri.IsSupported(instanceUri))
            {
                ShowProfileError(CottonCloudShareLinkStatusText.ResetFileLinksUnavailableStatus);
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                ShowProfileError(CottonCloudShareLinkStatusText.ResetFileLinksOfflineUnavailableStatus);
                return;
            }

            bool confirmed = await _dialogService.ShowConfirmationAsync(
                ResetFileLinksConfirmationTitle,
                ResetFileLinksConfirmationMessage,
                AccountResetFileLinksConfirmationAction,
                AccountCancelAction);
            if (!IsSameProfileContext(profileName, profileEmail, profileInstance, instanceUrl))
            {
                return;
            }

            if (!confirmed)
            {
                ShowProfileStatus(CottonCloudShareLinkStatusText.ResetFileLinksCancelledStatus);
                return;
            }

            ShowProfileStatus(CottonCloudShareLinkStatusText.ResetFileLinksInProgressStatus);

            try
            {
                await _cloudShareLinkService.InvalidateAllFileLinksAsync(instanceUri);
                if (!IsSameProfileContext(profileName, profileEmail, profileInstance, instanceUrl))
                {
                    return;
                }

                ShowProfileStatus(CottonCloudShareLinkStatusText.ResetFileLinksCompletedStatus);
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsSameProfileContext(profileName, profileEmail, profileInstance, instanceUrl))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile share-link reset authorization failure.");
                    return;
                }

                _logger.LogWarning(exception, "Cotton mobile share-link reset session expired.");
                await HandleFileBrowserSessionExpiredAsync(instanceUri);
            }
            catch (OperationCanceledException exception)
            {
                if (!IsSameProfileContext(profileName, profileEmail, profileInstance, instanceUrl))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile share-link reset cancellation.");
                    return;
                }

                ShowProfileStatus(CottonCloudShareLinkStatusText.ResetFileLinksCancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsSameProfileContext(profileName, profileEmail, profileInstance, instanceUrl))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile share-link reset failure.");
                    return;
                }

                _logger.LogError(exception, "Failed to reset Cotton mobile share links.");
                HttpStatusCode? statusCode = exception is CottonApiException apiException
                    ? apiException.StatusCode
                    : null;
                ShowProfileError(CottonCloudShareLinkStatusText.CreateResetFileLinksFailedStatus(
                    statusCode,
                    _networkAccess.HasInternetAccess));
            }
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
            QueueCloudToDeviceSyncRestoreForCurrentSession("internet access restored");

            if (!_shouldRetrySessionRestoreWhenOnline && !_shouldRetrySessionRestoreOnResume)
            {
                return;
            }

            QueueSessionRestoreRetry("internet access restored");
        }

        private void ForegroundService_Resumed(object? sender, EventArgs e)
        {
            QueueCloudToDeviceSyncRestoreForCurrentSession("foreground resume");

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
                    || (!Display.IsSignInVisible && !_isShowingCachedOfflineSession)
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
            await ClearLocalSessionAndProfileAsync(reason);
            await ClearCachedSensitiveStateAsync(reason);
        }

        private async Task ClearLocalSessionAndProfileAsync(string reason)
        {
            CancelCloudToDeviceSyncRestore();

            try
            {
                await _sessionService.ClearLocalSessionAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to clear Cotton mobile local session during {Reason}.", reason);
            }

            try
            {
                await _profileCacheStore.ClearAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to clear Cotton mobile cached profile during {Reason}.", reason);
            }
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

        private async Task OpenSyncAsync()
        {
            Uri? instanceUri = ResolveInstanceUri();
            if (instanceUri is null)
            {
                await _dialogService.ShowAlertAsync(
                    "Sync",
                    "Could not inspect sync folders for this instance.",
                    "OK");
                return;
            }

            try
            {
                await _syncSettingsPageService.OpenAsync(instanceUri);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton mobile sync page.");
                await _dialogService.ShowAlertAsync(
                    "Sync",
                    "Could not inspect sync folders.",
                    "OK");
            }
        }

        private async Task OpenSecurityAsync()
        {
            try
            {
                await _securitySettingsPageService.OpenAsync(this);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton mobile security page.");
                await _dialogService.ShowAlertAsync(
                    "Security",
                    "Could not inspect security settings.",
                    "OK");
            }
        }

        private async Task ReturnToSignedOutRootAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var navigation = Shell.Current?.Navigation;
                    if (navigation is not null && navigation.NavigationStack.Count > 1)
                    {
                        await navigation.PopToRootAsync();
                    }
                });
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to return Cotton mobile navigation to the signed-out root.");
            }
        }

        private async Task OpenNotificationsAsync()
        {
            await TryOpenNotificationsAsync(showAlertOnFailure: true);
        }

        private async Task OpenActivityAsync()
        {
            Uri? instanceUri = ResolveInstanceUri();
            if (instanceUri is null)
            {
                await _dialogService.ShowAlertAsync(
                    "Activity",
                    "Could not open activity for this instance.",
                    "OK");
                return;
            }

            try
            {
                await _activityFeedPageService.OpenAsync(instanceUri);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton mobile activity page.");
                await _dialogService.ShowAlertAsync(
                    "Activity",
                    "Could not load activity.",
                    "OK");
            }
        }

        private async Task<bool> TryOpenNotificationsAsync(bool showAlertOnFailure)
        {
            try
            {
                await _notificationSettingsPageService.OpenAsync();
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton mobile notifications page.");
                if (showAlertOnFailure)
                {
                    await _dialogService.ShowAlertAsync(
                        "Notifications",
                        "Could not inspect notifications.",
                        "OK");
                }

                return false;
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

        private void NotificationLaunchState_NotificationLaunchRequested(object? sender, EventArgs e)
        {
            QueuePendingNotificationOpen("notification launch");
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

        private void QueuePendingNotificationOpen(string reason)
        {
            if (_isNotificationOpenQueued || _notificationLaunchState.PendingNotificationLaunchCount == 0)
            {
                return;
            }

            _isNotificationOpenQueued = true;
            _ = RunQueuedNotificationOpenAsync(reason);
        }

        private async Task RunQueuedNotificationOpenAsync(string reason)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => OpenPendingNotificationsAfterLaunchAsync(reason));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to queue Cotton mobile Notifications open after {Reason}.",
                    reason);
                _isNotificationOpenQueued = false;
            }
        }

        private async Task OpenPendingNotificationsAfterLaunchAsync(string reason)
        {
            try
            {
                if (_isSessionRestoreInProgress || !Display.IsProfileVisible)
                {
                    return;
                }

                if (_notificationLaunchState.PendingNotificationLaunchCount == 0)
                {
                    return;
                }

                _logger.LogInformation("Opening Cotton mobile Notifications after {Reason}.", reason);
                if (await TryOpenNotificationsAsync(showAlertOnFailure: false))
                {
                    _notificationLaunchState.ClearPendingNotificationLaunches();
                }
            }
            finally
            {
                _isNotificationOpenQueued = false;
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
                _isShowingCachedOfflineSession = false;
                ClearSessionRestoreRetry();
                MainPageProfile profile = _presentationService.CreateProfile(result.InstanceUri, result.User);
                await SaveCachedProfileBestEffortAsync(result.InstanceUri, profile);
                ShowProfile(profile);
                IReadOnlyList<CottonTransferQueueItem> restoredTransfers =
                    await RestoreTransferQueueBestEffortAsync(result.InstanceUri);
                ShowTransferActivityIndicators(restoredTransfers);
                await ResumeQueuedBackgroundTransferBestEffortAsync(result.InstanceUri);
                await _remotePushRegistrationService.RegisterCurrentSessionBestEffortAsync(result.InstanceUri);
                string? accountScopeKey = CottonAccountScopeKey.TryCreateFromUsername(
                    result.User.Username,
                    out string resolvedAccountScopeKey)
                        ? resolvedAccountScopeKey
                        : null;
                await _fileBrowser.InitializeAsync(result.InstanceUri, accountScopeKey);
                QueueCloudToDeviceSyncRestore(result.InstanceUri, "authenticated session");
                _ = ScheduleCloudToDeviceBackgroundSyncBestEffortAsync(result.InstanceUri, "authenticated session");
                RefreshCommands();
                QueuePendingCaptureInboxOpen("authenticated session");
                QueuePendingNotificationOpen("authenticated session");
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

        private void ShowTransferActivityIndicators(IReadOnlyList<CottonTransferQueueItem> transfers)
        {
            Display.ShowTransferActivity(CottonTransferActivityIndicator.Create(transfers));
            Display.ShowBackupActivity(CottonCameraBackupActivityIndicator.Create(transfers));
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

        private void QueueCloudToDeviceSyncRestoreForCurrentSession(string reason)
        {
            if (!Display.IsProfileVisible || _isShowingCachedOfflineSession)
            {
                return;
            }

            Uri? instanceUri = ResolveInstanceUri();
            if (instanceUri is null)
            {
                return;
            }

            QueueCloudToDeviceSyncRestore(instanceUri, reason);
            _ = ScheduleCloudToDeviceBackgroundSyncBestEffortAsync(instanceUri, reason);
        }

        private void QueueCloudToDeviceSyncRestore(Uri instanceUri, string reason)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            if (!_networkAccess.HasInternetAccess || _isCloudToDeviceSyncRestoreInProgress)
            {
                return;
            }

            var cancellation = new CancellationTokenSource();
            _cloudToDeviceSyncRestoreCancellation = cancellation;
            _isCloudToDeviceSyncRestoreInProgress = true;
            _ = RunCloudToDeviceSyncRestoreBestEffortAsync(instanceUri, reason, cancellation);
        }

        private async Task RunCloudToDeviceSyncRestoreBestEffortAsync(
            Uri instanceUri,
            string reason,
            CancellationTokenSource cancellation)
        {
            try
            {
                CottonCloudToDeviceSyncRunSummary summary =
                    await _cloudToDeviceSyncCoordinator.RunAsync(instanceUri, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();

                if (summary.RootCount == 0)
                {
                    return;
                }

                _logger.LogInformation(
                    "Restored Cotton mobile cloud-to-device sync roots after {Reason}: {RootCount} roots, {CompletedRootCount} completed, {SkippedRootCount} skipped, {DownloadedCount} downloaded, {RefreshedCount} refreshed, {RenamedCount} renamed, {RemovedCount} removed, {BlockedItemCount} blocked.",
                    reason,
                    summary.RootCount,
                    summary.CompletedRootCount,
                    summary.SkippedRootCount,
                    summary.DownloadedCount,
                    summary.RefreshedCount,
                    summary.RenamedCount,
                    summary.RemovedCount,
                    summary.BlockedItemCount);
                if (summary.HasAppliedChanges)
                {
                    _fileBrowser.RefreshLocalFileMarkersAfterStorageChange();
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                _logger.LogDebug(
                    "Cancelled Cotton mobile cloud-to-device sync restore after {Reason}.",
                    reason);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to restore Cotton mobile cloud-to-device sync roots after {Reason}.",
                    reason);
            }
            finally
            {
                if (ReferenceEquals(_cloudToDeviceSyncRestoreCancellation, cancellation))
                {
                    _cloudToDeviceSyncRestoreCancellation = null;
                    _isCloudToDeviceSyncRestoreInProgress = false;
                }

                cancellation.Dispose();
            }
        }

        private async Task ScheduleCloudToDeviceBackgroundSyncBestEffortAsync(Uri instanceUri, string reason)
        {
            try
            {
                CottonAndroidBackgroundSyncScheduleResult result =
                    await _backgroundSyncCoordinator.ScheduleAsync(instanceUri);
                if (result.IsScheduled)
                {
                    _logger.LogInformation(
                        "Scheduled Cotton mobile cloud-to-device background sync after {Reason}: {StatusText}",
                        reason,
                        result.StatusText);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to schedule Cotton mobile cloud-to-device background sync after {Reason}.",
                    reason);
            }
        }

        private void CancelCloudToDeviceSyncRestore()
        {
            CancellationTokenSource? cancellation = _cloudToDeviceSyncRestoreCancellation;
            if (cancellation is null)
            {
                _isCloudToDeviceSyncRestoreInProgress = false;
                return;
            }

            _cloudToDeviceSyncRestoreCancellation = null;
            _isCloudToDeviceSyncRestoreInProgress = false;
            cancellation.Cancel();
        }

        private void ShowLoading(string message)
        {
            Display.ShowLoading(message);
            RefreshCommands();
            _screenReader.Announce(message);
        }

        private void ShowSignIn(string? status)
        {
            CancelCloudToDeviceSyncRestore();
            _isShowingCachedOfflineSession = false;
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
            _isShowingCachedOfflineSession = false;
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

        private void ShowProfileStatus(string status)
        {
            Display.ShowProfileStatus(status);
            RefreshCommands();
            _screenReader.Announce(status);
        }

        private static bool IsAuthorizationFailure(Exception exception)
        {
            return exception is CottonApiException
            {
                StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            };
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
            BeginFileSelectionCommand.RaiseCanExecuteChanged();
            ClearFileSelectionCommand.RaiseCanExecuteChanged();
            ShowFileSelectionActionsCommand.RaiseCanExecuteChanged();
            ShowFileAddActionsCommand.RaiseCanExecuteChanged();
            CancelFileActionCommand.RaiseCanExecuteChanged();
            RetryFileActionCommand.RaiseCanExecuteChanged();
            ToggleFileSearchCommand.RaiseCanExecuteChanged();
            ShowFileViewActionsCommand.RaiseCanExecuteChanged();
            ShowFileSortActionsCommand.RaiseCanExecuteChanged();
            OpenTransfersCommand.RaiseCanExecuteChanged();
            OpenCaptureInboxCommand.RaiseCanExecuteChanged();
            OpenBackupSetupCommand.RaiseCanExecuteChanged();
            OpenSyncSettingsCommand.RaiseCanExecuteChanged();
            OpenNotificationsCommand.RaiseCanExecuteChanged();
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
                    OpenCaptureInboxCommand.RaiseCanExecuteChanged();
                    OpenBackupSetupCommand.RaiseCanExecuteChanged();
                    OpenSyncSettingsCommand.RaiseCanExecuteChanged();
                    OpenNotificationsCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.IsFileUpButtonEnabled):
                    NavigateFilesUpCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.CanRefreshFiles):
                    RefreshFilesCommand.RaiseCanExecuteChanged();
                    break;
                case nameof(MainPageDisplayState.IsFileBrowserChromeEnabled):
                    RefreshFilesCommand.RaiseCanExecuteChanged();
                    ActivateFileBrowserEntryCommand.RaiseCanExecuteChanged();
                    ShowFileBrowserEntryActionsCommand.RaiseCanExecuteChanged();
                    BeginFileSelectionCommand.RaiseCanExecuteChanged();
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
                case nameof(MainPageDisplayState.IsFileSelectionActive):
                    ClearFileSelectionCommand.RaiseCanExecuteChanged();
                    ShowFileSelectionActionsCommand.RaiseCanExecuteChanged();
                    ShowFileAddActionsCommand.RaiseCanExecuteChanged();
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
