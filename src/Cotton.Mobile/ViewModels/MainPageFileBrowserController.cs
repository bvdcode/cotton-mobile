using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class MainPageFileBrowserController
    {
        private const string CancelAction = "Cancel";
        private const string DetailsAction = "Details";
        private const string DownloadAction = "Download";
        private const string OpenAction = "Open";
        private const string ShareAction = "Share";

        private readonly MainPageDisplayState _display;
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly IFileBrowserPreferenceStore _preferenceStore;
        private readonly IFileInteractionService _fileInteractionService;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<MainPageFileBrowserController> _logger;
        private readonly List<CottonFolderHandle> _fileNavigation = [];

        private CottonFolderHandle? _currentFolder;
        private Uri? _instanceUri;

        public MainPageFileBrowserController(
            MainPageDisplayState display,
            ICottonFileBrowserService fileBrowserService,
            IFileBrowserPreferenceStore preferenceStore,
            IFileInteractionService fileInteractionService,
            IUserDialogService dialogService,
            ILogger<MainPageFileBrowserController> logger)
        {
            ArgumentNullException.ThrowIfNull(display);
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(preferenceStore);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _display = display;
            _fileBrowserService = fileBrowserService;
            _preferenceStore = preferenceStore;
            _fileInteractionService = fileInteractionService;
            _dialogService = dialogService;
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
            _instanceUri = null;
            _currentFolder = null;
            _fileNavigation.Clear();
        }

        public async Task RefreshAsync()
        {
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
            if (_fileNavigation.Count == 0)
            {
                return;
            }

            int previousIndex = _fileNavigation.Count - 1;
            CottonFolderHandle previous = _fileNavigation[previousIndex];
            _fileNavigation.RemoveAt(previousIndex);
            await LoadFolderAsync(previous, preserveHistory: true);
        }

        public async Task OpenEntryAsync(CottonFileBrowserEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (entry.IsFolder)
            {
                await OpenFolderAsync(entry);
                return;
            }

            await ShowFileActionsAsync(entry);
        }

        public Task ToggleViewModeAsync()
        {
            CottonFileBrowserViewMode nextViewMode = _display.FileViewMode == CottonFileBrowserViewMode.List
                ? CottonFileBrowserViewMode.Tiles
                : CottonFileBrowserViewMode.List;
            _preferenceStore.SaveViewMode(nextViewMode);
            _display.ShowFileViewMode(nextViewMode);
            return Task.CompletedTask;
        }

        public Task SortByNameAsync()
        {
            return SetSortModeAsync(CottonFileBrowserSortMode.Name);
        }

        public Task SortByTypeAsync()
        {
            return SetSortModeAsync(CottonFileBrowserSortMode.Type);
        }

        public Task SortBySizeAsync()
        {
            return SetSortModeAsync(CottonFileBrowserSortMode.Size);
        }

        private async Task OpenFolderAsync(CottonFileBrowserEntry folder)
        {
            if (_currentFolder is not null)
            {
                _fileNavigation.Add(_currentFolder);
            }

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
                _display.ShowFilesLoading("Loading files...");
            }

            try
            {
                CottonFolderContent content = await _fileBrowserService.GetRootAsync(_instanceUri);
                _fileNavigation.Clear();
                _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _display.ShowFiles(content, canNavigateUp: false, CreatePath(content.FolderName));
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
                _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _display.ShowFiles(content, canNavigateUp: _fileNavigation.Count > 0, CreatePath(content.FolderName));
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

            _display.ShowFilesLoading($"Downloading {file.Name}...");

            try
            {
                CottonFileDownloadResult result = await _fileBrowserService.DownloadAsync(_instanceUri, file);
                _display.ShowFilesStatus($"Downloaded {result.FileName}.");
                await _dialogService.ShowAlertAsync(
                    "Downloaded",
                    $"{result.FileName} was saved to app storage.",
                    "OK");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to download Cotton mobile file {FileId}.", file.Id);
                _display.ShowFilesStatus("Download failed. Try again.");
            }
        }

        private string CreatePath(string currentFolderName)
        {
            IEnumerable<string> names = _fileNavigation
                .Select(folder => folder.Name)
                .Append(string.IsNullOrWhiteSpace(currentFolderName) ? "Files" : currentFolderName.Trim());
            return string.Join(" / ", names);
        }

        private async Task OpenFileAsync(CottonFileBrowserEntry file)
        {
            if (_instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to open files.");
                return;
            }

            _display.ShowFilesLoading($"Opening {file.Name}...");

            try
            {
                CottonFileDownloadResult result = await _fileBrowserService.DownloadAsync(_instanceUri, file);
                await _fileInteractionService.OpenAsync(result);
                _display.ShowFilesStatus($"Opened {result.FileName}.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to open Cotton mobile file {FileId}.", file.Id);
                _display.ShowFilesStatus("Open failed. Try again.");
            }
        }

        private async Task ShareFileAsync(CottonFileBrowserEntry file)
        {
            if (_instanceUri is null)
            {
                _display.ShowFilesStatus("Sign in to share files.");
                return;
            }

            _display.ShowFilesLoading($"Preparing {file.Name}...");

            try
            {
                CottonFileDownloadResult result = await _fileBrowserService.DownloadAsync(_instanceUri, file);
                await _fileInteractionService.ShareAsync(result);
                _display.ShowFilesStatus($"Shared {result.FileName}.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to share Cotton mobile file {FileId}.", file.Id);
                _display.ShowFilesStatus("Share failed. Try again.");
            }
        }

        private async Task ShowFileDetailsAsync(CottonFileBrowserEntry file)
        {
            string size = file.SizeBytes.HasValue ? $"{file.SizeBytes.Value:N0} bytes" : "Unknown size";
            string contentType = file.ContentType ?? "Unknown type";
            await _dialogService.ShowAlertAsync(
                file.Name,
                $"{file.Kind}\n{size}\n{contentType}",
                "OK");
        }
    }
}
