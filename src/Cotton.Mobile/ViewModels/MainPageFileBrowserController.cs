using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class MainPageFileBrowserController
    {
        private readonly MainPageDisplayState _display;
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<MainPageFileBrowserController> _logger;
        private readonly Stack<CottonFolderHandle> _fileNavigation = new();

        private CottonFolderHandle? _currentFolder;
        private Uri? _instanceUri;

        public MainPageFileBrowserController(
            MainPageDisplayState display,
            ICottonFileBrowserService fileBrowserService,
            IUserDialogService dialogService,
            ILogger<MainPageFileBrowserController> logger)
        {
            ArgumentNullException.ThrowIfNull(display);
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _display = display;
            _fileBrowserService = fileBrowserService;
            _dialogService = dialogService;
            _logger = logger;
        }

        public async Task InitializeAsync(Uri instanceUri)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            _instanceUri = instanceUri;
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
                await LoadRootFilesAsync();
                return;
            }

            await LoadFolderAsync(_currentFolder, preserveHistory: true);
        }

        public async Task NavigateUpAsync()
        {
            if (_fileNavigation.Count == 0)
            {
                return;
            }

            CottonFolderHandle previous = _fileNavigation.Pop();
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

            await DownloadFileAsync(entry);
        }

        private async Task OpenFolderAsync(CottonFileBrowserEntry folder)
        {
            if (_currentFolder is not null)
            {
                _fileNavigation.Push(_currentFolder);
            }

            await LoadFolderAsync(new CottonFolderHandle(folder.Id, folder.Name), preserveHistory: false);
        }

        private async Task LoadRootFilesAsync()
        {
            if (_instanceUri is null)
            {
                return;
            }

            _display.ShowFilesLoading("Loading files...");

            try
            {
                CottonFolderContent content = await _fileBrowserService.GetRootAsync(_instanceUri);
                _fileNavigation.Clear();
                _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _display.ShowFiles(content, canNavigateUp: false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile root files.");
                _display.ShowFilesStatus("Could not load files. Try refresh.");
            }
        }

        private async Task LoadFolderAsync(CottonFolderHandle folder, bool preserveHistory)
        {
            if (_instanceUri is null)
            {
                return;
            }

            _display.ShowFilesLoading($"Loading {folder.Name}...");

            try
            {
                CottonFolderContent content = await _fileBrowserService.GetFolderAsync(_instanceUri, folder);
                _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _display.ShowFiles(content, canNavigateUp: _fileNavigation.Count > 0);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile folder {FolderId}.", folder.Id);
                if (!preserveHistory && _fileNavigation.Count > 0)
                {
                    _fileNavigation.Pop();
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
    }
}
