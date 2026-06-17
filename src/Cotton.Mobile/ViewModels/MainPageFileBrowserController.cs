using Cotton.Mobile.Services;
using Cotton.Sdk;
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
        private const string ShareAction = "Share";
        private const string SortNameAction = "Name";
        private const string SortSizeAction = "Size";
        private const string SortTypeAction = "Type";
        private const string ViewListAction = "List";
        private const string ViewTilesAction = "Tiles";

        private readonly MainPageDisplayState _display;
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly IFileBrowserPreferenceStore _preferenceStore;
        private readonly IFileInteractionService _fileInteractionService;
        private readonly IFilePreviewService _filePreviewService;
        private readonly IFileThumbnailProvider _thumbnailProvider;
        private readonly IUserDialogService _dialogService;
        private readonly IFileBrowserSessionHandler _sessionHandler;
        private readonly ILogger<MainPageFileBrowserController> _logger;
        private readonly List<CottonFolderHandle> _fileNavigation = [];

        private CancellationTokenSource? _fileActionCancellation;
        private MainPageFileAction? _retryFileAction;
        private CottonFileBrowserEntry? _retryFileActionEntry;
        private CottonFolderHandle? _currentFolder;
        private Uri? _instanceUri;

        public MainPageFileBrowserController(
            MainPageDisplayState display,
            ICottonFileBrowserService fileBrowserService,
            IFileBrowserPreferenceStore preferenceStore,
            IFileInteractionService fileInteractionService,
            IFilePreviewService filePreviewService,
            IFileThumbnailProvider thumbnailProvider,
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
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(sessionHandler);
            ArgumentNullException.ThrowIfNull(logger);

            _display = display;
            _fileBrowserService = fileBrowserService;
            _preferenceStore = preferenceStore;
            _fileInteractionService = fileInteractionService;
            _filePreviewService = filePreviewService;
            _thumbnailProvider = thumbnailProvider;
            _dialogService = dialogService;
            _sessionHandler = sessionHandler;
            _logger = logger;
        }

        public async Task InitializeAsync(Uri instanceUri)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            _instanceUri = instanceUri;
            _display.ApplyFileBrowserPreferences(_preferenceStore.Get());
            await LoadRootFilesAsync();
        }

        public void Clear()
        {
            CancelCurrentFileAction();
            ClearFileActionRetry();
            _instanceUri = null;
            _currentFolder = null;
            _fileNavigation.Clear();
        }

        public async Task RefreshAsync()
        {
            ClearFileActionRetry();
            if (_instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to load files.");
                return;
            }

            if (_currentFolder is null)
            {
                await LoadRootFilesAsync(isRefresh: true);
                return;
            }

            await LoadFolderAsync(_currentFolder, preserveHistory: true, isRefresh: true);
        }

        public async Task NavigateUpAsync()
        {
            ClearFileActionRetry();
            if (_fileNavigation.Count == 0)
            {
                return;
            }

            int previousIndex = _fileNavigation.Count - 1;
            CottonFolderHandle previous = _fileNavigation[previousIndex];
            _fileNavigation.RemoveAt(previousIndex);
            _display.ClearFileSearch();
            await LoadFolderAsync(previous, preserveHistory: true);
        }

        public async Task ActivateEntryAsync(CottonFileBrowserEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (entry.IsFolder)
            {
                ClearFileActionRetry();
                await OpenFolderAsync(entry);
                return;
            }

            await OpenFileAsync(entry);
        }

        public async Task ShowEntryActionsAsync(CottonFileBrowserEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (entry.IsFolder)
            {
                ClearFileActionRetry();
                await OpenFolderAsync(entry);
                return;
            }

            await ShowFileActionsAsync(entry);
        }

        public async Task ShowViewActionsAsync()
        {
            string? action = await _dialogService.ShowActionSheetAsync(
                $"View files as {_display.FileViewMode}",
                CancelAction,
                null,
                ViewListAction,
                ViewTilesAction);

            switch (action)
            {
                case ViewListAction:
                    SetViewMode(CottonFileBrowserViewMode.List);
                    break;
                case ViewTilesAction:
                    SetViewMode(CottonFileBrowserViewMode.Tiles);
                    break;
            }
        }

        public async Task ShowSortActionsAsync()
        {
            string? action = await _dialogService.ShowActionSheetAsync(
                $"Sort files by {_display.FileSortMode}",
                CancelAction,
                null,
                SortNameAction,
                SortTypeAction,
                SortSizeAction);

            switch (action)
            {
                case SortNameAction:
                    await SetSortModeAsync(CottonFileBrowserSortMode.Name);
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

            switch (action)
            {
                case MainPageFileAction.Download:
                    await DownloadFileAsync(entry);
                    break;
                case MainPageFileAction.Open:
                    await OpenFileAsync(entry);
                    break;
                case MainPageFileAction.Share:
                    await ShareFileAsync(entry);
                    break;
            }
        }

        private async Task OpenFolderAsync(CottonFileBrowserEntry folder)
        {
            if (_currentFolder is not null)
            {
                _fileNavigation.Add(_currentFolder);
            }

            _display.ClearFileSearch();
            await LoadFolderAsync(new CottonFolderHandle(folder.Id, folder.Name), preserveHistory: false);
        }

        private async Task ShowFileActionsAsync(CottonFileBrowserEntry file)
        {
            string? action = await _dialogService.ShowActionSheetAsync(
                file.Name,
                CancelAction,
                null,
                OpenAction,
                DownloadAction,
                ShareAction,
                DetailsAction);

            switch (action)
            {
                case OpenAction:
                    await OpenFileAsync(file);
                    break;
                case DownloadAction:
                    await DownloadFileAsync(file);
                    break;
                case ShareAction:
                    await ShareFileAsync(file);
                    break;
                case DetailsAction:
                    await ShowFileDetailsAsync(file);
                    break;
            }
        }

        private Task SetSortModeAsync(CottonFileBrowserSortMode sortMode)
        {
            _preferenceStore.SaveSortMode(sortMode);
            _display.ShowFileSortMode(sortMode);
            return Task.CompletedTask;
        }

        private void SetViewMode(CottonFileBrowserViewMode viewMode)
        {
            _preferenceStore.SaveViewMode(viewMode);
            _display.ShowFileViewMode(viewMode);
        }

        private async Task LoadRootFilesAsync(bool isRefresh = false)
        {
            if (_instanceUri is null)
            {
                return;
            }

            if (isRefresh)
            {
                _display.ShowFilesRefreshing("Refreshing files...");
            }
            else
            {
                _display.ClearFileSearch();
                _display.ShowFilesLoading("Loading files...");
            }

            try
            {
                CottonFolderContent content = await _fileBrowserService.GetRootAsync(_instanceUri);
                content = await ApplyThumbnailsAsync(_instanceUri, content);
                content = ApplyLocalFiles(content);
                _fileNavigation.Clear();
                _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _display.ShowFiles(content, canNavigateUp: false, CreatePath(content.FolderName));
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                await HandleSessionExpiredAsync(exception);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile root files.");
                _display.ShowFilesStatus("Could not load files. Try refresh.");
            }
        }

        private async Task LoadFolderAsync(
            CottonFolderHandle folder,
            bool preserveHistory,
            bool isRefresh = false)
        {
            if (_instanceUri is null)
            {
                return;
            }

            if (isRefresh)
            {
                _display.ShowFilesRefreshing($"Refreshing {folder.Name}...");
            }
            else
            {
                _display.ShowFilesLoading($"Loading {folder.Name}...");
            }

            try
            {
                CottonFolderContent content = await _fileBrowserService.GetFolderAsync(_instanceUri, folder);
                content = await ApplyThumbnailsAsync(_instanceUri, content);
                content = ApplyLocalFiles(content);
                _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _display.ShowFiles(content, canNavigateUp: _fileNavigation.Count > 0, CreatePath(content.FolderName));
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                if (!preserveHistory && _fileNavigation.Count > 0)
                {
                    _fileNavigation.RemoveAt(_fileNavigation.Count - 1);
                }

                await HandleSessionExpiredAsync(exception);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile folder {FolderId}.", folder.Id);
                if (!preserveHistory && _fileNavigation.Count > 0)
                {
                    _fileNavigation.RemoveAt(_fileNavigation.Count - 1);
                }

                _display.ShowFilesStatus("Could not open folder. Try again.");
            }
        }

        private async Task DownloadFileAsync(CottonFileBrowserEntry file)
        {
            if (_instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to download files.");
                return;
            }

            CancellationTokenSource fileActionCancellation = BeginFileAction($"Downloading {file.Name}...");

            try
            {
                CottonFileDownloadResult result = await _fileBrowserService.DownloadAsync(
                    _instanceUri,
                    file,
                    CreateFileDownloadProgress(file, "Downloading"),
                    fileActionCancellation.Token);
                ShowLocalFileIfAvailable(file);
                _display.ShowFilesSummary();
                await ShowDownloadedFileActionsAsync(file, result, fileActionCancellation.Token);
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                ClearFileActionRetry();
                _display.ShowFilesStatus("Download cancelled.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to download Cotton mobile file {FileId}.", file.Id);
                ShowFileActionRetry(MainPageFileAction.Download, file, "Download failed.");
            }
            finally
            {
                EndFileAction(fileActionCancellation);
            }
        }

        private string CreatePath(string currentFolderName)
        {
            IEnumerable<string> names = _fileNavigation
                .Select(folder => folder.Name)
                .Append(string.IsNullOrWhiteSpace(currentFolderName) ? "Files" : currentFolderName.Trim());
            return string.Join(" / ", names);
        }

        private async Task<CottonFolderContent> ApplyThumbnailsAsync(
            Uri instanceUri,
            CottonFolderContent content)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(content);

            var entries = new List<CottonFileBrowserEntry>(content.Entries.Count);
            foreach (CottonFileBrowserEntry entry in content.Entries)
            {
                CottonFileThumbnailSnapshot thumbnail = await _thumbnailProvider.GetThumbnailAsync(
                    instanceUri,
                    entry);
                entries.Add(entry.WithThumbnail(thumbnail));
            }

            return new CottonFolderContent(content.FolderId, content.FolderName, entries);
        }

        private CottonFolderContent ApplyLocalFiles(CottonFolderContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            var entries = new List<CottonFileBrowserEntry>(content.Entries.Count);
            foreach (CottonFileBrowserEntry entry in content.Entries)
            {
                CottonLocalFileSnapshot? localFile = _fileBrowserService.GetLocalDownload(entry);
                entries.Add(localFile is null ? entry : entry.WithLocalFile(localFile));
            }

            return new CottonFolderContent(content.FolderId, content.FolderName, entries);
        }

        private async Task OpenFileAsync(CottonFileBrowserEntry file)
        {
            if (_instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to open files.");
                return;
            }

            CancellationTokenSource fileActionCancellation = BeginFileAction($"Opening {file.Name}...");

            try
            {
                CottonFileDownloadResult result = await PrepareFileForOpenOrShareAsync(
                    _instanceUri,
                    file,
                    "Opening",
                    fileActionCancellation.Token);
                if (_filePreviewService.CanPreview(file))
                {
                    await _filePreviewService.OpenAsync(file, result, fileActionCancellation.Token);
                }
                else
                {
                    await _fileInteractionService.OpenAsync(result, fileActionCancellation.Token);
                }

                _display.ShowFilesSummary();
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                ClearFileActionRetry();
                _display.ShowFilesStatus("Open cancelled.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to open Cotton mobile file {FileId}.", file.Id);
                ShowFileActionRetry(MainPageFileAction.Open, file, "Open failed.");
            }
            finally
            {
                EndFileAction(fileActionCancellation);
            }
        }

        private async Task ShareFileAsync(CottonFileBrowserEntry file)
        {
            if (_instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to share files.");
                return;
            }

            CancellationTokenSource fileActionCancellation = BeginFileAction($"Preparing {file.Name}...");

            try
            {
                CottonFileDownloadResult result = await PrepareFileForOpenOrShareAsync(
                    _instanceUri,
                    file,
                    "Preparing",
                    fileActionCancellation.Token);
                await _fileInteractionService.ShareAsync(result, fileActionCancellation.Token);
                _display.ShowFilesSummary();
            }
            catch (Exception exception)
                when (IsAuthorizationFailure(exception))
            {
                ClearFileActionRetry();
                await HandleSessionExpiredAsync(exception);
            }
            catch (OperationCanceledException) when (fileActionCancellation.IsCancellationRequested)
            {
                ClearFileActionRetry();
                _display.ShowFilesStatus("Share cancelled.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to share Cotton mobile file {FileId}.", file.Id);
                ShowFileActionRetry(MainPageFileAction.Share, file, "Share failed.");
            }
            finally
            {
                EndFileAction(fileActionCancellation);
            }
        }

        private async Task ShowFileDetailsAsync(CottonFileBrowserEntry file)
        {
            CottonLocalFileSnapshot? localFile = _fileBrowserService.GetLocalDownload(file);
            await _dialogService.ShowAlertAsync(
                file.Name,
                CreateFileDetailsMessage(file, localFile),
                "OK");
        }

        private async Task<CottonFileDownloadResult> PrepareFileForOpenOrShareAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            string actionName,
            CancellationToken cancellationToken)
        {
            CottonFileDownloadResult? localFile = _fileBrowserService.GetReusableLocalDownload(file);
            if (localFile is not null)
            {
                ShowLocalFileIfAvailable(file);
                return localFile;
            }

            CottonFileDownloadResult downloadedFile = await _fileBrowserService.DownloadAsync(
                instanceUri,
                file,
                CreateFileDownloadProgress(file, actionName),
                cancellationToken);
            ShowLocalFileIfAvailable(file);
            return downloadedFile;
        }

        private static string CreateFileDetailsMessage(
            CottonFileBrowserEntry file,
            CottonLocalFileSnapshot? localFile)
        {
            string size = file.SizeBytes.HasValue ? $"{file.SizeBytes.Value:N0} bytes" : "Unknown";
            string contentType = file.ContentType ?? "Unknown";
            string localCopy = localFile is null
                ? "Not downloaded"
                : $"Available ({localFile.SizeBytes:N0} bytes)";

            return string.Join(
                Environment.NewLine,
                $"Type: {file.Kind}",
                $"Size: {size}",
                $"Content type: {contentType}",
                $"Local copy: {localCopy}",
                $"File id: {file.Id:D}");
        }

        private async Task ShowDownloadedFileActionsAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken)
        {
            string? action = await _dialogService.ShowActionSheetAsync(
                $"Downloaded {downloadedFile.FileName}",
                DoneAction,
                null,
                OpenAction,
                ShareAction);

            switch (action)
            {
                case OpenAction:
                    await OpenDownloadedFileAsync(file, downloadedFile, cancellationToken);
                    break;
                case ShareAction:
                    await ShareDownloadedFileAsync(file, downloadedFile, cancellationToken);
                    break;
            }
        }

        private async Task OpenDownloadedFileAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken)
        {
            _display.ShowFileActionLoading($"Opening {file.Name}...");

            try
            {
                if (_filePreviewService.CanPreview(file))
                {
                    await _filePreviewService.OpenAsync(file, downloadedFile, cancellationToken);
                }
                else
                {
                    await _fileInteractionService.OpenAsync(downloadedFile, cancellationToken);
                }

                _display.ShowFilesSummary();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to open downloaded Cotton mobile file {FileId}.", file.Id);
                _display.ShowFilesStatus("Open failed.");
            }
        }

        private async Task ShareDownloadedFileAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken)
        {
            _display.ShowFileActionLoading($"Sharing {file.Name}...");

            try
            {
                await _fileInteractionService.ShareAsync(downloadedFile, cancellationToken);
                _display.ShowFilesSummary();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to share downloaded Cotton mobile file {FileId}.", file.Id);
                _display.ShowFilesStatus("Share failed.");
            }
        }

        private void ShowLocalFileIfAvailable(CottonFileBrowserEntry file)
        {
            CottonLocalFileSnapshot? localFile = _fileBrowserService.GetLocalDownload(file);
            if (localFile is null)
            {
                return;
            }

            _display.ShowFileLocalCopy(file, localFile);
        }

        private IProgress<long>? CreateFileDownloadProgress(CottonFileBrowserEntry file, string actionName)
        {
            if (file.SizeBytes is not > 0)
            {
                return null;
            }

            long totalBytes = file.SizeBytes.Value;
            int lastPercent = -1;
            return new Progress<long>(downloadedBytes =>
            {
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

        private void EndFileAction(CancellationTokenSource cancellation)
        {
            if (!ReferenceEquals(_fileActionCancellation, cancellation))
            {
                cancellation.Dispose();
                return;
            }

            _fileActionCancellation = null;
            cancellation.Dispose();
        }

        private void CancelCurrentFileAction()
        {
            _fileActionCancellation?.Cancel();
            _fileActionCancellation?.Dispose();
            _fileActionCancellation = null;
        }

        private void ShowFileActionRetry(
            MainPageFileAction action,
            CottonFileBrowserEntry entry,
            string status)
        {
            _retryFileAction = action;
            _retryFileActionEntry = entry;
            _display.ShowFileActionRetry($"{status} Retry?");
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
