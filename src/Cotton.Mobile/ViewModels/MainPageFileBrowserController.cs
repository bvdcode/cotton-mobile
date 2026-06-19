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
        private const string DetailsAction = "Details";
        private const string DoneAction = "Done";
        private const string DownloadAction = "Download";
        private const string DownloadAgainAction = "Download again";
        private const string KeepOfflineAction = "Keep offline";
        private const string OpenAction = "Open";
        private const string OpenWithSystemAppAction = "Open with system app";
        private const string RemoveOfflineAction = "Remove offline";
        private const string ShareAction = "Share";
        private const string UploadPhotoAction = "Upload photo";
        private const string UploadPhotoToFolderAction = "Upload photo to folder";
        private const string UploadVideoAction = "Upload video";
        private const string UploadVideoToFolderAction = "Upload video to folder";
        private const string UploadFileAction = "Upload file";
        private const string SortNameAction = "Name";
        private const string SortUpdatedAction = "Updated";
        private const string SortSizeAction = "Size";
        private const string SortTypeAction = "Type";
        private const string ViewListAction = "List";
        private const string ViewTilesAction = "Tiles";
        private const string CurrentActionSuffix = " (current)";
        private const string OfflineBrowseStatus = "Offline. Files marked On device can still open.";
        private const string OfflineDownloadStatus = "Offline. Download needs internet.";
        private const string OfflineOpenStatus = "Offline. This file is not available on device.";
        private const string OfflineShareStatus = "Offline. This file is not available on device.";
        private const string OfflineUploadStatus = "Offline. Upload needs internet.";
        private const string OpenUnavailableStatus = "No app can open this file.";
        private const int PayloadTooLargeStatusCode = 413;
        private const int InsufficientStorageStatusCode = 507;

        private readonly MainPageDisplayState _display;
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly ICottonFileUploadService _fileUploadService;
        private readonly ICottonFolderContentCache _folderContentCache;
        private readonly ICottonOfflineFilePinStore _offlineFilePinStore;
        private readonly IFileBrowserPreferenceStore _preferenceStore;
        private readonly IFileUploadPickerService _fileUploadPickerService;
        private readonly IPhotoUploadPickerService _photoUploadPickerService;
        private readonly IVideoUploadPickerService _videoUploadPickerService;
        private readonly IUploadDestinationPickerPageService _uploadDestinationPickerPageService;
        private readonly IFileInteractionService _fileInteractionService;
        private readonly IFilePreviewService _filePreviewService;
        private readonly IFileThumbnailProvider _thumbnailProvider;
        private readonly INetworkAccessService _networkAccess;
        private readonly IApplicationForegroundService _foregroundService;
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
        private bool _isFileLoadInProgress;
        private bool _isFolderNavigationInProgress;
        private bool _lastFileLoadFailed;
        private bool _lastFileLoadDisplayedCachedContent;
        private bool _isRecoveryRefreshInProgress;
        private string? _fileLoadRecoveryPendingAfterBusyReason;

        public MainPageFileBrowserController(
            MainPageDisplayState display,
            ICottonFileBrowserService fileBrowserService,
            ICottonFileUploadService fileUploadService,
            ICottonFolderContentCache folderContentCache,
            ICottonOfflineFilePinStore offlineFilePinStore,
            IFileBrowserPreferenceStore preferenceStore,
            IFileUploadPickerService fileUploadPickerService,
            IPhotoUploadPickerService photoUploadPickerService,
            IVideoUploadPickerService videoUploadPickerService,
            IUploadDestinationPickerPageService uploadDestinationPickerPageService,
            IFileInteractionService fileInteractionService,
            IFilePreviewService filePreviewService,
            IFileThumbnailProvider thumbnailProvider,
            INetworkAccessService networkAccess,
            IApplicationForegroundService foregroundService,
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
            ArgumentNullException.ThrowIfNull(photoUploadPickerService);
            ArgumentNullException.ThrowIfNull(videoUploadPickerService);
            ArgumentNullException.ThrowIfNull(uploadDestinationPickerPageService);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(filePreviewService);
            ArgumentNullException.ThrowIfNull(thumbnailProvider);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(foregroundService);
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
            _photoUploadPickerService = photoUploadPickerService;
            _videoUploadPickerService = videoUploadPickerService;
            _uploadDestinationPickerPageService = uploadDestinationPickerPageService;
            _fileInteractionService = fileInteractionService;
            _filePreviewService = filePreviewService;
            _thumbnailProvider = thumbnailProvider;
            _networkAccess = networkAccess;
            _foregroundService = foregroundService;
            _dialogService = dialogService;
            _sessionHandler = sessionHandler;
            _logger = logger;
            _display.FileSearchTextChanged += Display_FileSearchTextChanged;
            _networkAccess.InternetAccessRestored += NetworkAccess_InternetAccessRestored;
            _foregroundService.Resumed += ForegroundService_Resumed;
        }

        public async Task InitializeAsync(Uri instanceUri)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            _instanceUri = instanceUri;
            _display.ApplyFileBrowserPreferences(LoadFileBrowserPreferences());
            await LoadRootFilesAsync();
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

            if (_fileNavigation.Count == 0)
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
            bool changed = _instanceUri is not null
                && RefreshLocalFileMarkers(_instanceUri);

            if (changed && !IsFileBrowserBusy())
            {
                _display.ShowFilesStatus("On-device files updated.");
            }
        }

        public async Task NavigateUpAsync()
        {
            ClearFileActionRetry();
            if (IsFileBrowserBusy())
            {
                return;
            }

            if (_fileNavigation.Count == 0)
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
                int previousIndex = _fileNavigation.Count - 1;
                CottonFolderHandle previous = _fileNavigation[previousIndex];
                _fileNavigation.RemoveAt(previousIndex);
                _display.ClearFileSearch();
                if (_fileNavigation.Count == 0)
                {
                    await LoadRootFilesAsync();
                }
                else
                {
                    await LoadFolderAsync(previous, preserveHistory: true);
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

            if (currentEntry.IsFolder)
            {
                ClearFileActionRetry();
                await OpenFolderAsync(currentEntry);
                return;
            }

            await ShowFileActionsAsync(currentEntry);
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
                "Add",
                CancelAction,
                null,
                UploadPhotoAction,
                UploadVideoAction,
                UploadPhotoToFolderAction,
                UploadVideoToFolderAction,
                UploadFileAction);

            if (!CanUseFileBrowserContext(instanceUri) || !HasSameFolder(_currentFolder, folder))
            {
                return;
            }

            string? normalizedAction = NormalizeAction(action);
            if (normalizedAction == UploadPhotoAction)
            {
                await UploadPickedSourceAsync(
                    instanceUri,
                    folder,
                    _photoUploadPickerService.PickPhotoAsync,
                    "photo",
                    "Could not choose photo.");
            }
            else if (normalizedAction == UploadPhotoToFolderAction)
            {
                await UploadPickedSourceToDestinationAsync(
                    instanceUri,
                    _photoUploadPickerService.PickPhotoAsync,
                    "photo",
                    "Could not choose photo.");
            }
            else if (normalizedAction == UploadVideoAction)
            {
                await UploadPickedSourceAsync(
                    instanceUri,
                    folder,
                    _videoUploadPickerService.PickVideoAsync,
                    "video",
                    "Could not choose video.");
            }
            else if (normalizedAction == UploadVideoToFolderAction)
            {
                await UploadPickedSourceToDestinationAsync(
                    instanceUri,
                    _videoUploadPickerService.PickVideoAsync,
                    "video",
                    "Could not choose video.");
            }
            else if (normalizedAction == UploadFileAction)
            {
                await UploadPickedSourceAsync(
                    instanceUri,
                    folder,
                    _fileUploadPickerService.PickFileAsync,
                    "file",
                    "Could not choose file.");
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
                case MainPageFileAction.Download:
                    await DownloadFileAsync(currentEntry);
                    break;
                case MainPageFileAction.KeepOffline:
                    await KeepFileOfflineAsync(currentEntry);
                    break;
                case MainPageFileAction.Open:
                    await OpenFileAsync(currentEntry);
                    break;
                case MainPageFileAction.RemoveOffline:
                    await RemoveFileOfflineAsync(currentEntry);
                    break;
                case MainPageFileAction.Share:
                    await ShareFileAsync(currentEntry);
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
                if (_currentFolder is not null)
                {
                    _fileNavigation.Add(_currentFolder);
                }

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

            RefreshLocalFileMarkers(instanceUri);
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
                KeepOfflineAction,
            };
            if (refreshedFile.HasLocalCopy)
            {
                actions.Add(RemoveOfflineAction);
            }

            actions.Add(ShareAction);
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
                case RemoveOfflineAction:
                    await RemoveFileOfflineAsync(currentFile);
                    break;
                case ShareAction:
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
                content = ApplyLocalFiles(instanceUri, content);
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
                content = ApplyLocalFiles(instanceUri, content);
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
                    canNavigateUp: _fileNavigation.Count > 0,
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

            CottonFolderContent? cachedContent = await _folderContentCache.LoadRootAsync(
                instanceUri,
                fileLoadCancellation.Token);
            fileLoadCancellation.Token.ThrowIfCancellationRequested();
            if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
            {
                return true;
            }

            if (cachedContent is null)
            {
                return false;
            }

            cachedContent = await ApplyCachedThumbnailsAsync(instanceUri, cachedContent, fileLoadCancellation.Token);
            cachedContent = ApplyLocalFiles(instanceUri, cachedContent);
            _fileNavigation.Clear();
            _currentFolder = new CottonFolderHandle(cachedContent.FolderId, cachedContent.FolderName);
            _lastFileLoadFailed = true;
            _lastFileLoadDisplayedCachedContent = true;
            _display.ShowFiles(
                cachedContent,
                isRoot: true,
                canNavigateUp: false,
                CreatePath(cachedContent.FolderName));
            _display.ShowOfflineFilesNotice(isCachedListing: true);
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

            CottonFolderContent? cachedContent = await _folderContentCache.LoadFolderAsync(
                instanceUri,
                folder,
                fileLoadCancellation.Token);
            fileLoadCancellation.Token.ThrowIfCancellationRequested();
            if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
            {
                return true;
            }

            if (cachedContent is null)
            {
                return false;
            }

            cachedContent = await ApplyCachedThumbnailsAsync(instanceUri, cachedContent, fileLoadCancellation.Token);
            cachedContent = ApplyLocalFiles(instanceUri, cachedContent);
            _currentFolder = new CottonFolderHandle(cachedContent.FolderId, cachedContent.FolderName);
            _lastFileLoadFailed = true;
            _lastFileLoadDisplayedCachedContent = true;
            _display.ShowFiles(
                cachedContent,
                isRoot: false,
                canNavigateUp: _fileNavigation.Count > 0,
                CreatePath(cachedContent.FolderName));
            _display.ShowOfflineFilesNotice(isCachedListing: true);
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

                RefreshLocalFileMarkers(instanceUri);
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

        private async Task KeepFileOfflineAsync(CottonFileBrowserEntry file)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to keep files offline.");
                return;
            }

            CottonLocalFileSnapshot? reusableLocalFile =
                _fileBrowserService.GetReusableLocalDownloadSnapshot(instanceUri, file);
            if (reusableLocalFile is not null)
            {
                await PinFileOfflineAsync(instanceUri, file, CancellationToken.None);
                _display.ShowFileLocalCopy(file, reusableLocalFile);
                _display.ShowFilesStatus(CottonOfflineFileStatusText.CreateAvailableStatus(file.Name));
                return;
            }

            if (ShowOfflineRetryIfNeeded(
                    MainPageFileAction.KeepOffline,
                    file,
                    CottonOfflineFileStatusText.OfflineUnavailableStatus))
            {
                return;
            }

            CancellationTokenSource fileActionCancellation =
                BeginFileAction(CottonOfflineFileStatusText.CreateStartingStatus(file.Name));
            bool shouldRunRecoveryRefresh = false;

            try
            {
                CottonFileDownloadResult result = await _fileBrowserService.DownloadAsync(
                    instanceUri,
                    file,
                    CreateFileDownloadProgress(
                        file,
                        "Saving",
                        () => IsActiveFileAction(fileActionCancellation, instanceUri),
                        fileActionCancellation.Token),
                    fileActionCancellation.Token);
                fileActionCancellation.Token.ThrowIfCancellationRequested();
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    return;
                }

                await PinFileOfflineAsync(instanceUri, file, CancellationToken.None);
                RefreshLocalFileMarkers(instanceUri);
                _display.ShowFilesStatus(CottonOfflineFileStatusText.CreateAvailableStatus(result.FileName));
                shouldRunRecoveryRefresh = true;
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile keep-offline authorization failure {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug("Ignored stale Cotton mobile keep-offline cancellation {FileId}.", file.Id);
                    return;
                }

                ClearFileActionRetry();
                if (ShowReusableLocalFileIfAvailable(file))
                {
                    _display.ShowFilesStatus(CottonOfflineFileStatusText.CreateAvailableStatus(file.Name));
                    return;
                }

                _display.ShowFilesStatus(CottonOfflineFileStatusText.CancelledStatus);
            }
            catch (Exception exception)
            {
                if (!IsActiveFileAction(fileActionCancellation, instanceUri))
                {
                    _logger.LogDebug(exception, "Ignored stale Cotton mobile keep-offline failure {FileId}.", file.Id);
                    return;
                }

                _logger.LogError(exception, "Failed to keep Cotton mobile file offline {FileId}.", file.Id);
                ShowFileActionRetry(
                    MainPageFileAction.KeepOffline,
                    file,
                    CreateFileActionFailureStatus(
                        exception,
                        CottonOfflineFileStatusText.FailedStatus,
                        CottonOfflineFileStatusText.OfflineUnavailableStatus));
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

            CottonLocalFileSnapshot? reusableLocalFile =
                _fileBrowserService.GetReusableLocalDownloadSnapshot(instanceUri, file);
            if (reusableLocalFile is null)
            {
                await RemoveFileOfflinePinAsync(instanceUri, file.Id, CancellationToken.None);
                _display.ClearFileLocalCopy(file);
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

                _display.ClearFileLocalCopy(file);
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
            if (_fileNavigation.Count == 0)
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

        private string CreatePath(string currentFolderName)
        {
            IEnumerable<string> names = _fileNavigation
                .Select((folder, index) => index == 0 ? MainPageDisplayState.RootFilesTitle : folder.Name)
                .Append(string.IsNullOrWhiteSpace(currentFolderName) ? "Files" : currentFolderName.Trim());
            return string.Join(" / ", names);
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

        private CottonFolderContent ApplyLocalFiles(Uri instanceUri, CottonFolderContent content)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(content);

            var entries = new List<CottonFileBrowserEntry>(content.Entries.Count);
            foreach (CottonFileBrowserEntry entry in content.Entries)
            {
                CottonLocalFileSnapshot? localFile = _fileBrowserService.GetReusableLocalDownloadSnapshot(instanceUri, entry);
                entries.Add(localFile is null ? entry : entry.WithLocalFile(localFile));
            }

            return new CottonFolderContent(content.FolderId, content.FolderName, entries);
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
                    CreateFileActionFailureStatus(exception, "Open failed.", OfflineOpenStatus));
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

        private async Task ShowFileDetailsAsync(CottonFileBrowserEntry file)
        {
            CottonLocalFileSnapshot? localFile = _instanceUri is null
                ? null
                : _fileBrowserService.GetLocalDownload(_instanceUri, file);
            if (localFile is null || !IsReusableLocalFile(file, localFile))
            {
                _display.ClearFileLocalCopy(file);
            }

            await _dialogService.ShowAlertAsync(
                file.Name,
                CreateFileDetailsMessage(file, localFile),
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
            RefreshLocalFileMarkers(instanceUri);
            return downloadedFile;
        }

        private static string CreateFileDetailsMessage(
            CottonFileBrowserEntry file,
            CottonLocalFileSnapshot? localFile)
        {
            string size = file.SizeBytes.HasValue ? CottonFileSizeFormatter.Format(file.SizeBytes.Value) : "Unknown";
            string contentType = file.ContentType ?? "Unknown";
            string localCopy = CreateLocalCopyDetails(file, localFile);
            string updated = FormatLocalTimestamp(file.UpdatedAtUtc);

            return string.Join(
                Environment.NewLine,
                $"Type: {file.Kind}",
                $"Size: {size}",
                $"Updated: {updated}",
                $"Content type: {contentType}",
                $"On device: {localCopy}");
        }

        private static string FormatLocalTimestamp(DateTime value)
        {
            DateTime utc = value.Kind == DateTimeKind.Utc
                ? value
                : value.ToUniversalTime();
            return $"{utc.ToLocalTime():yyyy-MM-dd HH:mm}";
        }

        private static string CreateLocalCopyDetails(
            CottonFileBrowserEntry file,
            CottonLocalFileSnapshot? localFile)
        {
            if (localFile is null)
            {
                return "No";
            }

            if (file.SizeBytes.HasValue && file.SizeBytes.Value != localFile.SizeBytes)
            {
                return $"Needs refresh ({CottonFileSizeFormatter.Format(localFile.SizeBytes)})";
            }

            if (!CottonLocalFileFreshness.IsFresh(localFile.UpdatedAtUtc, file.UpdatedAtUtc))
            {
                return $"Needs refresh ({CottonFileSizeFormatter.Format(localFile.SizeBytes)})";
            }

            return $"Yes ({CottonFileSizeFormatter.Format(localFile.SizeBytes)})";
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
                ShareAction);

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
                case ShareAction:
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
            return _filePreviewService.CanPreview(file) ? OpenAction : OpenWithSystemAppAction;
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
            return _filePreviewService.CanPreview(file, localFile) ? OpenAction : OpenWithSystemAppAction;
        }

        private string CreateOpenAction(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile)
        {
            return _filePreviewService.CanPreview(file, downloadedFile) ? OpenAction : OpenWithSystemAppAction;
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
                    CreateFileActionFailureStatus(exception, "Open failed.", OfflineOpenStatus));
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

        private bool RefreshLocalFileMarkers(Uri instanceUri)
        {
            return _display.RefreshFileLocalCopies(entry =>
                _fileBrowserService.GetReusableLocalDownloadSnapshot(
                    instanceUri,
                    entry));
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

        private static string CreateCurrentActionLabel(string label, bool isCurrent)
        {
            return isCurrent ? label + CurrentActionSuffix : label;
        }

        private static string? NormalizeAction(string? action)
        {
            if (action is null)
            {
                return null;
            }

            return action.EndsWith(CurrentActionSuffix, StringComparison.Ordinal)
                ? action[..^CurrentActionSuffix.Length]
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
