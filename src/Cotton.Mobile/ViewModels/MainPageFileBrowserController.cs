using Cotton.Mobile.Services;
using Cotton.Sdk;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Cotton.Mobile.ViewModels
{
    public class MainPageFileBrowserController
    {
        private const string CancelAction = "Cancel";
        private const string CopyLinkAction = "Copy link";
        private const string DetailsAction = "Details";
        private const string DoneAction = "Done";
        private const string DownloadAction = "Download";
        private const string DownloadAgainAction = "Download again";
        private const string KeepOfflineAction = "Keep offline";
        private const string LinkExpirationTitle = "Link expires";
        private const string OpenAction = CottonFileOpenRouter.OpenActionLabel;
        private const string RefreshOfflineAction = "Refresh offline";
        private const string RemoveOfflineAction = "Remove offline";
        private const string ShareFileAction = "Share file";
        private const string ShareLinkAction = "Share link";
        private const string SyncFromSelectedFolderAction = CottonDeviceToCloudSyncStatusText.ActionLabel;
        private const string SyncBothWaysAction = CottonBidirectionalSyncStatusText.ActionLabel;
        private const string SyncToDeviceAction = CottonCloudToDeviceSyncStatusText.ActionLabel;
        private const string SyncToSelectedFolderAction = CottonCloudToDeviceSyncStatusText.ChooseFolderActionLabel;
        private const string NewFolderPromptTitle = "New folder";
        private const string NewFolderPromptMessage = "Folder name";
        private const string NewFolderCreateAction = "Create";
        private const string SortNameAction = "Name";
        private const string SortUpdatedAction = "Updated";
        private const string SortSizeAction = "Size";
        private const string SortTypeAction = "Type";
        private const string ViewListAction = "List";
        private const string ViewTilesAction = "Tiles";
        private const string CurrentActionPrefix = "✓ ";
        private const string OfflineBrowseStatus = "Offline. Files marked On device can still open.";
        private const string OfflineDownloadStatus = "Offline. Download needs internet.";
        private const string OfflineOpenStatus = "Offline. This file is not available on device.";
        private const string OfflineShareStatus = "Offline. This file is not available on device.";
        private const string OfflineUploadStatus = "Offline. Upload needs internet.";
        private const string OpenUnavailableStatus = CottonFileOpenRouter.OpenUnavailableStatus;
        private const int PayloadTooLargeStatusCode = 413;
        private const int InsufficientStorageStatusCode = 507;
        private static readonly TimeSpan SelectionClearActivationSettleDuration = TimeSpan.FromMilliseconds(350);

        private readonly MainPageDisplayState _display;
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly ICottonFileUploadService _fileUploadService;
        private readonly ICottonFolderContentCache _folderContentCache;
        private readonly ICottonOfflineFilePinStore _offlineFilePinStore;
        private readonly IFileBrowserPreferenceStore _preferenceStore;
        private readonly IFileUploadPickerService _fileUploadPickerService;
        private readonly IDocumentScanService _documentScanService;
        private readonly IPhotoUploadPickerService _photoUploadPickerService;
        private readonly IVideoUploadPickerService _videoUploadPickerService;
        private readonly IUploadDestinationPickerPageService _uploadDestinationPickerPageService;
        private readonly IFileInteractionService _fileInteractionService;
        private readonly IFilePreviewService _filePreviewService;
        private readonly ICottonCloudShareLinkService _cloudShareLinkService;
        private readonly ICloudShareLinkInteractionService _cloudShareLinkInteractionService;
        private readonly IFileThumbnailProvider _thumbnailProvider;
        private readonly CottonCloudToDeviceSyncRootSetupService _cloudToDeviceSyncRootSetupService;
        private readonly CottonDeviceToCloudSyncRootSetupService _deviceToCloudSyncRootSetupService;
        private readonly CottonBidirectionalSyncRootSetupService _bidirectionalSyncRootSetupService;
        private readonly ICottonSyncLocalRootPickerService _syncLocalRootPickerService;
        private readonly CottonCloudToDeviceSyncCoordinator _cloudToDeviceSyncCoordinator;
        private readonly CottonDeviceToCloudSyncCoordinator _deviceToCloudSyncCoordinator;
        private readonly CottonBidirectionalSyncCoordinator _bidirectionalSyncCoordinator;
        private readonly INetworkAccessService _networkAccess;
        private readonly IApplicationForegroundService _foregroundService;
        private readonly IDeviceStorageSpaceService _deviceStorageSpaceService;
        private readonly IUserDialogService _dialogService;
        private readonly IFileBrowserSessionHandler _sessionHandler;
        private readonly ILogger<MainPageFileBrowserController> _logger;
        private readonly List<CottonFolderHandle> _fileNavigation = [];

        private CancellationTokenSource? _fileActionCancellation;
        private CancellationTokenSource? _fileLoadCancellation;
        private MainPageFileAction? _retryFileAction;
        private CottonFileBrowserEntry? _retryFileActionEntry;
        private CottonFolderHandle? _currentFolder;
        private Uri? _instanceUri;
        private string? _accountScopeKey;
        private bool _isFileLoadInProgress;
        private bool _isFolderNavigationInProgress;
        private bool _lastFileLoadFailed;
        private bool _lastFileLoadDisplayedCachedContent;
        private bool _isRecoveryRefreshInProgress;
        private string? _fileLoadRecoveryPendingAfterBusyReason;
        private DateTimeOffset _ignoreEntryInteractionUntil = DateTimeOffset.MinValue;

        public MainPageFileBrowserController(
            MainPageDisplayState display,
            ICottonFileBrowserService fileBrowserService,
            ICottonFileUploadService fileUploadService,
            ICottonFolderContentCache folderContentCache,
            ICottonOfflineFilePinStore offlineFilePinStore,
            IFileBrowserPreferenceStore preferenceStore,
            IFileUploadPickerService fileUploadPickerService,
            IDocumentScanService documentScanService,
            IPhotoUploadPickerService photoUploadPickerService,
            IVideoUploadPickerService videoUploadPickerService,
            IUploadDestinationPickerPageService uploadDestinationPickerPageService,
            IFileInteractionService fileInteractionService,
            IFilePreviewService filePreviewService,
            ICottonCloudShareLinkService cloudShareLinkService,
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
            IUserDialogService dialogService,
            IFileBrowserSessionHandler sessionHandler,
            ILogger<MainPageFileBrowserController> logger)
        {
            ArgumentNullException.ThrowIfNull(display);
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(fileUploadService);
            ArgumentNullException.ThrowIfNull(folderContentCache);
            ArgumentNullException.ThrowIfNull(offlineFilePinStore);
            ArgumentNullException.ThrowIfNull(preferenceStore);
            ArgumentNullException.ThrowIfNull(fileUploadPickerService);
            ArgumentNullException.ThrowIfNull(documentScanService);
            ArgumentNullException.ThrowIfNull(photoUploadPickerService);
            ArgumentNullException.ThrowIfNull(videoUploadPickerService);
            ArgumentNullException.ThrowIfNull(uploadDestinationPickerPageService);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(filePreviewService);
            ArgumentNullException.ThrowIfNull(cloudShareLinkService);
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
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(sessionHandler);
            ArgumentNullException.ThrowIfNull(logger);

            _display = display;
            _fileBrowserService = fileBrowserService;
            _fileUploadService = fileUploadService;
            _folderContentCache = folderContentCache;
            _offlineFilePinStore = offlineFilePinStore;
            _preferenceStore = preferenceStore;
            _fileUploadPickerService = fileUploadPickerService;
            _documentScanService = documentScanService;
            _photoUploadPickerService = photoUploadPickerService;
            _videoUploadPickerService = videoUploadPickerService;
            _uploadDestinationPickerPageService = uploadDestinationPickerPageService;
            _fileInteractionService = fileInteractionService;
            _filePreviewService = filePreviewService;
            _cloudShareLinkService = cloudShareLinkService;
            _cloudShareLinkInteractionService = cloudShareLinkInteractionService;
            _thumbnailProvider = thumbnailProvider;
            _cloudToDeviceSyncRootSetupService = cloudToDeviceSyncRootSetupService;
            _deviceToCloudSyncRootSetupService = deviceToCloudSyncRootSetupService;
            _bidirectionalSyncRootSetupService = bidirectionalSyncRootSetupService;
            _syncLocalRootPickerService = syncLocalRootPickerService;
            _cloudToDeviceSyncCoordinator = cloudToDeviceSyncCoordinator;
            _deviceToCloudSyncCoordinator = deviceToCloudSyncCoordinator;
            _bidirectionalSyncCoordinator = bidirectionalSyncCoordinator;
            _networkAccess = networkAccess;
            _foregroundService = foregroundService;
            _deviceStorageSpaceService = deviceStorageSpaceService;
            _dialogService = dialogService;
            _sessionHandler = sessionHandler;
            _logger = logger;
            _display.FileSearchTextChanged += Display_FileSearchTextChanged;
            _networkAccess.InternetAccessRestored += NetworkAccess_InternetAccessRestored;
            _foregroundService.Resumed += ForegroundService_Resumed;
        }

        public async Task InitializeAsync(Uri instanceUri, string? accountScopeKey)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            _instanceUri = instanceUri;
            _accountScopeKey = string.IsNullOrWhiteSpace(accountScopeKey) ? null : accountScopeKey.Trim();
            _display.ApplyFileBrowserPreferences(LoadFileBrowserPreferences());
            await LoadRootFilesAsync();
        }

        public async Task<bool> HasCachedRootAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            CottonCachedFolderContentSnapshot? cachedSnapshot =
                await _folderContentCache.LoadRootSnapshotAsync(instanceUri, cancellationToken);
            return cachedSnapshot is not null;
        }

        public async Task<bool> InitializeCachedRootAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            CottonCachedFolderContentSnapshot? cachedSnapshot =
                await _folderContentCache.LoadRootSnapshotAsync(instanceUri, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (cachedSnapshot is null)
            {
                return false;
            }

            _instanceUri = instanceUri;
            _accountScopeKey = null;
            _display.ApplyFileBrowserPreferences(LoadFileBrowserPreferences());

            CancellationTokenSource fileLoadCancellation = BeginFileLoad();
            try
            {
                _display.ClearFileSearch();
                return await ShowCachedRootFilesAsync(instanceUri, cachedSnapshot, fileLoadCancellation);
            }
            finally
            {
                EndFileLoad(fileLoadCancellation);
            }
        }

        private CottonFileBrowserPreferences LoadFileBrowserPreferences()
        {
            try
            {
                return _preferenceStore.Get();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile file browser preferences.");
                return new CottonFileBrowserPreferences(
                    CottonFileBrowserViewMode.List,
                    CottonFileBrowserSortMode.Name);
            }
        }

        public void Clear()
        {
            CancelActiveWork();
            _instanceUri = null;
            _accountScopeKey = null;
            _currentFolder = null;
            _lastFileLoadFailed = false;
            _lastFileLoadDisplayedCachedContent = false;
            _fileLoadRecoveryPendingAfterBusyReason = null;
            _fileNavigation.Clear();
        }

        public void CancelActiveWork()
        {
            CancelCurrentFileLoad();
            CancelCurrentFileAction();
            ClearFileActionRetry();
            _isFileLoadInProgress = false;
            _isFolderNavigationInProgress = false;
            _lastFileLoadDisplayedCachedContent = false;
            _fileLoadRecoveryPendingAfterBusyReason = null;
        }

        public async Task RefreshAsync()
        {
            ClearFileActionRetry();
            if (IsFileBrowserBusy())
            {
                StopExternalRefreshIfNoLoadInProgress();
                return;
            }

            if (_instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to load files.");
                return;
            }

            if (IsDisplayingRootFolder())
            {
                await LoadRootFilesAsync(isRefresh: true);
                return;
            }

            if (_currentFolder is null)
            {
                _display.ShowFilesStatus("Could not refresh this folder. Go up and try again.");
                return;
            }

            await LoadFolderAsync(_currentFolder, preserveHistory: true, isRefresh: true);
        }

        public void RefreshLocalFileMarkersAfterStorageChange()
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                return;
            }

            _ = RefreshLocalFileMarkersAfterStorageChangeAsync(instanceUri);
        }

        private async Task RefreshLocalFileMarkersAfterStorageChangeAsync(Uri instanceUri)
        {
            try
            {
                bool changed = await RefreshLocalFileStateAsync(instanceUri, CancellationToken.None);
                if (changed && !IsFileBrowserBusy())
                {
                    _display.ShowFilesStatus("On-device files updated.");
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to refresh Cotton mobile local file markers after storage change.");
            }
        }

        public async Task NavigateUpAsync()
        {
            ClearFileActionRetry();
            if (IsFileBrowserBusy())
            {
                return;
            }

            CottonFileNavigationUpPlan plan =
                CottonFileNavigationPlanner.CreateNavigateUpPlan(_currentFolder, _fileNavigation);
            if (!plan.CanNavigate)
            {
                return;
            }

            _isFolderNavigationInProgress = true;
            try
            {
                CottonFolderHandle? originalFolder = _currentFolder;
                var originalNavigation = new List<CottonFolderHandle>(_fileNavigation);
                string originalSearchText = _display.FileSearchText;
                bool originalSearchOpen = _display.IsFileSearchOpen;
                _fileNavigation.Clear();
                _fileNavigation.AddRange(plan.NavigationAfterNavigate);
                _display.ClearFileSearch();
                if (plan.IsRootTarget)
                {
                    await LoadRootFilesAsync();
                }
                else
                {
                    await LoadFolderAsync(plan.TargetFolder!, preserveHistory: true);
                }

                if (ShouldRestoreNavigationAfterFileLoadFailure()
                    || ShouldRestoreNavigationAfterUnchangedFolder(originalFolder, originalNavigation))
                {
                    RestoreNavigation(originalFolder, originalNavigation, originalSearchText, originalSearchOpen);
                }
            }
            finally
            {
                _isFolderNavigationInProgress = false;
                QueuePendingFileLoadRecoveryRefreshAfterBusy();
            }
        }

        public async Task ActivateEntryAsync(CottonFileBrowserEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            CottonFileBrowserEntry? currentEntry = GetCurrentVisibleEntry(entry);
            if (currentEntry is null)
            {
                return;
            }

            if (!_display.IsFileSelectionActive && ShouldIgnoreEntryInteraction())
            {
                return;
            }

            if (_display.IsFileSelectionActive)
            {
                _display.ToggleFileEntrySelection(currentEntry);
                return;
            }

            if (currentEntry.IsFolder)
            {
                ClearFileActionRetry();
                await OpenFolderAsync(currentEntry);
                return;
            }

            await OpenFileAsync(currentEntry);
        }

        public async Task ShowEntryActionsAsync(CottonFileBrowserEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            CottonFileBrowserEntry? currentEntry = GetCurrentVisibleEntry(entry);
            if (currentEntry is null)
            {
                return;
            }

            if (!_display.IsFileSelectionActive && ShouldIgnoreEntryInteraction())
            {
                return;
            }

            if (_display.IsFileSelectionActive)
            {
                _display.ToggleFileEntrySelection(currentEntry);
                return;
            }

            if (currentEntry.IsFolder)
            {
                ClearFileActionRetry();
                await ShowFolderActionsAsync(currentEntry);
                return;
            }

            await ShowFileActionsAsync(currentEntry);
        }

        public Task BeginEntrySelectionAsync(CottonFileBrowserEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            CottonFileBrowserEntry? currentEntry = GetCurrentVisibleEntry(entry);
            if (currentEntry is null || !_display.IsFileBrowserChromeEnabled)
            {
                return Task.CompletedTask;
            }

            _display.SelectFileEntry(currentEntry);
            _ignoreEntryInteractionUntil = DateTimeOffset.MinValue;
            return Task.CompletedTask;
        }

        public Task ClearSelectionAsync()
        {
            if (_display.IsFileSelectionActive)
            {
                _ignoreEntryInteractionUntil = DateTimeOffset.UtcNow.Add(SelectionClearActivationSettleDuration);
            }

            _display.ClearFileSelection();
            return Task.CompletedTask;
        }

        public async Task ShowSelectionActionsAsync()
        {
            ClearFileActionRetry();
            CottonFileSelectionSnapshot selection = _display.FileSelection;
            if (!selection.IsActive)
            {
                return;
            }

            CottonFileBulkActionSnapshot[] actions = selection.Actions
                .Where(IsSupportedSelectionAction)
                .Where(action => action.IsEnabled)
                .ToArray();
            if (actions.Length == 0)
            {
                _display.ShowFilesStatus("No actions available for this selection.");
                return;
            }

            string? selectedLabel = await _dialogService.ShowActionSheetAsync(
                selection.TitleText,
                CancelAction,
                null,
                actions.Select(action => action.Label).ToArray());
            CottonFileBulkActionSnapshot? selectedAction = actions.FirstOrDefault(
                action => string.Equals(action.Label, selectedLabel, StringComparison.Ordinal));
            if (selectedAction is null)
            {
                return;
            }

            CottonFileBrowserEntry[] entries = selection.Entries.ToArray();
            switch (selectedAction.Kind)
            {
                case CottonFileBulkActionKind.CopyLinks:
                    await CopySelectionCloudShareLinksAsync(entries);
                    break;
                case CottonFileBulkActionKind.ShareLinks:
                    await ShareSelectionCloudShareLinksAsync(entries);
                    break;
            }
        }

        private bool ShouldIgnoreEntryInteraction()
        {
            if (_ignoreEntryInteractionUntil <= DateTimeOffset.UtcNow)
            {
                _ignoreEntryInteractionUntil = DateTimeOffset.MinValue;
                return false;
            }

            return true;
        }

        private static bool IsSupportedSelectionAction(CottonFileBulkActionSnapshot action)
        {
            return action.Kind is CottonFileBulkActionKind.CopyLinks or CottonFileBulkActionKind.ShareLinks;
        }

        public async Task ShowViewActionsAsync()
        {
            Uri? instanceUri = GetActiveFileBrowserInstance();
            if (instanceUri is null)
            {
                return;
            }

            string listAction = CreateCurrentActionLabel(
                ViewListAction,
                _display.FileViewMode == CottonFileBrowserViewMode.List);
            string tilesAction = CreateCurrentActionLabel(
                ViewTilesAction,
                _display.FileViewMode == CottonFileBrowserViewMode.Tiles);
            string? action = await _dialogService.ShowActionSheetAsync(
                "View files as",
                CancelAction,
                null,
                listAction,
                tilesAction);

            if (!CanUseFileBrowserContext(instanceUri))
            {
                return;
            }

            switch (NormalizeAction(action))
            {
                case ViewListAction:
                    SetViewMode(CottonFileBrowserViewMode.List);
                    break;
                case ViewTilesAction:
                    SetViewMode(CottonFileBrowserViewMode.Tiles);
                    break;
            }
        }

        public Task ToggleFileSearchAsync()
        {
            ClearFileActionRetry();
            _display.ToggleFileSearch();
            return Task.CompletedTask;
        }

        public async Task ShowAddActionsAsync()
        {
            ClearFileActionRetry();
            Uri? instanceUri = GetActiveFileBrowserInstance();
            CottonFolderHandle? folder = _currentFolder;
            if (instanceUri is null || folder is null)
            {
                _display.ShowFilesStatus("Open a folder before uploading.");
                return;
            }

            string? action = await _dialogService.ShowActionSheetAsync(
                CottonFileAddActionSheet.CreateTitle(folder),
                CottonFileAddActionSheet.CancelAction,
                null,
                CottonFileAddActionSheet.CreateActions(_documentScanService.IsAvailable).ToArray());

            if (!CanUseFileBrowserContext(instanceUri) || !HasSameFolder(_currentFolder, folder))
            {
                return;
            }

            string? normalizedAction = NormalizeAction(action);
            if (normalizedAction == CottonFileAddActionSheet.NewFolderAction)
            {
                await CreateFolderAsync(instanceUri, folder);
            }
            else if (normalizedAction == CottonFileAddActionSheet.UploadFileAction)
            {
                await UploadPickedSourceAsync(
                    instanceUri,
                    folder,
                    _fileUploadPickerService.PickFileAsync,
                    "file",
                    "Could not choose file.");
            }
            else if (normalizedAction == CottonFileAddActionSheet.ScanDocumentAction)
            {
                await UploadPickedSourceAsync(
                    instanceUri,
                    folder,
                    _documentScanService.ScanDocumentAsync,
                    "document",
                    "Could not scan document.");
            }
            else if (normalizedAction == CottonFileAddActionSheet.UploadPhotoAction)
            {
                await UploadPickedSourceAsync(
                    instanceUri,
                    folder,
                    _photoUploadPickerService.PickPhotoAsync,
                    "photo",
                    "Could not choose photo.");
            }
            else if (normalizedAction == CottonFileAddActionSheet.UploadPhotoToFolderAction)
            {
                await UploadPickedSourceToDestinationAsync(
                    instanceUri,
                    _photoUploadPickerService.PickPhotoAsync,
                    "photo",
                    "Could not choose photo.");
            }
            else if (normalizedAction == CottonFileAddActionSheet.UploadVideoAction)
            {
                await UploadPickedSourceAsync(
                    instanceUri,
                    folder,
                    _videoUploadPickerService.PickVideoAsync,
                    "video",
                    "Could not choose video.");
            }
            else if (normalizedAction == CottonFileAddActionSheet.UploadVideoToFolderAction)
            {
                await UploadPickedSourceToDestinationAsync(
                    instanceUri,
                    _videoUploadPickerService.PickVideoAsync,
                    "video",
                    "Could not choose video.");
            }
        }

        private async Task CreateFolderAsync(Uri instanceUri, CottonFolderHandle parentFolder)
        {
            if (!_networkAccess.HasInternetAccess)
            {
                _display.ShowFilesStatus(CottonFolderCreationStatusText.OfflineStatus);
                return;
            }

            string? requestedName = await _dialogService.ShowPromptAsync(
                NewFolderPromptTitle,
                NewFolderPromptMessage,
                NewFolderCreateAction,
                CancelAction);
            if (requestedName is null)
            {
                return;
            }

            if (!CanUseFileBrowserContext(instanceUri) || !HasSameFolder(_currentFolder, parentFolder))
            {
                return;
            }

            if (!CottonFolderNameValidator.TryNormalize(
                    requestedName,
                    _display.AllFileEntries.Select(entry => entry.Name),
                    out string folderName,
                    out string validationStatus))
            {
                _display.ShowFilesStatus(validationStatus);
                return;
            }

            CancellationTokenSource fileActionCancellation =
                BeginFileAction(CottonFolderCreationStatusText.CreateCreatingStatus(folderName));

            try
            {
                await _fileBrowserService.CreateFolderAsync(
                    instanceUri,
                    parentFolder,
                    folderName,
                    fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri)
                    || !HasSameFolder(_currentFolder, parentFolder))
                {
                    return;
                }

                await RefreshAfterFolderCreatedAsync(
                    instanceUri,
                    parentFolder,
                    folderName,
                    fileActionCancellation);
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile folder creation authorization failure.");
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile folder creation cancellation.");
                    return;
                }

                ClearFileActionRetry();
                _display.ShowFilesStatus(CottonFolderCreationStatusText.CancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile folder creation failure.");
                    return;
                }

                _logger.LogError(exception, "Failed to create Cotton mobile folder.");
                ClearFileActionRetry();
                _display.ShowFilesStatus(CreateFolderCreationFailureStatus(exception));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh: false);
            }
        }

        public async Task ShowSortActionsAsync()
        {
            Uri? instanceUri = GetActiveFileBrowserInstance();
            if (instanceUri is null)
            {
                return;
            }

            string nameAction = CreateCurrentActionLabel(
                SortNameAction,
                _display.FileSortMode == CottonFileBrowserSortMode.Name);
            string updatedAction = CreateCurrentActionLabel(
                SortUpdatedAction,
                _display.FileSortMode == CottonFileBrowserSortMode.Updated);
            string typeAction = CreateCurrentActionLabel(
                SortTypeAction,
                _display.FileSortMode == CottonFileBrowserSortMode.Type);
            string sizeAction = CreateCurrentActionLabel(
                SortSizeAction,
                _display.FileSortMode == CottonFileBrowserSortMode.Size);
            string? action = await _dialogService.ShowActionSheetAsync(
                "Sort files by",
                CancelAction,
                null,
                nameAction,
                updatedAction,
                typeAction,
                sizeAction);

            if (!CanUseFileBrowserContext(instanceUri))
            {
                return;
            }

            switch (NormalizeAction(action))
            {
                case SortNameAction:
                    await SetSortModeAsync(CottonFileBrowserSortMode.Name);
                    break;
                case SortUpdatedAction:
                    await SetSortModeAsync(CottonFileBrowserSortMode.Updated);
                    break;
                case SortTypeAction:
                    await SetSortModeAsync(CottonFileBrowserSortMode.Type);
                    break;
                case SortSizeAction:
                    await SetSortModeAsync(CottonFileBrowserSortMode.Size);
                    break;
            }
        }

        public Task CancelFileActionAsync()
        {
            if (_fileActionCancellation is null)
            {
                return Task.CompletedTask;
            }

            _display.ShowFileActionCancelling("Cancelling...");
            _fileActionCancellation.Cancel();
            return Task.CompletedTask;
        }

        public async Task RetryFileActionAsync()
        {
            if (_retryFileAction is null || _retryFileActionEntry is null)
            {
                return;
            }

            MainPageFileAction action = _retryFileAction.Value;
            CottonFileBrowserEntry entry = _retryFileActionEntry;
            ClearFileActionRetry();
            CottonFileBrowserEntry? currentEntry = GetCurrentVisibleEntry(entry);
            if (currentEntry is null)
            {
                return;
            }

            switch (action)
            {
                case MainPageFileAction.CopyLink:
                    await CopyCloudShareLinkAsync(currentEntry);
                    break;
                case MainPageFileAction.Download:
                    await DownloadFileAsync(currentEntry);
                    break;
                case MainPageFileAction.KeepFolderOffline:
                    await PlanFolderOfflineAsync(currentEntry);
                    break;
                case MainPageFileAction.KeepOffline:
                    await KeepFileOfflineAsync(currentEntry);
                    break;
                case MainPageFileAction.Open:
                    await OpenFileAsync(currentEntry);
                    break;
                case MainPageFileAction.RefreshOffline:
                    await RefreshOfflineFileAsync(currentEntry);
                    break;
                case MainPageFileAction.RemoveOffline:
                    await RemoveFileOfflineAsync(currentEntry);
                    break;
                case MainPageFileAction.Share:
                    await ShareFileAsync(currentEntry);
                    break;
                case MainPageFileAction.ShareLink:
                    await ShareCloudShareLinkAsync(currentEntry);
                    break;
                case MainPageFileAction.SyncFolderToDevice:
                    await SyncFolderToDeviceAsync(currentEntry);
                    break;
                case MainPageFileAction.SyncFolderFromSelectedFolder:
                    await SyncFolderFromDeviceAsync(currentEntry);
                    break;
                case MainPageFileAction.SyncFolderBothWays:
                    await SyncFolderBothWaysAsync(currentEntry);
                    break;
                case MainPageFileAction.SyncFolderToSelectedFolder:
                    await SyncFolderToDeviceAsync(currentEntry, useSelectedFolder: true);
                    break;
            }
        }

        private async Task ShowFolderActionsAsync(CottonFileBrowserEntry folder)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null || !CanUseVisibleEntry(folder, instanceUri))
            {
                return;
            }

            var actions = new List<string>
            {
                OpenAction,
                CopyLinkAction,
                ShareLinkAction,
                KeepOfflineAction,
                SyncToDeviceAction,
            };
            if (_syncLocalRootPickerService.IsAvailable)
            {
                actions.Add(SyncToSelectedFolderAction);
                actions.Add(SyncFromSelectedFolderAction);
                actions.Add(SyncBothWaysAction);
            }

            string? action = await _dialogService.ShowActionSheetAsync(
                folder.Name,
                CancelAction,
                null,
                actions.ToArray());

            CottonFileBrowserEntry? currentFolder = GetCurrentVisibleEntry(folder, instanceUri);
            if (currentFolder is null)
            {
                return;
            }

            switch (NormalizeAction(action))
            {
                case OpenAction:
                    await OpenFolderAsync(currentFolder);
                    break;
                case CopyLinkAction:
                    await CopyCloudShareLinkAsync(currentFolder);
                    break;
                case ShareLinkAction:
                    await ShareCloudShareLinkAsync(currentFolder);
                    break;
                case KeepOfflineAction:
                    await PlanFolderOfflineAsync(currentFolder);
                    break;
                case SyncToDeviceAction:
                    await SyncFolderToDeviceAsync(currentFolder);
                    break;
                case SyncToSelectedFolderAction:
                    await SyncFolderToDeviceAsync(currentFolder, useSelectedFolder: true);
                    break;
                case SyncFromSelectedFolderAction:
                    await SyncFolderFromDeviceAsync(currentFolder);
                    break;
                case SyncBothWaysAction:
                    await SyncFolderBothWaysAsync(currentFolder);
                    break;
            }
        }

        private async Task OpenFolderAsync(CottonFileBrowserEntry folder)
        {
            if (IsFileBrowserBusy())
            {
                return;
            }

            _isFolderNavigationInProgress = true;
            try
            {
                CottonFolderHandle? originalFolder = _currentFolder;
                var originalNavigation = new List<CottonFolderHandle>(_fileNavigation);
                string originalSearchText = _display.FileSearchText;
                bool originalSearchOpen = _display.IsFileSearchOpen;
                IReadOnlyList<CottonFolderHandle> navigationAfterOpen =
                    CottonFileNavigationPlanner.CreateNavigationAfterOpenFolder(
                        _currentFolder,
                        _fileNavigation,
                        IsDisplayingRootFolder());
                _fileNavigation.Clear();
                _fileNavigation.AddRange(navigationAfterOpen);

                _display.ClearFileSearch();
                await LoadFolderAsync(new CottonFolderHandle(folder.Id, folder.Name), preserveHistory: false);
                if (ShouldRestoreNavigationAfterFileLoadFailure())
                {
                    _display.RestoreFileSearch(originalSearchText, originalSearchOpen);
                }
                else if (ShouldRestoreNavigationAfterUnchangedFolder(originalFolder, originalNavigation))
                {
                    RestoreNavigation(originalFolder, originalNavigation, originalSearchText, originalSearchOpen);
                }
            }
            finally
            {
                _isFolderNavigationInProgress = false;
                QueuePendingFileLoadRecoveryRefreshAfterBusy();
            }
        }

        private async Task ShowFileActionsAsync(CottonFileBrowserEntry file)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null || !CanUseVisibleEntry(file, instanceUri))
            {
                return;
            }

            await RefreshLocalFileStateAsync(instanceUri, CancellationToken.None);
            CottonFileBrowserEntry? refreshedFile = GetCurrentVisibleEntry(file, instanceUri);
            if (refreshedFile is null)
            {
                return;
            }

            string openAction = CreateOpenAction(refreshedFile, instanceUri);
            string downloadAction = CreateDownloadAction(refreshedFile);
            var actions = new List<string>
            {
                openAction,
                downloadAction,
            };
            if (refreshedFile.OfflineAvailability.NeedsRefresh)
            {
                actions.Add(RefreshOfflineAction);
            }
            else if (refreshedFile.OfflineAvailability.IsPinned)
            {
                actions.Add(RemoveOfflineAction);
            }
            else
            {
                actions.Add(KeepOfflineAction);
            }

            actions.Add(CopyLinkAction);
            actions.Add(ShareLinkAction);
            actions.Add(ShareFileAction);
            actions.Add(DetailsAction);

            string? action = await _dialogService.ShowActionSheetAsync(
                refreshedFile.Name,
                CancelAction,
                null,
                actions.ToArray());

            CottonFileBrowserEntry? currentFile = GetCurrentVisibleEntry(refreshedFile, instanceUri);
            if (currentFile is null)
            {
                return;
            }

            if (string.Equals(action, openAction, StringComparison.Ordinal))
            {
                await OpenFileAsync(currentFile);
                return;
            }

            switch (action)
            {
                case DownloadAction:
                case DownloadAgainAction:
                    await DownloadFileAsync(currentFile);
                    break;
                case KeepOfflineAction:
                    await KeepFileOfflineAsync(currentFile);
                    break;
                case RefreshOfflineAction:
                    await RefreshOfflineFileAsync(currentFile);
                    break;
                case RemoveOfflineAction:
                    await RemoveFileOfflineAsync(currentFile);
                    break;
                case CopyLinkAction:
                    await CopyCloudShareLinkAsync(currentFile);
                    break;
                case ShareLinkAction:
                    await ShareCloudShareLinkAsync(currentFile);
                    break;
                case ShareFileAction:
                    await ShareFileAsync(currentFile);
                    break;
                case DetailsAction:
                    await ShowFileDetailsAsync(currentFile);
                    break;
            }
        }

        private bool CanUseVisibleEntry(CottonFileBrowserEntry entry)
        {
            return GetCurrentVisibleEntry(entry) is not null;
        }

        private bool CanUseVisibleEntry(CottonFileBrowserEntry entry, Uri instanceUri)
        {
            return GetCurrentVisibleEntry(entry, instanceUri) is not null;
        }

        private Uri? GetActiveFileBrowserInstance()
        {
            Uri? instanceUri = _instanceUri;
            return instanceUri is not null && CanUseFileBrowserContext(instanceUri)
                ? instanceUri
                : null;
        }

        private bool CanUseFileBrowserContext(Uri instanceUri)
        {
            return Uri.Equals(_instanceUri, instanceUri)
                && _display.IsProfileVisible
                && !IsFileBrowserBusy();
        }

        private CottonFileBrowserEntry? GetCurrentVisibleEntry(CottonFileBrowserEntry entry)
        {
            Uri? instanceUri = _instanceUri;
            return instanceUri is null ? null : GetCurrentVisibleEntry(entry, instanceUri);
        }

        private CottonFileBrowserEntry? GetCurrentVisibleEntry(CottonFileBrowserEntry entry, Uri instanceUri)
        {
            if (!CanUseFileBrowserContext(instanceUri))
            {
                return null;
            }

            foreach (CottonFileBrowserEntry visibleEntry in _display.FileEntries)
            {
                if (visibleEntry.Id == entry.Id)
                {
                    return visibleEntry;
                }
            }

            return null;
        }

        private Task SetSortModeAsync(CottonFileBrowserSortMode sortMode)
        {
            ClearFileActionRetry();
            _display.ShowFileSortMode(sortMode);
            SaveSortModePreference(sortMode);
            return Task.CompletedTask;
        }

        private void SetViewMode(CottonFileBrowserViewMode viewMode)
        {
            ClearFileActionRetry();
            _display.ShowFileViewMode(viewMode);
            SaveViewModePreference(viewMode);
        }

        private void SaveSortModePreference(CottonFileBrowserSortMode sortMode)
        {
            try
            {
                _preferenceStore.SaveSortMode(sortMode);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save Cotton mobile file sort mode preference.");
            }
        }

        private void SaveViewModePreference(CottonFileBrowserViewMode viewMode)
        {
            try
            {
                _preferenceStore.SaveViewMode(viewMode);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save Cotton mobile file view mode preference.");
            }
        }

        private async Task LoadRootFilesAsync(bool isRefresh = false)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                return;
            }

            CancellationTokenSource fileLoadCancellation = BeginFileLoad();
            try
            {
                if (isRefresh)
                {
                    _display.ShowFilesRefreshing("Refreshing files...");
                    if (ShowCurrentOfflineNoticeIfNeeded())
                    {
                        return;
                    }
                }
                else
                {
                    _display.ClearFileSearch();
                    _display.ShowFilesLoading("Loading files...");
                }

                CottonFolderContent content = await _fileBrowserService.GetRootAsync(
                    instanceUri,
                    fileLoadCancellation.Token);
                await _folderContentCache.SaveRootAsync(instanceUri, content, fileLoadCancellation.Token);
                content = await ApplyThumbnailsAsync(instanceUri, content, fileLoadCancellation.Token);
                content = await ApplyLocalFilesAsync(instanceUri, content, fileLoadCancellation.Token);
                if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
                {
                    return;
                }

                _fileNavigation.Clear();
                _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _lastFileLoadFailed = false;
                _lastFileLoadDisplayedCachedContent = false;
                _display.ShowFiles(content, isRoot: true, canNavigateUp: false, CreatePath(content.FolderName));
            }
            catch (OperationCanceledException) when (fileLoadCancellation.IsCancellationRequested)
            {
                _logger.LogDebug("Cotton mobile root file load was cancelled.");
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile root authorization failure.");
                    return;
                }

                await HandleSessionExpiredAsync(exception);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile root load failure.");
                    return;
                }

                _logger.LogWarning(exception, "Failed to load Cotton mobile root files.");
                if (await ShowCachedRootFilesIfOfflineAsync(instanceUri, fileLoadCancellation))
                {
                    return;
                }

                ShowFileLoadFailure("Could not load files. Try refresh.");
            }
            finally
            {
                EndFileLoad(fileLoadCancellation);
            }
        }

        private async Task LoadFolderAsync(
            CottonFolderHandle folder,
            bool preserveHistory,
            bool isRefresh = false)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                return;
            }

            CancellationTokenSource fileLoadCancellation = BeginFileLoad();
            try
            {
                if (isRefresh)
                {
                    _display.ShowFilesRefreshing($"Refreshing {folder.Name}...");
                    if (ShowCurrentOfflineNoticeIfNeeded())
                    {
                        return;
                    }
                }
                else
                {
                    _display.ShowFilesLoading($"Loading {folder.Name}...");
                }

                CottonFolderContent content = await _fileBrowserService.GetFolderAsync(
                    instanceUri,
                    folder,
                    fileLoadCancellation.Token);
                await _folderContentCache.SaveFolderAsync(instanceUri, content, fileLoadCancellation.Token);
                content = await ApplyThumbnailsAsync(instanceUri, content, fileLoadCancellation.Token);
                content = await ApplyLocalFilesAsync(instanceUri, content, fileLoadCancellation.Token);
                if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
                {
                    return;
                }

                _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _lastFileLoadFailed = false;
                _lastFileLoadDisplayedCachedContent = false;
                _display.ShowFiles(
                    content,
                    isRoot: false,
                    canNavigateUp: true,
                    CreatePath(content.FolderName));
            }
            catch (OperationCanceledException) when (fileLoadCancellation.IsCancellationRequested)
            {
                _logger.LogDebug("Cotton mobile folder file load {FolderId} was cancelled.", folder.Id);
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile folder authorization failure {FolderId}.", folder.Id);
                    return;
                }

                if (!preserveHistory && _fileNavigation.Count > 0)
                {
                    _fileNavigation.RemoveAt(_fileNavigation.Count - 1);
                }

                await HandleSessionExpiredAsync(exception);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile folder load failure {FolderId}.", folder.Id);
                    return;
                }

                _logger.LogWarning(exception, "Failed to load Cotton mobile folder {FolderId}.", folder.Id);
                if (await ShowCachedFolderFilesIfOfflineAsync(instanceUri, folder, fileLoadCancellation))
                {
                    return;
                }

                if (!preserveHistory && _fileNavigation.Count > 0)
                {
                    _fileNavigation.RemoveAt(_fileNavigation.Count - 1);
                }

                ShowFileLoadFailure("Could not open folder. Try again.");
            }
            finally
            {
                EndFileLoad(fileLoadCancellation);
            }
        }

        private async Task<bool> ShowCachedRootFilesIfOfflineAsync(
            Uri instanceUri,
            CancellationTokenSource fileLoadCancellation)
        {
            if (_networkAccess.HasInternetAccess)
            {
                return false;
            }

            CottonCachedFolderContentSnapshot? cachedSnapshot = await _folderContentCache.LoadRootSnapshotAsync(
                instanceUri,
                fileLoadCancellation.Token);
            fileLoadCancellation.Token.ThrowIfCancellationRequested();
            if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
            {
                return true;
            }

            if (cachedSnapshot is null)
            {
                return false;
            }

            return await ShowCachedRootFilesAsync(instanceUri, cachedSnapshot, fileLoadCancellation);
        }

        private async Task<bool> ShowCachedRootFilesAsync(
            Uri instanceUri,
            CottonCachedFolderContentSnapshot cachedSnapshot,
            CancellationTokenSource fileLoadCancellation)
        {
            CottonFolderContent cachedContent = await ApplyCachedThumbnailsAsync(
                instanceUri,
                cachedSnapshot.Content,
                fileLoadCancellation.Token);
            cachedContent = await ApplyLocalFilesAsync(instanceUri, cachedContent, fileLoadCancellation.Token);
            if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
            {
                return true;
            }

            _fileNavigation.Clear();
            _currentFolder = new CottonFolderHandle(cachedContent.FolderId, cachedContent.FolderName);
            _lastFileLoadFailed = true;
            _lastFileLoadDisplayedCachedContent = true;
            _display.ShowFiles(
                cachedContent,
                isRoot: true,
                canNavigateUp: false,
                CreatePath(cachedContent.FolderName));
            _display.ShowOfflineFilesNotice(isCachedListing: true, cachedSnapshot.CachedAtUtc);
            return true;
        }

        private async Task<bool> ShowCachedFolderFilesIfOfflineAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationTokenSource fileLoadCancellation)
        {
            if (_networkAccess.HasInternetAccess)
            {
                return false;
            }

            CottonCachedFolderContentSnapshot? cachedSnapshot = await _folderContentCache.LoadFolderSnapshotAsync(
                instanceUri,
                folder,
                fileLoadCancellation.Token);
            fileLoadCancellation.Token.ThrowIfCancellationRequested();
            if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
            {
                return true;
            }

            if (cachedSnapshot is null)
            {
                return false;
            }

            CottonFolderContent cachedContent = await ApplyCachedThumbnailsAsync(
                instanceUri,
                cachedSnapshot.Content,
                fileLoadCancellation.Token);
            cachedContent = await ApplyLocalFilesAsync(instanceUri, cachedContent, fileLoadCancellation.Token);
            _currentFolder = new CottonFolderHandle(cachedContent.FolderId, cachedContent.FolderName);
            _lastFileLoadFailed = true;
            _lastFileLoadDisplayedCachedContent = true;
            _display.ShowFiles(
                cachedContent,
                isRoot: false,
                canNavigateUp: true,
                CreatePath(cachedContent.FolderName));
            _display.ShowOfflineFilesNotice(isCachedListing: true, cachedSnapshot.CachedAtUtc);
            return true;
        }

        private async Task DownloadFileAsync(CottonFileBrowserEntry file)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to download files.");
                return;
            }

            if (ShowOfflineRetryIfNeeded(MainPageFileAction.Download, file, OfflineDownloadStatus))
            {
                return;
            }

            CancellationTokenSource fileActionCancellation = BeginFileAction($"Downloading {file.Name}...");
            bool shouldRunRecoveryRefresh = false;

            try
            {
                CottonFileDownloadResult result = await _fileBrowserService.DownloadAsync(
                    instanceUri,
                    file,
                    CreateFileDownloadProgress(
                        file,
                        "Downloading",
                        () => IsActiveFileAction(fileActionCancellation, instanceUri),
                        fileActionCancellation.Token),
                    fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                await RefreshLocalFileStateAsync(instanceUri, CancellationToken.None);
                _display.ShowFileActionAwaitingFollowUp();
                shouldRunRecoveryRefresh = await ShowDownloadedFileActionsBestEffortAsync(
                    file,
                    result,
                    () => IsActiveFileAction(fileActionCancellation, instanceUri),
                    fileActionCancellation.Token);
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile download authorization failure {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile download cancellation {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                if (ShowReusableLocalFileIfAvailable(file))
                {
                    _display.ShowFilesSummary();
                    return;
                }

                _display.ShowFilesStatus("Download cancelled.");
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile download failure {FileId}.", file.Id);
                    return;
                }

                _logger.LogError(exception, "Failed to download Cotton mobile file {FileId}.", file.Id);
                ShowFileActionRetry(
                    MainPageFileAction.Download,
                    file,
                    CreateFileActionFailureStatus(exception, "Download failed.", OfflineDownloadStatus));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh);
            }
        }

        private Task KeepFileOfflineAsync(CottonFileBrowserEntry file)
        {
            return SaveFileOfflineAsync(
                file,
                MainPageFileAction.KeepOffline,
                "keep files offline",
                "Saving",
                CottonOfflineFileStatusText.CreateStartingStatus(file.Name),
                CottonOfflineFileStatusText.CreateAvailableStatus,
                CottonOfflineFileStatusText.CancelledStatus,
                CottonOfflineFileStatusText.FailedStatus,
                CottonOfflineFileStatusText.OfflineUnavailableStatus);
        }

        private Task RefreshOfflineFileAsync(CottonFileBrowserEntry file)
        {
            return SaveFileOfflineAsync(
                file,
                MainPageFileAction.RefreshOffline,
                "refresh offline files",
                "Refreshing",
                CottonOfflineFileStatusText.CreateRefreshingStatus(file.Name),
                CottonOfflineFileStatusText.CreateRefreshedStatus,
                CottonOfflineFileStatusText.RefreshCancelledStatus,
                CottonOfflineFileStatusText.RefreshFailedStatus,
                CottonOfflineFileStatusText.RefreshOfflineUnavailableStatus);
        }

        private async Task SaveFileOfflineAsync(
            CottonFileBrowserEntry file,
            MainPageFileAction retryAction,
            string signInActionName,
            string progressActionName,
            string startingStatus,
            Func<string, string> createCompletedStatus,
            string cancelledStatus,
            string failedStatus,
            string offlineUnavailableStatus)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus($"Sign in to {signInActionName}.");
                return;
            }

            CottonLocalFileSnapshot? reusableLocalFile =
                _fileBrowserService.GetReusableLocalDownloadSnapshot(instanceUri, file);
            if (reusableLocalFile is not null)
            {
                await PinFileOfflineAsync(instanceUri, file, CancellationToken.None);
                await RefreshLocalFileStateAsync(instanceUri, CancellationToken.None);
                _display.ShowFilesStatus(createCompletedStatus(file.Name));
                return;
            }

            if (ShowOfflineRetryIfNeeded(retryAction, file, offlineUnavailableStatus))
            {
                return;
            }

            CancellationTokenSource fileActionCancellation = BeginFileAction(startingStatus);
            bool shouldRunRecoveryRefresh = false;

            try
            {
                CottonFileDownloadResult result = await _fileBrowserService.DownloadAsync(
                    instanceUri,
                    file,
                    CreateFileDownloadProgress(
                        file,
                        progressActionName,
                        () => IsActiveFileAction(fileActionCancellation, instanceUri),
                        fileActionCancellation.Token),
                    fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                await PinFileOfflineAsync(instanceUri, file, CancellationToken.None);
                await RefreshLocalFileStateAsync(instanceUri, CancellationToken.None);
                _display.ShowFilesStatus(createCompletedStatus(result.FileName));
                shouldRunRecoveryRefresh = true;
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile offline-save authorization failure {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile offline-save cancellation {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                if (await ShowReusableLocalFileIfAvailableAsync(file))
                {
                    _display.ShowFilesStatus(createCompletedStatus(file.Name));
                    return;
                }

                _display.ShowFilesStatus(cancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile offline-save failure {FileId}.", file.Id);
                    return;
                }

                _logger.LogError(exception, "Failed to save Cotton mobile file offline {FileId}.", file.Id);
                ShowFileActionRetry(
                    retryAction,
                    file,
                    CreateFileActionFailureStatus(
                        exception,
                        failedStatus,
                        offlineUnavailableStatus));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh);
            }
        }

        private async Task RemoveFileOfflineAsync(CottonFileBrowserEntry file)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to remove offline files.");
                return;
            }

            CottonLocalFileSnapshot? localFile = _fileBrowserService.GetLocalDownload(instanceUri, file);
            if (localFile is null)
            {
                await RemoveFileOfflinePinAsync(instanceUri, file.Id, CancellationToken.None);
                await RefreshLocalFileStateAsync(instanceUri, CancellationToken.None);
                _display.ShowFilesStatus(CottonOfflineFileStatusText.CreateNotOnDeviceStatus(file.Name));
                return;
            }

            CancellationTokenSource fileActionCancellation =
                BeginFileAction($"Removing {file.Name} from this device...");

            try
            {
                bool deleted = await _fileBrowserService.DeleteLocalDownloadAsync(
                    instanceUri,
                    file,
                    fileActionCancellation.Token);
                await RemoveFileOfflinePinAsync(instanceUri, file.Id, CancellationToken.None);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                await RefreshLocalFileStateAsync(instanceUri, CancellationToken.None);
                _display.ShowFilesStatus(
                    deleted
                        ? CottonOfflineFileStatusText.CreateRemovedStatus(file.Name)
                        : CottonOfflineFileStatusText.CreateNotOnDeviceStatus(file.Name));
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile remove-offline cancellation {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                _display.ShowFilesStatus(CottonOfflineFileStatusText.RemoveCancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile remove-offline failure {FileId}.", file.Id);
                    return;
                }

                _logger.LogError(exception, "Failed to remove Cotton mobile offline file {FileId}.", file.Id);
                ShowFileActionRetry(
                    MainPageFileAction.RemoveOffline,
                    file,
                    CottonOfflineFileStatusText.RemoveFailedStatus);
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh: false);
            }
        }

        private Task PinFileOfflineAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken)
        {
            return _offlineFilePinStore.AddOrReplaceAsync(
                instanceUri,
                CottonOfflineFilePinSnapshot.Create(file, DateTime.UtcNow),
                cancellationToken);
        }

        private Task RemoveFileOfflinePinAsync(
            Uri instanceUri,
            Guid fileId,
            CancellationToken cancellationToken)
        {
            return _offlineFilePinStore.RemoveAsync(instanceUri, fileId, cancellationToken);
        }

        private async Task PlanFolderOfflineAsync(CottonFileBrowserEntry folder)
        {
            if (!folder.IsFolder)
            {
                return;
            }

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to keep folders offline.");
                return;
            }

            var folderHandle = new CottonFolderHandle(folder.Id, folder.Name);
            if (!_networkAccess.HasInternetAccess)
            {
                CottonFolderContent? cachedContent =
                    await TryLoadCachedFolderForOfflinePlanAsync(instanceUri, folderHandle, CancellationToken.None);
                if (cachedContent is not null)
                {
                    ShowFolderOfflinePlanStatus(cachedContent, isCachedEstimate: true);
                    return;
                }

                ClearFileActionRetry();
                ShowFileActionRetry(
                    MainPageFileAction.KeepFolderOffline,
                    folder,
                    CottonOfflineFolderStatusText.OfflineUnavailableStatus);
                return;
            }

            CancellationTokenSource fileActionCancellation =
                BeginFileAction(CottonOfflineFolderStatusText.CreateStartingStatus(folder.Name));

            try
            {
                CottonFolderContent content = await _fileBrowserService.GetFolderAsync(
                    instanceUri,
                    folderHandle,
                    fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                await _folderContentCache.SaveFolderAsync(
                    instanceUri,
                    content,
                    fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                CottonOfflineFolderPlanSnapshot plan = ShowFolderOfflinePlanStatus(
                    content,
                    isCachedEstimate: false);
                if (plan.CanQueueDirectFiles)
                {
                    await DownloadFolderDirectFilesOfflineAsync(
                        instanceUri,
                        folder,
                        content,
                        fileActionCancellation);
                }
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile folder offline authorization failure {FolderId}.", folder.Id);
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile folder offline cancellation {FolderId}.", folder.Id);
                    return;
                }

                ClearFileActionRetry();
                _display.ShowFilesStatus(CottonOfflineFolderStatusText.CancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile folder offline failure {FolderId}.", folder.Id);
                    return;
                }

                _logger.LogWarning(exception, "Failed to plan Cotton mobile folder offline {FolderId}.", folder.Id);
                ShowFileActionRetry(
                    MainPageFileAction.KeepFolderOffline,
                    folder,
                    _networkAccess.HasInternetAccess
                        ? CottonOfflineFolderStatusText.FailedStatus
                        : CottonOfflineFolderStatusText.OfflineUnavailableStatus);
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh: false);
            }
        }

        private async Task<CottonFolderContent?> TryLoadCachedFolderForOfflinePlanAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _folderContentCache.LoadFolderAsync(
                    instanceUri,
                    folder,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogDebug(
                    exception,
                    "Failed to load cached Cotton mobile folder offline plan {FolderId}.",
                    folder.Id);
                return null;
            }
        }

        private async Task DownloadFolderDirectFilesOfflineAsync(
            Uri instanceUri,
            CottonFileBrowserEntry folder,
            CottonFolderContent content,
            CancellationTokenSource fileActionCancellation)
        {
            CottonOfflineDownloadQueueSnapshot queue = CottonOfflineDownloadQueueSnapshot.Create(content);
            if (!await ConfirmOfflineFolderStorageAsync(queue, fileActionCancellation.Token))
            {
                ClearFileActionRetry();
                _display.ShowFilesStatus(CottonOfflineFolderStatusText.CancelledStatus);
                return;
            }

            _display.ShowFileActionLoading(CottonOfflineDownloadQueueStatusText.CreateQueuedStatus(queue));
            _display.ShowOfflinePackProgress(
                CottonOfflinePackProgressSnapshot.CreateRunning(
                    queue,
                    completedCount: 0,
                    completedBytes: 0,
                    currentItem: queue.Items[0]));

            int completedCount = 0;
            long completedBytes = 0;
            try
            {
                foreach (CottonOfflineDownloadQueueItem item in queue.Items)
                {
                    fileActionCancellation.Token.ThrowIfCancellationRequested();
                    if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                    {
                        return;
                    }

                    CottonFileBrowserEntry file = content.Entries.Single(entry => entry.Id == item.FileId);
                    _display.ShowOfflinePackProgress(
                        CottonOfflinePackProgressSnapshot.CreateRunning(
                            queue,
                            completedCount,
                            completedBytes,
                            item));
                    CottonLocalFileSnapshot? reusableLocalFile =
                        _fileBrowserService.GetReusableLocalDownloadSnapshot(instanceUri, file);
                    if (reusableLocalFile is not null)
                    {
                        await _offlineFilePinStore.AddOrReplaceAsync(
                            instanceUri,
                            item.CreatePin(DateTime.UtcNow),
                            CancellationToken.None);
                        _display.ShowFileLocalCopy(file, reusableLocalFile);
                        completedCount++;
                        completedBytes += item.SizeBytes;
                        _display.ShowOfflinePackProgress(
                            CottonOfflinePackProgressSnapshot.CreateRunning(
                                queue,
                                completedCount,
                                completedBytes,
                                GetNextOfflineQueueItem(queue, completedCount)));
                        continue;
                    }

                    _display.ShowFileActionLoading(
                        CottonOfflineDownloadQueueStatusText.CreateStartingItemStatus(item, queue.TotalCount));
                    await _fileBrowserService.DownloadAsync(
                        instanceUri,
                        file,
                        CreateFileDownloadProgress(
                            file,
                            $"Saving {item.Position} of {queue.TotalCount}",
                            () => IsActiveFileAction(fileActionCancellation, instanceUri),
                            fileActionCancellation.Token),
                        fileActionCancellation.Token);
                    fileActionCancellation.Token.ThrowIfCancellationRequested();
                    if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                    {
                        return;
                    }

                    await _offlineFilePinStore.AddOrReplaceAsync(
                        instanceUri,
                        item.CreatePin(DateTime.UtcNow),
                        CancellationToken.None);
                    completedCount++;
                    completedBytes += item.SizeBytes;
                    await RefreshLocalFileStateAsync(instanceUri, CancellationToken.None);
                    _display.ShowOfflinePackProgress(
                        CottonOfflinePackProgressSnapshot.CreateRunning(
                            queue,
                            completedCount,
                            completedBytes,
                            GetNextOfflineQueueItem(queue, completedCount)));
                }

                _display.ShowOfflinePackProgress(CottonOfflinePackProgressSnapshot.CreateCompleted(queue));
                _display.ShowFilesStatus(CottonOfflineDownloadQueueStatusText.CreateCompletedStatus(queue));
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                throw;
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                ClearFileActionRetry();
                _display.ShowOfflinePackProgress(
                    CottonOfflinePackProgressSnapshot.CreateCancelled(
                        queue,
                        completedCount,
                        completedBytes));
                _display.ShowFilesStatus(
                    CottonOfflineDownloadQueueStatusText.CreateCancelledStatus(
                        completedCount,
                        queue.TotalCount));
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile folder offline download failure {FolderId}.", folder.Id);
                    return;
                }

                _logger.LogWarning(exception, "Failed to keep Cotton mobile folder offline {FolderId}.", folder.Id);
                _display.ShowOfflinePackProgress(
                    CottonOfflinePackProgressSnapshot.CreateFailed(
                        queue,
                        completedCount,
                        completedBytes));
                ShowFileActionRetry(
                    MainPageFileAction.KeepFolderOffline,
                    folder,
                    CottonOfflineDownloadQueueStatusText.CreateFailedStatus(
                        completedCount,
                        queue.TotalCount));
            }
        }

        private async Task<bool> ConfirmOfflineFolderStorageAsync(
            CottonOfflineDownloadQueueSnapshot queue,
            CancellationToken cancellationToken)
        {
            CottonDeviceStorageSpaceSnapshot storageSpace =
                await _deviceStorageSpaceService.GetAppDataStorageSpaceAsync(cancellationToken);
            CottonOfflineFolderFreeSpaceWarning warning =
                CottonOfflineFolderFreeSpaceWarningPolicy.CreateWarning(queue, storageSpace);
            if (!warning.ShouldWarn)
            {
                return true;
            }

            return await _dialogService.ShowConfirmationAsync(
                warning.Title,
                warning.Message,
                CottonOfflineFolderFreeSpaceWarningText.AcceptAction,
                CottonOfflineFolderFreeSpaceWarningText.CancelAction);
        }

        private static CottonOfflineDownloadQueueItem? GetNextOfflineQueueItem(
            CottonOfflineDownloadQueueSnapshot queue,
            int completedCount)
        {
            ArgumentNullException.ThrowIfNull(queue);
            if (completedCount < 0 || completedCount > queue.TotalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(completedCount), "Completed count must be within the queue.");
            }

            return completedCount == queue.TotalCount ? null : queue.Items[completedCount];
        }

        private CottonOfflineFolderPlanSnapshot ShowFolderOfflinePlanStatus(
            CottonFolderContent content,
            bool isCachedEstimate)
        {
            CottonOfflineFolderPlanSnapshot plan = CottonOfflineFolderPlanSnapshot.Create(content);
            _display.ShowFilesStatus(CottonOfflineFolderStatusText.CreatePlanStatus(plan, isCachedEstimate));
            return plan;
        }

        private async Task SyncFolderToDeviceAsync(CottonFileBrowserEntry folder, bool useSelectedFolder = false)
        {
            if (!folder.IsFolder)
            {
                return;
            }

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to sync folders.");
                return;
            }

            string? accountScopeKey = _accountScopeKey;
            if (string.IsNullOrWhiteSpace(accountScopeKey))
            {
                _display.ShowFilesStatus(CottonCloudToDeviceSyncStatusText.AccountUnavailableStatus);
                return;
            }

            MainPageFileAction retryAction = useSelectedFolder
                ? MainPageFileAction.SyncFolderToSelectedFolder
                : MainPageFileAction.SyncFolderToDevice;
            if (ShowOfflineRetryIfNeeded(
                    retryAction,
                    folder,
                    CottonCloudToDeviceSyncStatusText.OfflineUnavailableStatus))
            {
                return;
            }

            CottonSyncLocalRootSnapshot? selectedLocalRoot = null;
            if (useSelectedFolder)
            {
                try
                {
                    selectedLocalRoot = await _syncLocalRootPickerService.PickUserSelectedDocumentTreeAsync();
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to pick Cotton mobile sync local folder.");
                    _display.ShowFilesStatus(CottonCloudToDeviceSyncStatusText.FailedStatus);
                    return;
                }

                if (selectedLocalRoot is null)
                {
                    _display.ShowFilesStatus(CottonCloudToDeviceSyncStatusText.CancelledStatus);
                    return;
                }
            }

            CancellationTokenSource fileActionCancellation =
                BeginFileAction(CottonCloudToDeviceSyncStatusText.CreateStartingStatus(folder.Name));
            bool shouldRunRecoveryRefresh = false;

            try
            {
                var destination = new CottonUploadDestinationSnapshot(
                    folder.Id,
                    folder.Name,
                    CreateChildFolderPath(folder.Name));
                CottonCloudToDeviceSyncRootSetupResult setupResult = selectedLocalRoot is null
                    ? await _cloudToDeviceSyncRootSetupService.EnableAppPrivateRootAsync(
                        instanceUri,
                        accountScopeKey,
                        destination,
                        fileActionCancellation.Token)
                    : await _cloudToDeviceSyncRootSetupService.EnableUserSelectedDocumentTreeRootAsync(
                        instanceUri,
                        accountScopeKey,
                        destination,
                        selectedLocalRoot,
                        fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                CottonCloudToDeviceSyncRunSummary summary =
                    await _cloudToDeviceSyncCoordinator.RunRootAsync(
                        instanceUri,
                        setupResult.Root,
                        fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                await RefreshLocalFileStateAsync(instanceUri, CancellationToken.None);
                _display.ShowFilesStatus(CottonCloudToDeviceSyncStatusText.CreateCompletedStatus(summary));
                shouldRunRecoveryRefresh = summary.HasAppliedChanges;
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile sync authorization failure {FolderId}.", folder.Id);
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile sync cancellation {FolderId}.", folder.Id);
                    return;
                }

                ClearFileActionRetry();
                _display.ShowFilesStatus(CottonCloudToDeviceSyncStatusText.CancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile sync failure {FolderId}.", folder.Id);
                    return;
                }

                _logger.LogWarning(exception, "Failed to sync Cotton mobile folder to device {FolderId}.", folder.Id);
                ShowFileActionRetry(
                    retryAction,
                    folder,
                    CreateFileActionFailureStatus(
                        exception,
                        CottonCloudToDeviceSyncStatusText.FailedStatus,
                        CottonCloudToDeviceSyncStatusText.OfflineUnavailableStatus));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh);
            }
        }

        private async Task SyncFolderFromDeviceAsync(CottonFileBrowserEntry folder)
        {
            if (!folder.IsFolder)
            {
                return;
            }

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to sync folders.");
                return;
            }

            string? accountScopeKey = _accountScopeKey;
            if (string.IsNullOrWhiteSpace(accountScopeKey))
            {
                _display.ShowFilesStatus(CottonDeviceToCloudSyncStatusText.AccountUnavailableStatus);
                return;
            }

            if (ShowOfflineRetryIfNeeded(
                    MainPageFileAction.SyncFolderFromSelectedFolder,
                    folder,
                    CottonDeviceToCloudSyncStatusText.OfflineUnavailableStatus))
            {
                return;
            }

            CottonSyncLocalRootSnapshot? selectedLocalRoot;
            try
            {
                selectedLocalRoot = await _syncLocalRootPickerService.PickUserSelectedDocumentTreeAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to pick Cotton mobile sync source folder.");
                _display.ShowFilesStatus(CottonDeviceToCloudSyncStatusText.FailedStatus);
                return;
            }

            if (selectedLocalRoot is null)
            {
                _display.ShowFilesStatus(CottonDeviceToCloudSyncStatusText.CancelledStatus);
                return;
            }

            CancellationTokenSource fileActionCancellation =
                BeginFileAction(CottonDeviceToCloudSyncStatusText.CreateStartingStatus(folder.Name));
            bool shouldRunRecoveryRefresh = false;

            try
            {
                var destination = new CottonUploadDestinationSnapshot(
                    folder.Id,
                    folder.Name,
                    CreateChildFolderPath(folder.Name));
                CottonDeviceToCloudSyncRootSetupResult setupResult =
                    await _deviceToCloudSyncRootSetupService.EnableUserSelectedDocumentTreeRootAsync(
                        instanceUri,
                        accountScopeKey,
                        destination,
                        selectedLocalRoot,
                        fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                if (setupResult.DirectionConflict)
                {
                    ClearFileActionRetry();
                    _display.ShowFilesStatus(CottonDeviceToCloudSyncStatusText.DirectionConflictStatus);
                    return;
                }

                CottonDeviceToCloudSyncRunSummary summary =
                    await _deviceToCloudSyncCoordinator.RunRootAsync(
                        instanceUri,
                        setupResult.Root,
                        fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                if (summary.NeedsDestructiveReview)
                {
                    _display.ShowFileActionLoading(CottonDeviceToCloudSyncStatusText.DestructiveReviewRequiredStatus);
                    bool confirmed = await ConfirmDeviceToCloudRemoteDeletesAsync(
                        summary.DestructiveReviewRemoteDeleteCount);
                    if (!confirmed)
                    {
                        ClearFileActionRetry();
                        _display.ShowFilesStatus(CottonDeviceToCloudSyncStatusText.CancelledStatus);
                        return;
                    }

                    summary = await _deviceToCloudSyncCoordinator.RunRootAsync(
                        instanceUri,
                        setupResult.Root,
                        CottonDeviceToCloudSyncRunOptions.AllowRemoteDeletes,
                        fileActionCancellation.Token);
                    fileActionCancellation.Token.ThrowIfCancellationRequested();
                    if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                    {
                        return;
                    }
                }

                _display.ShowFilesStatus(CottonDeviceToCloudSyncStatusText.CreateCompletedStatus(summary));
                shouldRunRecoveryRefresh = summary.HasAppliedChanges;
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile device sync authorization failure {FolderId}.", folder.Id);
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile device sync cancellation {FolderId}.", folder.Id);
                    return;
                }

                ClearFileActionRetry();
                _display.ShowFilesStatus(CottonDeviceToCloudSyncStatusText.CancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile device sync failure {FolderId}.", folder.Id);
                    return;
                }

                _logger.LogWarning(exception, "Failed to sync Cotton mobile folder from device {FolderId}.", folder.Id);
                ShowFileActionRetry(
                    MainPageFileAction.SyncFolderFromSelectedFolder,
                    folder,
                    CreateFileActionFailureStatus(
                        exception,
                        CottonDeviceToCloudSyncStatusText.FailedStatus,
                        CottonDeviceToCloudSyncStatusText.OfflineUnavailableStatus));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh);
            }
        }

        private async Task<bool> ConfirmDeviceToCloudRemoteDeletesAsync(int fileCount)
        {
            return await _dialogService.ShowConfirmationAsync(
                CottonDeviceToCloudSyncStatusText.ConfirmRemoteDeleteTitle,
                CottonDeviceToCloudSyncStatusText.CreateConfirmRemoteDeleteMessage(fileCount),
                CottonDeviceToCloudSyncStatusText.ConfirmRemoteDeleteAction,
                CancelAction);
        }

        private async Task SyncFolderBothWaysAsync(CottonFileBrowserEntry folder)
        {
            if (!folder.IsFolder)
            {
                return;
            }

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to sync folders.");
                return;
            }

            string? accountScopeKey = _accountScopeKey;
            if (string.IsNullOrWhiteSpace(accountScopeKey))
            {
                _display.ShowFilesStatus(CottonBidirectionalSyncStatusText.AccountUnavailableStatus);
                return;
            }

            if (ShowOfflineRetryIfNeeded(
                    MainPageFileAction.SyncFolderBothWays,
                    folder,
                    CottonBidirectionalSyncStatusText.OfflineUnavailableStatus))
            {
                return;
            }

            CottonSyncLocalRootSnapshot? selectedLocalRoot;
            try
            {
                selectedLocalRoot = await _syncLocalRootPickerService.PickUserSelectedDocumentTreeAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to pick Cotton mobile bidirectional sync folder.");
                _display.ShowFilesStatus(CottonBidirectionalSyncStatusText.FailedStatus);
                return;
            }

            if (selectedLocalRoot is null)
            {
                _display.ShowFilesStatus(CottonBidirectionalSyncStatusText.CancelledStatus);
                return;
            }

            CancellationTokenSource fileActionCancellation =
                BeginFileAction(CottonBidirectionalSyncStatusText.CreateStartingStatus(folder.Name));
            bool shouldRunRecoveryRefresh = false;

            try
            {
                var destination = new CottonUploadDestinationSnapshot(
                    folder.Id,
                    folder.Name,
                    CreateChildFolderPath(folder.Name));
                CottonBidirectionalSyncRootSetupResult setupResult =
                    await _bidirectionalSyncRootSetupService.EnableUserSelectedDocumentTreeRootAsync(
                        instanceUri,
                        accountScopeKey,
                        destination,
                        selectedLocalRoot,
                        fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                CottonBidirectionalSyncRunSummary summary =
                    await _bidirectionalSyncCoordinator.RunRootAsync(
                        instanceUri,
                        setupResult.Root,
                        fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                if (summary.NeedsDestructiveReview)
                {
                    _display.ShowFileActionLoading(CottonBidirectionalSyncStatusText.DestructiveReviewRequiredStatus);
                    bool confirmed = await ConfirmBidirectionalDestructiveChangesAsync(
                        summary.DestructiveReviewLocalDeleteCount,
                        summary.DestructiveReviewRemoteDeleteCount);
                    if (!confirmed)
                    {
                        ClearFileActionRetry();
                        _display.ShowFilesStatus(CottonBidirectionalSyncStatusText.CancelledStatus);
                        return;
                    }

                    summary = await _bidirectionalSyncCoordinator.RunRootAsync(
                        instanceUri,
                        setupResult.Root,
                        CottonBidirectionalSyncRunOptions.AllowDestructiveDeletes,
                        fileActionCancellation.Token);
                    fileActionCancellation.Token.ThrowIfCancellationRequested();
                    if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                    {
                        return;
                    }
                }

                await RefreshLocalFileStateAsync(instanceUri, CancellationToken.None);
                _display.ShowFilesStatus(CottonBidirectionalSyncStatusText.CreateCompletedStatus(summary));
                shouldRunRecoveryRefresh = summary.HasAppliedChanges;
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile bidirectional sync authorization failure {FolderId}.", folder.Id);
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile bidirectional sync cancellation {FolderId}.", folder.Id);
                    return;
                }

                ClearFileActionRetry();
                _display.ShowFilesStatus(CottonBidirectionalSyncStatusText.CancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile bidirectional sync failure {FolderId}.", folder.Id);
                    return;
                }

                _logger.LogWarning(exception, "Failed to sync Cotton mobile folder both ways {FolderId}.", folder.Id);
                ShowFileActionRetry(
                    MainPageFileAction.SyncFolderBothWays,
                    folder,
                    CreateFileActionFailureStatus(
                        exception,
                        CottonBidirectionalSyncStatusText.FailedStatus,
                        CottonBidirectionalSyncStatusText.OfflineUnavailableStatus));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh);
            }
        }

        private async Task<bool> ConfirmBidirectionalDestructiveChangesAsync(
            int localDeleteCount,
            int remoteDeleteCount)
        {
            return await _dialogService.ShowConfirmationAsync(
                CottonBidirectionalSyncStatusText.ConfirmDestructiveTitle,
                CottonBidirectionalSyncStatusText.CreateConfirmDestructiveMessage(
                    localDeleteCount,
                    remoteDeleteCount),
                CottonBidirectionalSyncStatusText.ConfirmDestructiveAction,
                CancelAction);
        }

        private async Task UploadPickedSourceAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            Func<CancellationToken, Task<CottonFileUploadSource?>> pickSourceAsync,
            string sourceKind,
            string pickFailureStatus)
        {
            if (!_networkAccess.HasInternetAccess)
            {
                _display.ShowFilesStatus(OfflineUploadStatus);
                return;
            }

            CottonFileUploadSource? source = await PickUploadSourceAsync(
                pickSourceAsync,
                sourceKind,
                pickFailureStatus);
            if (source is null)
            {
                return;
            }

            if (!CanUseFileBrowserContext(instanceUri) || !HasSameFolder(_currentFolder, folder))
            {
                return;
            }

            source = ResolveUniqueUploadSource(source);
            await UploadSourceToFolderAsync(
                instanceUri,
                folder,
                source,
                sourceKind,
                destinationPath: null,
                refreshCurrentFolderAfterUpload: true);
        }

        private async Task UploadPickedSourceToDestinationAsync(
            Uri instanceUri,
            Func<CancellationToken, Task<CottonFileUploadSource?>> pickSourceAsync,
            string sourceKind,
            string pickFailureStatus)
        {
            if (!_networkAccess.HasInternetAccess)
            {
                _display.ShowFilesStatus(OfflineUploadStatus);
                return;
            }

            CottonUploadDestinationSnapshot? destination;
            try
            {
                destination = await _uploadDestinationPickerPageService.PickAsync(instanceUri);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to pick a Cotton mobile upload destination.");
                _display.ShowFilesStatus("Could not choose destination.");
                return;
            }

            if (destination is null)
            {
                return;
            }

            if (!CanUseFileBrowserContext(instanceUri))
            {
                return;
            }

            CottonFolderHandle destinationFolder = destination.ToFolderHandle();
            IReadOnlyList<CottonFileBrowserEntry> destinationEntries;
            try
            {
                destinationEntries = HasSameFolder(_currentFolder, destinationFolder)
                    ? _display.AllFileEntries
                    : (await _fileBrowserService.GetFolderAsync(instanceUri, destinationFolder)).Entries;
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                await HandleSessionExpiredAsync(exception);
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to load Cotton mobile upload destination {FolderId}.",
                    destinationFolder.Id);
                _display.ShowFilesStatus("Could not load destination.");
                return;
            }

            CottonFileUploadSource? source = await PickUploadSourceAsync(
                pickSourceAsync,
                sourceKind,
                pickFailureStatus);
            if (source is null)
            {
                return;
            }

            if (!CanUseFileBrowserContext(instanceUri))
            {
                return;
            }

            source = ResolveUniqueUploadSource(source, destinationEntries);
            bool refreshCurrentFolderAfterUpload = HasSameFolder(_currentFolder, destinationFolder);
            await UploadSourceToFolderAsync(
                instanceUri,
                destinationFolder,
                source,
                sourceKind,
                destination.Path,
                refreshCurrentFolderAfterUpload);
        }

        private async Task<CottonFileUploadSource?> PickUploadSourceAsync(
            Func<CancellationToken, Task<CottonFileUploadSource?>> pickSourceAsync,
            string sourceKind,
            string pickFailureStatus)
        {
            try
            {
                return await pickSourceAsync(CancellationToken.None);
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to pick a Cotton mobile upload {SourceKind}.", sourceKind);
                _display.ShowFilesStatus(pickFailureStatus);
                return null;
            }
        }

        private async Task UploadSourceToFolderAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CottonFileUploadSource source,
            string sourceKind,
            string? destinationPath,
            bool refreshCurrentFolderAfterUpload)
        {
            CancellationTokenSource fileActionCancellation = BeginFileAction(
                CottonFileUploadStatusText.CreateStartingStatus(sourceKind, source.Snapshot.Name));

            try
            {
                await _fileUploadService.UploadAsync(
                    instanceUri,
                    folder,
                    source,
                    CreateFileUploadProgress(
                        source.Snapshot,
                        () => IsActiveFileAction(fileActionCancellation, instanceUri),
                        fileActionCancellation.Token),
                    fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                if (refreshCurrentFolderAfterUpload)
                {
                    await RefreshAfterUploadAsync(
                        instanceUri,
                        folder,
                        source.Snapshot.Name,
                        sourceKind,
                        fileActionCancellation);
                }
                else
                {
                    string target = string.IsNullOrWhiteSpace(destinationPath) ? folder.Name : destinationPath;
                    _display.ShowFilesStatus(CottonFileUploadStatusText.CreateCompletedStatus(
                        sourceKind,
                        source.Snapshot.Name,
                        target));
                }
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile upload authorization failure.");
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile upload cancellation.");
                    return;
                }

                ClearFileActionRetry();
                _display.ShowFilesStatus("Upload cancelled.");
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile upload failure.");
                    return;
                }

                _logger.LogError(exception, "Failed to upload Cotton mobile file.");
                ClearFileActionRetry();
                _display.ShowFilesStatus(CreateUploadFailureStatus(exception));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh: false);
            }
        }

        private CottonFileUploadSource ResolveUniqueUploadSource(CottonFileUploadSource source)
        {
            return ResolveUniqueUploadSource(source, _display.AllFileEntries);
        }

        private static CottonFileUploadSource ResolveUniqueUploadSource(
            CottonFileUploadSource source,
            IEnumerable<CottonFileBrowserEntry> entries)
        {
            IReadOnlyList<string> existingFileNames = entries
                .Where(entry => entry.Type == CottonFileBrowserEntryType.File)
                .Select(entry => entry.Name)
                .ToArray();
            string resolvedName = CottonFileUploadNameResolver.ResolveUniqueName(
                source.Snapshot.Name,
                existingFileNames);
            return string.Equals(resolvedName, source.Snapshot.Name, StringComparison.Ordinal)
                ? source
                : source.WithSnapshot(source.Snapshot.WithName(resolvedName));
        }

        private async Task RefreshAfterUploadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            string fileName,
            string sourceKind,
            CancellationTokenSource fileActionCancellation)
        {
            if (IsDisplayingRootFolder())
            {
                await LoadRootFilesAsync(isRefresh: true);
            }
            else
            {
                await LoadFolderAsync(folder, preserveHistory: true, isRefresh: true);
            }

            if (IsActiveFileAction(fileActionCancellation, instanceUri)
                && HasSameFolder(_currentFolder, folder)
                && !_lastFileLoadFailed)
            {
                _display.ShowFilesStatus(CottonFileUploadStatusText.CreateCompletedStatus(sourceKind, fileName));
            }
        }

        private async Task RefreshAfterFolderCreatedAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            string folderName,
            CancellationTokenSource fileActionCancellation)
        {
            if (IsDisplayingRootFolder())
            {
                await LoadRootFilesAsync(isRefresh: true);
            }
            else
            {
                await LoadFolderAsync(folder, preserveHistory: true, isRefresh: true);
            }

            if (IsActiveFileAction(fileActionCancellation, instanceUri)
                && HasSameFolder(_currentFolder, folder)
                && !_lastFileLoadFailed)
            {
                _display.ShowFilesStatus(CottonFolderCreationStatusText.CreateCreatedStatus(folderName));
            }
        }

        private string CreatePath(string currentFolderName)
        {
            return string.Join(
                " / ",
                CottonFileNavigationPlanner.CreatePathSegments(
                    MainPageDisplayState.RootFilesTitle,
                    _fileNavigation,
                    currentFolderName));
        }

        private string CreateChildFolderPath(string childFolderName)
        {
            string normalizedChildName = string.IsNullOrWhiteSpace(childFolderName)
                ? "Folder"
                : childFolderName.Trim();
            string parentPath = _currentFolder is null
                ? MainPageDisplayState.RootFilesTitle
                : CreatePath(_currentFolder.Name);
            return $"{parentPath} / {normalizedChildName}";
        }

        private bool IsDisplayingRootFolder()
        {
            return !_display.CanNavigateFilesUp
                && string.Equals(
                    _display.FilesTitle,
                    MainPageDisplayState.RootFilesTitle,
                    StringComparison.Ordinal)
                && !_display.IsFilesPathVisible;
        }

        private async Task<CottonFolderContent> ApplyThumbnailsAsync(
            Uri instanceUri,
            CottonFolderContent content,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(content);

            var entries = new List<CottonFileBrowserEntry>(content.Entries.Count);
            foreach (CottonFileBrowserEntry entry in content.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CottonFileThumbnailSnapshot thumbnail = await _thumbnailProvider.GetThumbnailAsync(
                    instanceUri,
                    entry,
                    cancellationToken);
                entries.Add(entry.WithThumbnail(thumbnail));
            }

            return new CottonFolderContent(content.FolderId, content.FolderName, entries);
        }

        private async Task<CottonFolderContent> ApplyCachedThumbnailsAsync(
            Uri instanceUri,
            CottonFolderContent content,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(content);

            var entries = new List<CottonFileBrowserEntry>(content.Entries.Count);
            foreach (CottonFileBrowserEntry entry in content.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CottonFileThumbnailSnapshot thumbnail = await _thumbnailProvider.GetCachedThumbnailAsync(
                    instanceUri,
                    entry,
                    cancellationToken);
                entries.Add(entry.WithThumbnail(thumbnail));
            }

            return new CottonFolderContent(content.FolderId, content.FolderName, entries);
        }

        private async Task<CottonFolderContent> ApplyLocalFilesAsync(
            Uri instanceUri,
            CottonFolderContent content,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(content);

            IReadOnlyDictionary<Guid, CottonOfflineFilePinSnapshot> pins =
                await LoadOfflinePinsByIdAsync(instanceUri, cancellationToken);
            var entries = new List<CottonFileBrowserEntry>(content.Entries.Count);
            foreach (CottonFileBrowserEntry entry in content.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                entries.Add(CreateEntryWithLocalState(instanceUri, entry, pins));
            }

            return new CottonFolderContent(content.FolderId, content.FolderName, entries);
        }

        private async Task<IReadOnlyDictionary<Guid, CottonOfflineFilePinSnapshot>> LoadOfflinePinsByIdAsync(
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CottonOfflineFilePinSnapshot> pins =
                await _offlineFilePinStore.LoadAsync(instanceUri, cancellationToken);
            return pins
                .GroupBy(pin => pin.FileId)
                .ToDictionary(group => group.Key, group => group.First());
        }

        private CottonFileBrowserEntry CreateEntryWithLocalState(
            Uri instanceUri,
            CottonFileBrowserEntry entry,
            IReadOnlyDictionary<Guid, CottonOfflineFilePinSnapshot> pins)
        {
            if (entry.Type != CottonFileBrowserEntryType.File)
            {
                return entry;
            }

            if (pins.TryGetValue(entry.Id, out CottonOfflineFilePinSnapshot? pin))
            {
                CottonLocalFileSnapshot? localFile = _fileBrowserService.GetLocalDownload(instanceUri, entry);
                CottonOfflineFileAvailabilitySnapshot availability =
                    CottonOfflineFileAvailabilitySnapshot.Create(entry, pin, localFile);
                CottonFileBrowserEntry updatedEntry = entry.WithOfflineAvailability(availability);
                return availability.IsAvailable && localFile is not null
                    ? updatedEntry.WithLocalFile(localFile)
                    : updatedEntry.WithoutLocalFile();
            }

            CottonLocalFileSnapshot? reusableLocalFile =
                _fileBrowserService.GetReusableLocalDownloadSnapshot(instanceUri, entry);
            CottonFileBrowserEntry unpinnedEntry =
                entry.WithOfflineAvailability(CottonOfflineFileAvailabilitySnapshot.NotPinned);
            return reusableLocalFile is null ? unpinnedEntry.WithoutLocalFile() : unpinnedEntry.WithLocalFile(reusableLocalFile);
        }

        private async Task OpenFileAsync(CottonFileBrowserEntry file)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to open files.");
                return;
            }

            if (ShowOfflineUnavailableRetryIfNeeded(MainPageFileAction.Open, file, OfflineOpenStatus))
            {
                return;
            }

            CancellationTokenSource fileActionCancellation = BeginFileAction($"Opening {file.Name}...");
            bool shouldRunRecoveryRefresh = false;

            try
            {
                CottonFileDownloadResult result = await PrepareFileForOpenOrShareAsync(
                    instanceUri,
                    file,
                    "Opening",
                    () => IsActiveFileAction(fileActionCancellation, instanceUri),
                    fileActionCancellation.Token);
                if (_filePreviewService.CanPreview(file, result))
                {
                    await _filePreviewService.OpenAsync(file, result, fileActionCancellation.Token);
                }
                else
                {
                    await _fileInteractionService.OpenAsync(result, fileActionCancellation.Token);
                }

                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                _display.ShowFilesSummary();
                shouldRunRecoveryRefresh = true;
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile open authorization failure {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile open cancellation {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                if (ShowReusableLocalFileIfAvailable(file))
                {
                    _display.ShowFilesSummary();
                    return;
                }

                _display.ShowFilesStatus("Open cancelled.");
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile open failure {FileId}.", file.Id);
                    return;
                }

                ClearLocalFileMarkerIfFileMissing(exception, file);
                _logger.LogError(exception, "Failed to open Cotton mobile file {FileId}.", file.Id);
                ShowFileActionRetry(
                    MainPageFileAction.Open,
                    file,
                    CreateOpenFileActionFailureStatus(file, exception));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh);
            }
        }

        private async Task ShareFileAsync(CottonFileBrowserEntry file)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to share files.");
                return;
            }

            if (ShowOfflineUnavailableRetryIfNeeded(MainPageFileAction.Share, file, OfflineShareStatus))
            {
                return;
            }

            CancellationTokenSource fileActionCancellation = BeginFileAction($"Preparing {file.Name}...");
            bool shouldRunRecoveryRefresh = false;

            try
            {
                CottonFileDownloadResult result = await PrepareFileForOpenOrShareAsync(
                    instanceUri,
                    file,
                    "Preparing",
                    () => IsActiveFileAction(fileActionCancellation, instanceUri),
                    fileActionCancellation.Token);
                await _fileInteractionService.ShareAsync(result, fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                _display.ShowFilesSummary();
                shouldRunRecoveryRefresh = true;
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile share authorization failure {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile share cancellation {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                if (ShowReusableLocalFileIfAvailable(file))
                {
                    _display.ShowFilesSummary();
                    return;
                }

                _display.ShowFilesStatus("Share cancelled.");
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile share failure {FileId}.", file.Id);
                    return;
                }

                ClearLocalFileMarkerIfFileMissing(exception, file);
                _logger.LogError(exception, "Failed to share Cotton mobile file {FileId}.", file.Id);
                ShowFileActionRetry(
                    MainPageFileAction.Share,
                    file,
                    CreateFileActionFailureStatus(exception, "Share failed.", OfflineShareStatus));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh);
            }
        }

        private Task CopyCloudShareLinkAsync(CottonFileBrowserEntry entry)
        {
            return CreateCloudShareLinkAsync(
                entry,
                MainPageFileAction.CopyLink,
                async (snapshot, cancellationToken) =>
                {
                    _display.ShowFileActionLoading(
                        CottonCloudShareLinkStatusText.CreateCopyingStatus(entry.Name));
                    await _cloudShareLinkInteractionService.CopyAsync(snapshot, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    _display.ShowFilesStatus(CottonCloudShareLinkStatusText.CreateCopiedStatus(entry.Name));
                },
                CottonCloudShareLinkStatusText.CopyFailedStatus);
        }

        private Task ShareCloudShareLinkAsync(CottonFileBrowserEntry entry)
        {
            return CreateCloudShareLinkAsync(
                entry,
                MainPageFileAction.ShareLink,
                async (snapshot, cancellationToken) =>
                {
                    _display.ShowFileActionLoading(
                        CottonCloudShareLinkStatusText.CreateSharingStatus(entry.Name));
                    await _cloudShareLinkInteractionService.ShareAsync(
                        snapshot,
                        CreateCloudShareLinkShareTitle(entry),
                        cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    _display.ShowFilesSummary();
                },
                CottonCloudShareLinkStatusText.ShareFailedStatus);
        }

        private Task CopySelectionCloudShareLinksAsync(IReadOnlyList<CottonFileBrowserEntry> entries)
        {
            return UseSelectionCloudShareLinksAsync(
                entries,
                async (links, cancellationToken) =>
                {
                    _display.ShowFileActionLoading(CottonCloudShareLinkStatusText.CreateCopyingStatus(links.Count));
                    await _cloudShareLinkInteractionService.CopyAsync(links, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    _display.ShowFilesStatus(CottonCloudShareLinkStatusText.CreateCopiedStatus(links.Count));
                },
                CottonCloudShareLinkStatusText.CopyFailedStatus);
        }

        private Task ShareSelectionCloudShareLinksAsync(IReadOnlyList<CottonFileBrowserEntry> entries)
        {
            return UseSelectionCloudShareLinksAsync(
                entries,
                async (links, cancellationToken) =>
                {
                    _display.ShowFileActionLoading(CottonCloudShareLinkStatusText.CreateSharingStatus(links.Count));
                    await _cloudShareLinkInteractionService.ShareAsync(
                        links,
                        links.Count == 1 ? "Share link" : $"Share {links.Count} links",
                        cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    _display.ShowFilesSummary();
                },
                CottonCloudShareLinkStatusText.ShareFailedStatus);
        }

        private async Task UseSelectionCloudShareLinksAsync(
            IReadOnlyList<CottonFileBrowserEntry> entries,
            Func<IReadOnlyList<CottonCloudShareLinkSnapshot>, CancellationToken, Task> useLinksAsync,
            string interactionFailureStatus)
        {
            ArgumentNullException.ThrowIfNull(entries);
            ArgumentNullException.ThrowIfNull(useLinksAsync);
            ArgumentException.ThrowIfNullOrWhiteSpace(interactionFailureStatus);

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to create links.");
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                _display.ShowFilesStatus(CottonCloudShareLinkStatusText.OfflineUnavailableStatus);
                return;
            }

            CottonFileBrowserEntry[] selectedEntries = entries
                .Select(entry => GetCurrentVisibleEntry(entry, instanceUri))
                .Where(entry => entry is not null)
                .Select(entry => entry!)
                .ToArray();
            if (selectedEntries.Length == 0)
            {
                _display.ShowFilesStatus("Selection is no longer available.");
                return;
            }

            CottonCloudShareLinkExpirationOption? expiration = await SelectCloudShareLinkExpirationAsync();
            if (expiration is null)
            {
                _display.ShowFilesStatus(CottonCloudShareLinkStatusText.CancelledStatus);
                return;
            }

            CancellationTokenSource fileActionCancellation = BeginFileAction(
                CottonCloudShareLinkStatusText.CreateCreatingStatus(selectedEntries.Length));
            bool didCreateLink = false;

            try
            {
                var links = new List<CottonCloudShareLinkSnapshot>();
                for (int index = 0; index < selectedEntries.Length; index++)
                {
                    fileActionCancellation.Token.ThrowIfCancellationRequested();
                    if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                    {
                        return;
                    }

                    _display.ShowFileActionLoading(
                        CottonCloudShareLinkStatusText.CreateCreatingItemStatus(index + 1, selectedEntries.Length));
                    CottonCloudShareLinkSnapshot link = await _cloudShareLinkService.CreateAsync(
                        instanceUri,
                        CreateCloudShareLinkRequest(selectedEntries[index], expiration.ExpireAfterMinutes),
                        fileActionCancellation.Token);
                    didCreateLink = true;
                    links.Add(link);
                }

                if (links.Count == 0)
                {
                    _display.ShowFilesStatus("Selection is no longer available.");
                    return;
                }

                await useLinksAsync(links, fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile selection share-link authorization failure.");
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile selection share-link cancellation.");
                    return;
                }

                ClearFileActionRetry();
                _display.ShowFilesStatus(CottonCloudShareLinkStatusText.CancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile selection share-link failure.");
                    return;
                }

                _logger.LogError(exception, "Failed to create or use Cotton mobile selection share links.");
                _display.ShowFilesStatus(
                    didCreateLink
                        ? interactionFailureStatus
                        : CreateBulkCloudShareLinkCreationFailureStatus(exception));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh: false);
            }
        }

        private async Task CreateCloudShareLinkAsync(
            CottonFileBrowserEntry entry,
            MainPageFileAction retryAction,
            Func<CottonCloudShareLinkSnapshot, CancellationToken, Task> useLinkAsync,
            string interactionFailureStatus)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to create links.");
                return;
            }

            if (ShowOfflineRetryIfNeeded(
                retryAction,
                entry,
                CottonCloudShareLinkStatusText.OfflineUnavailableStatus))
            {
                return;
            }

            CottonCloudShareLinkTargetKind targetKind = CreateCloudShareLinkTargetKind(entry);
            CottonCloudShareLinkExpirationOption? expiration = await SelectCloudShareLinkExpirationAsync();
            if (expiration is null)
            {
                _display.ShowFilesStatus(CottonCloudShareLinkStatusText.CancelledStatus);
                return;
            }

            CancellationTokenSource fileActionCancellation = BeginFileAction(
                CottonCloudShareLinkStatusText.CreateCreatingStatus(entry.Name));
            bool didCreateLink = false;

            try
            {
                CottonCloudShareLinkSnapshot snapshot = await _cloudShareLinkService.CreateAsync(
                    instanceUri,
                    CreateCloudShareLinkRequest(entry, expiration.ExpireAfterMinutes),
                    fileActionCancellation.Token);
                didCreateLink = true;
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                await useLinkAsync(snapshot, fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile share-link authorization failure {EntryId}.", entry.Id);
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile share-link cancellation {EntryId}.", entry.Id);
                    return;
                }

                ClearFileActionRetry();
                _display.ShowFilesStatus(CottonCloudShareLinkStatusText.CancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile share-link failure {EntryId}.", entry.Id);
                    return;
                }

                _logger.LogError(exception, "Failed to create or use Cotton mobile share link {EntryId}.", entry.Id);
                ShowFileActionRetry(
                    retryAction,
                    entry,
                    didCreateLink
                        ? interactionFailureStatus
                        : CreateCloudShareLinkCreationFailureStatus(targetKind, exception));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh: false);
            }
        }

        private async Task ShowFileDetailsAsync(CottonFileBrowserEntry file)
        {
            CottonLocalFileSnapshot? localFile = _instanceUri is null
                ? null
                : _fileBrowserService.GetLocalDownload(_instanceUri, file);
            if (localFile is null || !IsReusableLocalFile(file, localFile))
            {
                _display.ClearFileLocalCopy(file);
            }

            CottonFileDetailsDisplayState details = CottonFileDetailsDisplayState.Create(
                file,
                localFile,
                TimeZoneInfo.Local);

            await _dialogService.ShowAlertAsync(
                details.Title,
                details.Message,
                "OK");
        }

        private async Task<CottonFileDownloadResult> PrepareFileForOpenOrShareAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            string actionName,
            Func<bool> canUpdateProgress,
            CancellationToken cancellationToken)
        {
            CottonFileDownloadResult? localFile = _fileBrowserService.GetReusableLocalDownload(instanceUri, file);
            if (localFile is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ShowReusableLocalFileIfAvailable(file);
                return localFile;
            }

            CottonFileDownloadResult downloadedFile = await _fileBrowserService.DownloadAsync(
                instanceUri,
                file,
                CreateFileDownloadProgress(file, actionName, canUpdateProgress, cancellationToken),
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshLocalFileStateAsync(instanceUri, cancellationToken);
            return downloadedFile;
        }

        private static bool IsReusableLocalFile(
            CottonFileBrowserEntry file,
            CottonLocalFileSnapshot localFile)
        {
            return (!file.SizeBytes.HasValue || file.SizeBytes.Value == localFile.SizeBytes)
                && CottonLocalFileFreshness.IsFresh(localFile.UpdatedAtUtc, file.UpdatedAtUtc);
        }

        private async Task<bool> ShowDownloadedFileActionsAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            Func<bool> canUseAction,
            CancellationToken cancellationToken)
        {
            string openAction = CreateOpenAction(file, downloadedFile);
            string? action = await _dialogService.ShowActionSheetAsync(
                $"Downloaded {downloadedFile.FileName}",
                DoneAction,
                null,
                openAction,
                ShareFileAction);

            cancellationToken.ThrowIfCancellationRequested();
            if (!canUseAction())
            {
                return false;
            }

            if (string.Equals(action, openAction, StringComparison.Ordinal))
            {
                return await OpenDownloadedFileAsync(file, downloadedFile, canUseAction, cancellationToken);
            }

            switch (action)
            {
                case ShareFileAction:
                    return await ShareDownloadedFileAsync(file, downloadedFile, canUseAction, cancellationToken);
            }

            _display.ShowFilesSummary();
            return true;
        }

        private async Task<bool> ShowDownloadedFileActionsBestEffortAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            Func<bool> canUseAction,
            CancellationToken cancellationToken)
        {
            try
            {
                return await ShowDownloadedFileActionsAsync(file, downloadedFile, canUseAction, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (!canUseAction())
                {
                    _logger.LogDebug(
                        exception,
                        "Ignored stale Cotton mobile downloaded file actions failure {FileId}.",
                        file.Id);
                    return false;
                }

                _logger.LogWarning(exception, "Failed to show Cotton mobile downloaded file actions {FileId}.", file.Id);
                _display.ShowFilesStatus("Downloaded. Could not show file actions.");
                return false;
            }
        }

        private string CreateOpenAction(CottonFileBrowserEntry file)
        {
            return CottonFileOpenRouter.CreateRoute(file).ActionLabel;
        }

        private async Task<CottonCloudShareLinkExpirationOption?> SelectCloudShareLinkExpirationAsync()
        {
            IReadOnlyList<CottonCloudShareLinkExpirationOption> options =
                CottonCloudShareLinkExpirationCatalog.CreateOptions();
            string? action = await _dialogService.ShowActionSheetAsync(
                LinkExpirationTitle,
                CancelAction,
                null,
                options.Select(option => option.Label).ToArray());
            return CottonCloudShareLinkExpirationCatalog.FindByLabel(action);
        }

        private static CottonCloudShareLinkRequest CreateCloudShareLinkRequest(
            CottonFileBrowserEntry entry,
            int expireAfterMinutes)
        {
            return entry.IsFolder
                ? CottonCloudShareLinkRequest.ForFolder(entry.Id, expireAfterMinutes)
                : CottonCloudShareLinkRequest.ForFile(entry.Id, expireAfterMinutes);
        }

        private static CottonCloudShareLinkTargetKind CreateCloudShareLinkTargetKind(CottonFileBrowserEntry entry)
        {
            return entry.IsFolder
                ? CottonCloudShareLinkTargetKind.Folder
                : CottonCloudShareLinkTargetKind.File;
        }

        private static string CreateCloudShareLinkShareTitle(CottonFileBrowserEntry entry)
        {
            return $"Share link to {entry.Name}";
        }

        private string CreateCloudShareLinkCreationFailureStatus(
            CottonCloudShareLinkTargetKind targetKind,
            Exception exception)
        {
            HttpStatusCode? statusCode = exception is CottonApiException apiException
                ? apiException.StatusCode
                : null;

            return CottonCloudShareLinkStatusText.CreateCreationFailedStatus(
                targetKind,
                statusCode,
                _networkAccess.HasInternetAccess);
        }

        private string CreateBulkCloudShareLinkCreationFailureStatus(Exception exception)
        {
            HttpStatusCode? statusCode = exception is CottonApiException apiException
                ? apiException.StatusCode
                : null;

            return CottonCloudShareLinkStatusText.CreateBulkCreationFailedStatus(
                statusCode,
                _networkAccess.HasInternetAccess);
        }

        private static string CreateDownloadAction(CottonFileBrowserEntry file)
        {
            return file.HasLocalCopy ? DownloadAgainAction : DownloadAction;
        }

        private string CreateOpenAction(CottonFileBrowserEntry file, Uri instanceUri)
        {
            CottonLocalFileSnapshot? localFile = _fileBrowserService.GetReusableLocalDownloadSnapshot(instanceUri, file);
            return localFile is null
                ? CreateOpenAction(file)
                : CreateOpenAction(file, localFile);
        }

        private string CreateOpenAction(
            CottonFileBrowserEntry file,
            CottonLocalFileSnapshot localFile)
        {
            return CottonFileOpenRouter.CreateRoute(file, localFile.SizeBytes).ActionLabel;
        }

        private string CreateOpenAction(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile)
        {
            return CottonFileOpenRouter.CreateRoute(file, downloadedFile.SizeBytes).ActionLabel;
        }

        private async Task<bool> OpenDownloadedFileAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            Func<bool> canUseAction,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!canUseAction())
            {
                return false;
            }

            _display.ShowFileActionLoading($"Opening {file.Name}...");

            try
            {
                if (_filePreviewService.CanPreview(file, downloadedFile))
                {
                    await _filePreviewService.OpenAsync(file, downloadedFile, cancellationToken);
                }
                else
                {
                    await _fileInteractionService.OpenAsync(downloadedFile, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!canUseAction())
                {
                    return false;
                }

                _display.ShowFilesSummary();
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (!canUseAction())
                {
                    _logger.LogDebug(
                        exception,
                        "Ignored stale Cotton mobile downloaded file open failure {FileId}.",
                        file.Id);
                    return false;
                }

                ClearLocalFileMarkerIfFileMissing(exception, file);
                _logger.LogError(exception, "Failed to open downloaded Cotton mobile file {FileId}.", file.Id);
                ShowFileActionRetry(
                    MainPageFileAction.Open,
                    file,
                    CreateOpenFileActionFailureStatus(file, exception));
                return false;
            }
        }

        private async Task<bool> ShareDownloadedFileAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            Func<bool> canUseAction,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!canUseAction())
            {
                return false;
            }

            _display.ShowFileActionLoading($"Sharing {file.Name}...");

            try
            {
                await _fileInteractionService.ShareAsync(downloadedFile, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!canUseAction())
                {
                    return false;
                }

                _display.ShowFilesSummary();
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (!canUseAction())
                {
                    _logger.LogDebug(
                        exception,
                        "Ignored stale Cotton mobile downloaded file share failure {FileId}.",
                        file.Id);
                    return false;
                }

                ClearLocalFileMarkerIfFileMissing(exception, file);
                _logger.LogError(exception, "Failed to share downloaded Cotton mobile file {FileId}.", file.Id);
                ShowFileActionRetry(
                    MainPageFileAction.Share,
                    file,
                    CreateFileActionFailureStatus(exception, "Share failed.", OfflineShareStatus));
                return false;
            }
        }

        private bool ShowReusableLocalFileIfAvailable(CottonFileBrowserEntry file)
        {
            if (_instanceUri is null)
            {
                return false;
            }

            CottonLocalFileSnapshot? localFile = _fileBrowserService.GetReusableLocalDownloadSnapshot(_instanceUri, file);
            if (localFile is null)
            {
                _display.ClearFileLocalCopy(file);
                return false;
            }

            _display.ShowFileLocalCopy(file, localFile);
            return true;
        }

        private async Task<bool> ShowReusableLocalFileIfAvailableAsync(CottonFileBrowserEntry file)
        {
            if (_instanceUri is null)
            {
                return false;
            }

            CottonLocalFileSnapshot? localFile = _fileBrowserService.GetReusableLocalDownloadSnapshot(_instanceUri, file);
            await RefreshLocalFileStateAsync(_instanceUri, CancellationToken.None);
            return localFile is not null;
        }

        private async Task<bool> RefreshLocalFileStateAsync(Uri instanceUri, CancellationToken cancellationToken)
        {
            if (!Uri.Equals(_instanceUri, instanceUri))
            {
                return false;
            }

            IReadOnlyDictionary<Guid, CottonOfflineFilePinSnapshot> pins =
                await LoadOfflinePinsByIdAsync(instanceUri, cancellationToken);
            if (!Uri.Equals(_instanceUri, instanceUri))
            {
                return false;
            }

            return _display.RefreshFileLocalStates(entry => CreateEntryWithLocalState(instanceUri, entry, pins));
        }

        private void ClearLocalFileMarkerIfFileMissing(Exception exception, CottonFileBrowserEntry file)
        {
            if (exception is FileNotFoundException)
            {
                _display.ClearFileLocalCopy(file);
            }
        }

        private IProgress<long>? CreateFileDownloadProgress(
            CottonFileBrowserEntry file,
            string actionName,
            Func<bool> canUpdateProgress,
            CancellationToken cancellationToken)
        {
            if (file.SizeBytes is not > 0)
            {
                return null;
            }

            long totalBytes = file.SizeBytes.Value;
            int lastPercent = -1;
            return new Progress<long>(downloadedBytes =>
            {
                if (cancellationToken.IsCancellationRequested || !canUpdateProgress())
                {
                    return;
                }

                int percent = (int)Math.Min(100d, Math.Floor(downloadedBytes / (double)totalBytes * 100d));
                if (percent == lastPercent)
                {
                    return;
                }

                lastPercent = percent;
                _display.ShowFileActionLoading($"{actionName} {file.Name}... {percent}%");
            });
        }

        private IProgress<long> CreateFileUploadProgress(
            CottonFileUploadSourceSnapshot source,
            Func<bool> canUpdateProgress,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);

            string? lastStatus = null;
            return new Progress<long>(uploadedBytes =>
            {
                if (cancellationToken.IsCancellationRequested || !canUpdateProgress())
                {
                    return;
                }

                string status = new CottonFileUploadProgressSnapshot(source, uploadedBytes).StatusText;
                if (string.Equals(status, lastStatus, StringComparison.Ordinal))
                {
                    return;
                }

                lastStatus = status;
                _display.ShowFileActionLoading(status);
            });
        }

        private CancellationTokenSource BeginFileAction(string status)
        {
            CancelCurrentFileAction();
            ClearFileActionRetry();
            var cancellation = new CancellationTokenSource();
            _fileActionCancellation = cancellation;
            _display.ShowFileActionLoading(status);
            return cancellation;
        }

        private CancellationTokenSource BeginFileLoad()
        {
            CancelCurrentFileLoad();
            var cancellation = new CancellationTokenSource();
            _fileLoadCancellation = cancellation;
            _isFileLoadInProgress = true;
            return cancellation;
        }

        private void EndFileAction(CancellationTokenSource cancellation, bool shouldRunRecoveryRefresh)
        {
            if (!ReferenceEquals(_fileActionCancellation, cancellation))
            {
                cancellation.Dispose();
                return;
            }

            _fileActionCancellation = null;
            cancellation.Dispose();
            if (shouldRunRecoveryRefresh && string.IsNullOrWhiteSpace(_fileLoadRecoveryPendingAfterBusyReason))
            {
                QueueFileLoadRecoveryRefreshAfterFileAction();
            }

            QueuePendingFileLoadRecoveryRefreshAfterBusy();
        }

        private void EndFileLoad(CancellationTokenSource cancellation)
        {
            if (!ReferenceEquals(_fileLoadCancellation, cancellation))
            {
                cancellation.Dispose();
                return;
            }

            _fileLoadCancellation = null;
            _isFileLoadInProgress = false;
            cancellation.Dispose();
            QueuePendingFileLoadRecoveryRefreshAfterBusy();
        }

        private void CancelCurrentFileAction()
        {
            CancellationTokenSource? cancellation = _fileActionCancellation;
            if (cancellation is null)
            {
                return;
            }

            _fileActionCancellation = null;
            cancellation.Cancel();
        }

        private void CancelCurrentFileLoad()
        {
            CancellationTokenSource? cancellation = _fileLoadCancellation;
            if (cancellation is null)
            {
                return;
            }

            _fileLoadCancellation = null;
            cancellation.Cancel();
        }

        private bool IsActiveFileLoad(CancellationTokenSource cancellation, Uri instanceUri)
        {
            return ReferenceEquals(_fileLoadCancellation, cancellation)
                && Uri.Equals(_instanceUri, instanceUri);
        }

        private bool IsActiveFileAction(CancellationTokenSource cancellation, Uri instanceUri)
        {
            return ReferenceEquals(_fileActionCancellation, cancellation)
                && Uri.Equals(_instanceUri, instanceUri);
        }

        private void ShowFileActionRetry(
            MainPageFileAction action,
            CottonFileBrowserEntry entry,
            string status)
        {
            _retryFileAction = action;
            _retryFileActionEntry = entry;
            _display.ShowFileActionRetry(status);
        }

        private void ShowFileLoadFailure(string fallbackStatus)
        {
            _lastFileLoadFailed = true;
            _lastFileLoadDisplayedCachedContent = false;
            if (_networkAccess.HasInternetAccess)
            {
                _display.ShowFilesStatus(fallbackStatus);
                return;
            }

            _display.ShowOfflineFilesNotice();
        }

        private bool ShowCurrentOfflineNoticeIfNeeded()
        {
            if (_networkAccess.HasInternetAccess || _display.TotalFileEntryCount == 0)
            {
                return false;
            }

            _lastFileLoadFailed = true;
            _lastFileLoadDisplayedCachedContent = false;
            _display.ShowOfflineFilesNotice();
            return true;
        }

        private bool ShouldRestoreNavigationAfterFileLoadFailure()
        {
            return _lastFileLoadFailed
                && !_lastFileLoadDisplayedCachedContent
                && _instanceUri is not null;
        }

        private bool ShouldRestoreNavigationAfterUnchangedFolder(
            CottonFolderHandle? originalFolder,
            IReadOnlyList<CottonFolderHandle> originalNavigation)
        {
            return _instanceUri is not null
                && HasSameFolder(_currentFolder, originalFolder)
                && !HasSameNavigation(originalNavigation);
        }

        private void RestoreNavigation(
            CottonFolderHandle? originalFolder,
            IEnumerable<CottonFolderHandle> originalNavigation,
            string originalSearchText,
            bool originalSearchOpen)
        {
            _currentFolder = originalFolder;
            _fileNavigation.Clear();
            _fileNavigation.AddRange(originalNavigation);
            _display.RestoreFileSearch(originalSearchText, originalSearchOpen);
        }

        private static bool HasSameFolder(CottonFolderHandle? current, CottonFolderHandle? expected)
        {
            if (current is null || expected is null)
            {
                return current is null && expected is null;
            }

            return current.Id == expected.Id;
        }

        private bool HasSameNavigation(IReadOnlyList<CottonFolderHandle> expectedNavigation)
        {
            if (_fileNavigation.Count != expectedNavigation.Count)
            {
                return false;
            }

            for (int index = 0; index < _fileNavigation.Count; index++)
            {
                if (_fileNavigation[index].Id != expectedNavigation[index].Id)
                {
                    return false;
                }
            }

            return true;
        }

        private void NetworkAccess_InternetAccessRestored(object? sender, EventArgs e)
        {
            QueueFileLoadRecoveryRefresh("internet access restored");
        }

        private void Display_FileSearchTextChanged(object? sender, EventArgs e)
        {
            ClearFileActionRetry();
        }

        private void ForegroundService_Resumed(object? sender, EventArgs e)
        {
            if (!_networkAccess.HasInternetAccess)
            {
                return;
            }

            QueueFileLoadRecoveryRefresh("foreground resume");
        }

        private void QueueFileLoadRecoveryRefresh(string reason)
        {
            if (CanRunRecoveryRefresh())
            {
                _isRecoveryRefreshInProgress = true;
                _ = RunQueuedFileLoadRecoveryRefreshAsync(reason);
                return;
            }

            if (IsFileBrowserBusy() && _instanceUri is not null)
            {
                _fileLoadRecoveryPendingAfterBusyReason = reason;
                return;
            }
        }

        private void QueueFileLoadRecoveryRefreshAfterFileAction()
        {
            if (!_networkAccess.HasInternetAccess)
            {
                return;
            }

            QueueFileLoadRecoveryRefresh("file action completed");
        }

        private async Task RunQueuedFileLoadRecoveryRefreshAsync(string reason)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => RefreshAfterFileLoadRecoveryAsync(reason));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to queue Cotton mobile files recovery refresh after {Reason}.", reason);
                _isRecoveryRefreshInProgress = false;
            }
        }

        private async Task RefreshAfterFileLoadRecoveryAsync(string reason)
        {
            try
            {
                if (!CanRunQueuedRecoveryRefresh())
                {
                    return;
                }

                _logger.LogInformation("Refreshing Cotton mobile files after {Reason}.", reason);
                await RefreshAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to refresh Cotton mobile files after {Reason}.", reason);
            }
            finally
            {
                _isRecoveryRefreshInProgress = false;
            }
        }

        private bool CanRunRecoveryRefresh()
        {
            return CanRunQueuedRecoveryRefresh()
                && !_isRecoveryRefreshInProgress;
        }

        private bool CanRunQueuedRecoveryRefresh()
        {
            return _lastFileLoadFailed
                && _instanceUri is not null
                && !IsFileBrowserBusy();
        }

        private void QueuePendingFileLoadRecoveryRefreshAfterBusy()
        {
            if (IsFileBrowserBusy())
            {
                return;
            }

            string? reason = _fileLoadRecoveryPendingAfterBusyReason;
            if (string.IsNullOrWhiteSpace(reason))
            {
                return;
            }

            _fileLoadRecoveryPendingAfterBusyReason = null;
            if (!_networkAccess.HasInternetAccess)
            {
                return;
            }

            QueueFileLoadRecoveryRefresh(reason);
        }

        private bool IsFileBrowserBusy()
        {
            return _isFolderNavigationInProgress
                || _isFileLoadInProgress
                || _fileActionCancellation is not null
                || _display.IsFilesLoading;
        }

        private void StopExternalRefreshIfNoLoadInProgress()
        {
            if (!_isFileLoadInProgress && _display.IsFilesRefreshing)
            {
                _display.StopFilesRefreshing();
            }
        }

        private bool ShowOfflineUnavailableRetryIfNeeded(
            MainPageFileAction action,
            CottonFileBrowserEntry file,
            string offlineStatus)
        {
            if (_networkAccess.HasInternetAccess)
            {
                return false;
            }

            if (ShowReusableLocalFileIfAvailable(file))
            {
                return false;
            }

            ClearFileActionRetry();
            ShowFileActionRetry(action, file, offlineStatus);
            return true;
        }

        private bool ShowOfflineRetryIfNeeded(
            MainPageFileAction action,
            CottonFileBrowserEntry file,
            string offlineStatus)
        {
            if (_networkAccess.HasInternetAccess)
            {
                return false;
            }

            ClearFileActionRetry();
            ShowFileActionRetry(action, file, offlineStatus);
            return true;
        }

        private string CreateFileActionFailureStatus(
            Exception exception,
            string fallbackStatus,
            string offlineStatus)
        {
            if (!_networkAccess.HasInternetAccess)
            {
                return offlineStatus;
            }

            return exception is FileOpenUnavailableException
                ? OpenUnavailableStatus
                : fallbackStatus;
        }

        private string CreateOpenFileActionFailureStatus(
            CottonFileBrowserEntry file,
            Exception exception)
        {
            if (!_networkAccess.HasInternetAccess)
            {
                return OfflineOpenStatus;
            }

            return exception is FileOpenUnavailableException
                ? CottonFileOpenRouter.CreateRoute(file).UnavailableStatus
                : "Open failed.";
        }

        private string CreateUploadFailureStatus(Exception exception)
        {
            if (!_networkAccess.HasInternetAccess)
            {
                return OfflineUploadStatus;
            }

            if (exception is CottonApiException apiException
                && apiException.StatusCode is HttpStatusCode httpStatusCode)
            {
                int statusCode = (int)httpStatusCode;
                if (statusCode == PayloadTooLargeStatusCode)
                {
                    return "Upload failed. File is too large.";
                }

                if (statusCode == InsufficientStorageStatusCode)
                {
                    return "Upload failed. Storage quota is full.";
                }

                if (httpStatusCode is HttpStatusCode.BadRequest
                    or HttpStatusCode.Conflict
                    or HttpStatusCode.UnprocessableEntity)
                {
                    return "Upload failed. Server rejected this file.";
                }
            }

            return "Upload failed.";
        }

        private string CreateFolderCreationFailureStatus(Exception exception)
        {
            if (!_networkAccess.HasInternetAccess)
            {
                return CottonFolderCreationStatusText.OfflineStatus;
            }

            if (exception is CottonApiException apiException
                && apiException.StatusCode is HttpStatusCode.Conflict)
            {
                return CottonFolderCreationStatusText.DuplicateStatus;
            }

            return CottonFolderCreationStatusText.FailedStatus;
        }

        private static string CreateCurrentActionLabel(string label, bool isCurrent)
        {
            return isCurrent ? CurrentActionPrefix + label : label;
        }

        private static string? NormalizeAction(string? action)
        {
            if (action is null)
            {
                return null;
            }

            return action.StartsWith(CurrentActionPrefix, StringComparison.Ordinal)
                ? action[CurrentActionPrefix.Length..]
                : action;
        }

        private void ClearFileActionRetry()
        {
            _retryFileAction = null;
            _retryFileActionEntry = null;
            _display.ClearFileActionRetry();
        }

        private async Task HandleSessionExpiredAsync(Exception exception)
        {
            Uri? expiredInstanceUri = _instanceUri;
            _logger.LogWarning(exception, "Cotton mobile file browser session expired.");
            Clear();
            await _sessionHandler.HandleFileBrowserSessionExpiredAsync(expiredInstanceUri);
        }

        private static bool IsAuthorizationFailure(Exception exception)
        {
            return exception is CottonApiException
            {
                StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            };
        }
    }
}
