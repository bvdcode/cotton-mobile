using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Cotton.Mobile.ViewModels
{
    public class StorageSettingsViewModel : ViewModelBase
    {
        private const string CancelAction = "Cancel";
        private const string ClearAllAction = "Clear all";
        private const string ClearDownloadedFilesAction = "Clear downloads";
        private const string ClearFolderListingsAction = "Clear lists";
        private const string ClearThumbnailsAction = "Clear thumbnails";
        private const string ClearAllTitle = "Clear all cached files";
        private const string ClearDownloadedFilesTitle = "Clear downloaded and offline files";
        private const string ClearFolderListingsTitle = "Clear offline folder lists";
        private const string ClearThumbnailsTitle = "Clear thumbnails";

        private readonly IStorageManagementService _storageManagementService;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<StorageSettingsViewModel> _logger;
        private bool _isBusy;
        private string _totalSizeText = "0 B";
        private string _totalFileCountText = "0 files";
        private string _thumbnailSizeText = "0 B";
        private string _thumbnailFileCountText = "0 files";
        private string _folderListingSizeText = "0 B";
        private string _folderListingFileCountText = "0 files";
        private string _downloadedSizeText = "0 B";
        private string _downloadedFileCountText = "0 files";
        private string _onDeviceSummaryText = CottonOnDeviceStorageSummary.Empty.SummaryText;
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
            ClearFolderListingsCommand = new AsyncCommand(
                ClearFolderListingsAsync,
                LogUnhandledCommandException,
                () => !IsBusy);
            ClearDownloadedFilesCommand = new AsyncCommand(
                ClearDownloadedFilesAsync,
                LogUnhandledCommandException,
                () => !IsBusy);
            ClearAllCommand = new AsyncCommand(ClearAllAsync, LogUnhandledCommandException, () => !IsBusy);
            ShowOnDeviceStorage(CottonOnDeviceStorageSummary.Empty);
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand ClearThumbnailsCommand { get; }

        public AsyncCommand ClearFolderListingsCommand { get; }

        public AsyncCommand ClearDownloadedFilesCommand { get; }

        public AsyncCommand ClearAllCommand { get; }

        public ObservableCollection<CottonOnDeviceStorageBucketSnapshot> OnDeviceBuckets { get; } = new();

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

        public string FolderListingSizeText
        {
            get => _folderListingSizeText;
            private set => SetProperty(ref _folderListingSizeText, value);
        }

        public string FolderListingFileCountText
        {
            get => _folderListingFileCountText;
            private set => SetProperty(ref _folderListingFileCountText, value);
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

        public string OnDeviceSummaryText
        {
            get => _onDeviceSummaryText;
            private set => SetProperty(ref _onDeviceSummaryText, value);
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
                "Files marked On device, including kept-offline files, will need internet to open again.",
                ClearDownloadedFilesAction,
                _storageManagementService.ClearDownloadedFilesAsync,
                "Downloaded and offline files cleared.");
        }

        private Task ClearFolderListingsAsync()
        {
            return ClearAsync(
                ClearFolderListingsTitle,
                "Folder lists will reload from your Cotton instance when you browse.",
                ClearFolderListingsAction,
                _storageManagementService.ClearFolderListingsCacheAsync,
                "Offline folder lists cleared.");
        }

        private Task ClearAllAsync()
        {
            return ClearAsync(
                ClearAllTitle,
                "Thumbnail previews, offline folder lists, and files marked On device, including kept-offline files, will be removed from this device.",
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
            await RunStorageActionAsync(
                async () =>
                {
                    bool confirmed = await _dialogService.ShowConfirmationAsync(
                        title,
                        message,
                        acceptAction,
                        CancelAction);
                    if (!confirmed)
                    {
                        Status = null;
                        return;
                    }

                    await clearAsync(CancellationToken.None);
                    CottonStorageSummary summary = await _storageManagementService.GetSummaryAsync();
                    ShowSummary(summary);
                    Status = successStatus;
                },
                "Could not clear app storage.",
                RefreshSummaryAfterClearFailureAsync);
        }

        private async Task RunStorageActionAsync(
            Func<Task> actionAsync,
            string failureStatus,
            Func<Task>? failureCallbackAsync = null)
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
                if (failureCallbackAsync is not null)
                {
                    await RunFailureCallbackAsync(failureCallbackAsync);
                }

                Status = failureStatus;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshSummaryAfterClearFailureAsync()
        {
            CottonStorageSummary summary = await _storageManagementService.GetSummaryAsync();
            ShowSummary(summary);
        }

        private async Task RunFailureCallbackAsync(Func<Task> failureCallbackAsync)
        {
            try
            {
                await failureCallbackAsync();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Cotton mobile storage failure callback failed.");
            }
        }

        private void ShowSummary(CottonStorageSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            TotalSizeText = CottonFileSizeFormatter.Format(summary.TotalSizeBytes);
            TotalFileCountText = FormatFileCount(summary.TotalFileCount);
            ThumbnailSizeText = CottonFileSizeFormatter.Format(summary.ThumbnailCache.SizeBytes);
            ThumbnailFileCountText = FormatFileCount(summary.ThumbnailCache.FileCount);
            FolderListingSizeText = CottonFileSizeFormatter.Format(summary.FolderListings.SizeBytes);
            FolderListingFileCountText = FormatFileCount(summary.FolderListings.FileCount);
            DownloadedSizeText = CottonFileSizeFormatter.Format(summary.DownloadedFiles.SizeBytes);
            DownloadedFileCountText = FormatFileCount(summary.DownloadedFiles.FileCount);
            ShowOnDeviceStorage(summary.OnDeviceStorage);
        }

        private void ShowOnDeviceStorage(CottonOnDeviceStorageSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            OnDeviceSummaryText = summary.SummaryText;
            OnDeviceBuckets.Clear();
            foreach (CottonOnDeviceStorageBucketSnapshot bucket in summary.Buckets)
            {
                OnDeviceBuckets.Add(bucket);
            }
        }

        private void RaiseCommandStatesChanged()
        {
            LoadCommand.RaiseCanExecuteChanged();
            ClearThumbnailsCommand.RaiseCanExecuteChanged();
            ClearFolderListingsCommand.RaiseCanExecuteChanged();
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

    }
}
