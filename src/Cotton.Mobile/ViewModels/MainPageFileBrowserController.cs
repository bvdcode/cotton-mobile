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
        private const string OpenAction = "Open";
        private const string OpenWithSystemAppAction = "Open with system app";
        private const string ShareAction = "Share";
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

        private readonly MainPageDisplayState _display;
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly IFileBrowserPreferenceStore _preferenceStore;
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
        private bool _isRecoveryRefreshInProgress;

        public MainPageFileBrowserController(
            MainPageDisplayState display,
            ICottonFileBrowserService fileBrowserService,
            IFileBrowserPreferenceStore preferenceStore,
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
            ArgumentNullException.ThrowIfNull(preferenceStore);
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
            _preferenceStore = preferenceStore;
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
            _fileNavigation.Clear();
        }

        public void CancelActiveWork()
        {
            CancelCurrentFileLoad();
            CancelCurrentFileAction();
            ClearFileActionRetry();
            _isFileLoadInProgress = false;
            _isFolderNavigationInProgress = false;
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
            if (_instanceUri is not null)
            {
                RefreshLocalFileMarkers(_instanceUri);
            }

            if (!IsFileBrowserBusy())
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

                if (_lastFileLoadFailed && _instanceUri is not null)
                {
                    _currentFolder = originalFolder;
                    _fileNavigation.Clear();
                    _fileNavigation.AddRange(originalNavigation);
                    _display.RestoreFileSearch(originalSearchText, originalSearchOpen);
                }
            }
            finally
            {
                _isFolderNavigationInProgress = false;
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
                case MainPageFileAction.Open:
                    await OpenFileAsync(currentEntry);
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
                string originalSearchText = _display.FileSearchText;
                bool originalSearchOpen = _display.IsFileSearchOpen;
                if (_currentFolder is not null)
                {
                    _fileNavigation.Add(_currentFolder);
                }

                _display.ClearFileSearch();
                await LoadFolderAsync(new CottonFolderHandle(folder.Id, folder.Name), preserveHistory: false);
                if (_lastFileLoadFailed && _instanceUri is not null)
                {
                    _display.RestoreFileSearch(originalSearchText, originalSearchOpen);
                }
            }
            finally
            {
                _isFolderNavigationInProgress = false;
            }
        }

        private async Task ShowFileActionsAsync(CottonFileBrowserEntry file)
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null || !CanUseVisibleEntry(file, instanceUri))
            {
                return;
            }

            string openAction = CreateOpenAction(file, instanceUri);
            string? action = await _dialogService.ShowActionSheetAsync(
                file.Name,
                CancelAction,
                null,
                openAction,
                DownloadAction,
                ShareAction,
                DetailsAction);

            CottonFileBrowserEntry? currentFile = GetCurrentVisibleEntry(file, instanceUri);
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
                    await DownloadFileAsync(currentFile);
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
                content = await ApplyThumbnailsAsync(instanceUri, content, fileLoadCancellation.Token);
                content = ApplyLocalFiles(instanceUri, content);
                if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
                {
                    return;
                }

                _fileNavigation.Clear();
                _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _lastFileLoadFailed = false;
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
                content = await ApplyThumbnailsAsync(instanceUri, content, fileLoadCancellation.Token);
                content = ApplyLocalFiles(instanceUri, content);
                if (!IsActiveFileLoad(fileLoadCancellation, instanceUri))
                {
                    return;
                }

                _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _lastFileLoadFailed = false;
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
                _display.ShowFilesSummary();
                shouldRunRecoveryRefresh = await ShowDownloadedFileActionsBestEffortAsync(
                    file,
                    result,
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
                ShowFileActionRetry(MainPageFileAction.Download, file, CreateFileActionFailureStatus("Download failed.", OfflineDownloadStatus));
            }
            finally
            {
                EndFileAction(fileActionCancellation, shouldRunRecoveryRefresh);
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
                ShowFileActionRetry(MainPageFileAction.Open, file, CreateFileActionFailureStatus("Open failed.", OfflineOpenStatus));
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
                ShowFileActionRetry(MainPageFileAction.Share, file, CreateFileActionFailureStatus("Share failed.", OfflineShareStatus));
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
            string size = file.SizeBytes.HasValue ? FormatStorageSize(file.SizeBytes.Value) : "Unknown";
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
                return $"Needs refresh ({FormatStorageSize(localFile.SizeBytes)})";
            }

            if (!CottonLocalFileFreshness.IsFresh(localFile.UpdatedAtUtc, file.UpdatedAtUtc))
            {
                return $"Needs refresh ({FormatStorageSize(localFile.SizeBytes)})";
            }

            return $"Yes ({FormatStorageSize(localFile.SizeBytes)})";
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

            if (string.Equals(action, openAction, StringComparison.Ordinal))
            {
                return await OpenDownloadedFileAsync(file, downloadedFile, cancellationToken);
            }

            switch (action)
            {
                case ShareAction:
                    return await ShareDownloadedFileAsync(file, downloadedFile, cancellationToken);
            }

            return true;
        }

        private async Task<bool> ShowDownloadedFileActionsBestEffortAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken)
        {
            try
            {
                return await ShowDownloadedFileActionsAsync(file, downloadedFile, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to show Cotton mobile downloaded file actions {FileId}.", file.Id);
                _display.ShowFilesStatus("Downloaded. Could not show file actions.");
                return false;
            }
        }

        private string CreateOpenAction(CottonFileBrowserEntry file)
        {
            return _filePreviewService.CanPreview(file) ? OpenAction : OpenWithSystemAppAction;
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
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                _display.ShowFilesSummary();
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                ClearLocalFileMarkerIfFileMissing(exception, file);
                _logger.LogError(exception, "Failed to open downloaded Cotton mobile file {FileId}.", file.Id);
                _display.ShowFilesStatus("Open failed.");
                return false;
            }
        }

        private async Task<bool> ShareDownloadedFileAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _display.ShowFileActionLoading($"Sharing {file.Name}...");

            try
            {
                await _fileInteractionService.ShareAsync(downloadedFile, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                _display.ShowFilesSummary();
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                ClearLocalFileMarkerIfFileMissing(exception, file);
                _logger.LogError(exception, "Failed to share downloaded Cotton mobile file {FileId}.", file.Id);
                _display.ShowFilesStatus("Share failed.");
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

        private void RefreshLocalFileMarkers(Uri instanceUri)
        {
            _display.RefreshFileLocalCopies(entry =>
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
            if (shouldRunRecoveryRefresh)
            {
                QueueFileLoadRecoveryRefreshAfterFileAction();
            }
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
            if (_networkAccess.HasInternetAccess)
            {
                _display.ShowFilesStatus(fallbackStatus);
                return;
            }

            _display.ShowOfflineFilesNotice();
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
            if (!CanRunRecoveryRefresh())
            {
                return;
            }

            _isRecoveryRefreshInProgress = true;
            _ = RunQueuedFileLoadRecoveryRefreshAsync(reason);
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

        private bool IsFileBrowserBusy()
        {
            return _isFolderNavigationInProgress
                || _isFileLoadInProgress
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

        private string CreateFileActionFailureStatus(string fallbackStatus, string offlineStatus)
        {
            return CreateOfflineAwareStatus(fallbackStatus, offlineStatus);
        }

        private string CreateOfflineAwareStatus(string fallbackStatus, string offlineStatus)
        {
            return _networkAccess.HasInternetAccess ? fallbackStatus : offlineStatus;
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

        private static string FormatStorageSize(long bytes)
        {
            const long Kilobyte = 1024;
            const long Megabyte = Kilobyte * 1024;
            const long Gigabyte = Megabyte * 1024;

            return bytes switch
            {
                < Kilobyte => $"{bytes} B",
                < Megabyte => $"{bytes / (double)Kilobyte:0.#} KB",
                < Gigabyte => $"{bytes / (double)Megabyte:0.#} MB",
                _ => $"{bytes / (double)Gigabyte:0.#} GB",
            };
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
