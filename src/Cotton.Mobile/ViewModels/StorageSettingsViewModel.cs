using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class StorageSettingsViewModel : ViewModelBase
    {
        private const string CancelAction = "Cancel";
        private const string ClearAllAction = "Clear all";
        private const string ClearDownloadedFilesAction = "Clear downloads";
        private const string ClearThumbnailsAction = "Clear thumbnails";
        private const string ClearAllTitle = "Clear all cached files";
        private const string ClearDownloadedFilesTitle = "Clear downloaded files";
        private const string ClearThumbnailsTitle = "Clear thumbnails";

        private readonly IStorageManagementService _storageManagementService;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<StorageSettingsViewModel> _logger;
        private bool _isBusy;
        private string _totalSizeText = "0 B";
        private string _totalFileCountText = "0 files";
        private string _thumbnailSizeText = "0 B";
        private string _thumbnailFileCountText = "0 files";
        private string _downloadedSizeText = "0 B";
        private string _downloadedFileCountText = "0 files";
        private string? _status;

        public StorageSettingsViewModel(
            IStorageManagementService storageManagementService,
            IUserDialogService dialogService,
            ILogger<StorageSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(storageManagementService);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _storageManagementService = storageManagementService;
            _dialogService = dialogService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            ClearThumbnailsCommand = new AsyncCommand(ClearThumbnailsAsync, LogUnhandledCommandException, () => !IsBusy);
            ClearDownloadedFilesCommand = new AsyncCommand(
                ClearDownloadedFilesAsync,
                LogUnhandledCommandException,
                () => !IsBusy);
            ClearAllCommand = new AsyncCommand(ClearAllAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand ClearThumbnailsCommand { get; }

        public AsyncCommand ClearDownloadedFilesCommand { get; }

        public AsyncCommand ClearAllCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCommandStatesChanged();
                }
            }
        }

        public string TotalSizeText
        {
            get => _totalSizeText;
            private set => SetProperty(ref _totalSizeText, value);
        }

        public string TotalFileCountText
        {
            get => _totalFileCountText;
            private set => SetProperty(ref _totalFileCountText, value);
        }

        public string ThumbnailSizeText
        {
            get => _thumbnailSizeText;
            private set => SetProperty(ref _thumbnailSizeText, value);
        }

        public string ThumbnailFileCountText
        {
            get => _thumbnailFileCountText;
            private set => SetProperty(ref _thumbnailFileCountText, value);
        }

        public string DownloadedSizeText
        {
            get => _downloadedSizeText;
            private set => SetProperty(ref _downloadedSizeText, value);
        }

        public string DownloadedFileCountText
        {
            get => _downloadedFileCountText;
            private set => SetProperty(ref _downloadedFileCountText, value);
        }

        public string? Status
        {
            get => _status;
            private set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(IsStatusVisible));
                }
            }
        }

        public bool IsStatusVisible => !string.IsNullOrWhiteSpace(Status);

        private async Task LoadAsync()
        {
            await RunStorageActionAsync(
                async () =>
                {
                    CottonStorageSummary summary = await _storageManagementService.GetSummaryAsync();
                    ShowSummary(summary);
                    Status = null;
                },
                "Could not inspect app storage.");
        }

        private Task ClearThumbnailsAsync()
        {
            return ClearAsync(
                ClearThumbnailsTitle,
                "Thumbnail previews will reload as you browse.",
                ClearThumbnailsAction,
                _storageManagementService.ClearThumbnailCacheAsync,
                "Thumbnails cleared.");
        }

        private Task ClearDownloadedFilesAsync()
        {
            return ClearAsync(
                ClearDownloadedFilesTitle,
                "Files marked On device will need internet to open again.",
                ClearDownloadedFilesAction,
                _storageManagementService.ClearDownloadedFilesAsync,
                "Downloaded files cleared.");
        }

        private Task ClearAllAsync()
        {
            return ClearAsync(
                ClearAllTitle,
                "Thumbnail previews and files marked On device will be removed from this device.",
                ClearAllAction,
                _storageManagementService.ClearAllCachedFilesAsync,
                "Cached files cleared.");
        }

        private async Task ClearAsync(
            string title,
            string message,
            string acceptAction,
            Func<CancellationToken, Task> clearAsync,
            string successStatus)
        {
            bool confirmed = await _dialogService.ShowConfirmationAsync(
                title,
                message,
                acceptAction,
                CancelAction);
            if (!confirmed)
            {
                return;
            }

            await RunStorageActionAsync(
                async () =>
                {
                    await clearAsync(CancellationToken.None);
                    CottonStorageSummary summary = await _storageManagementService.GetSummaryAsync();
                    ShowSummary(summary);
                    Status = successStatus;
                },
                "Could not clear app storage.");
        }

        private async Task RunStorageActionAsync(Func<Task> actionAsync, string failureStatus)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                await actionAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile storage action failed.");
                Status = failureStatus;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ShowSummary(CottonStorageSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            TotalSizeText = FormatStorageSize(summary.TotalSizeBytes);
            TotalFileCountText = FormatFileCount(summary.TotalFileCount);
            ThumbnailSizeText = FormatStorageSize(summary.ThumbnailCache.SizeBytes);
            ThumbnailFileCountText = FormatFileCount(summary.ThumbnailCache.FileCount);
            DownloadedSizeText = FormatStorageSize(summary.DownloadedFiles.SizeBytes);
            DownloadedFileCountText = FormatFileCount(summary.DownloadedFiles.FileCount);
        }

        private void RaiseCommandStatesChanged()
        {
            LoadCommand.RaiseCanExecuteChanged();
            ClearThumbnailsCommand.RaiseCanExecuteChanged();
            ClearDownloadedFilesCommand.RaiseCanExecuteChanged();
            ClearAllCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile storage command exception.");
        }

        private static string FormatFileCount(int fileCount)
        {
            return fileCount == 1 ? "1 file" : $"{fileCount:N0} files";
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
    }
}
