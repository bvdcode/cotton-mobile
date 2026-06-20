using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Cotton.Mobile.ViewModels
{
    public class StorageSettingsViewModel : ViewModelBase
    {
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
        private string _transferStagingSizeText = "0 B";
        private string _transferStagingFileCountText = "0 files";
        private string _onDeviceSummaryText = CottonOnDeviceStorageSummary.Empty.SummaryText;
        private string _storageBudgetSummaryText = CottonStorageBudgetSummary.Empty.SummaryText;
        private string _protectedOfflineText = CottonStorageBudgetSummary.Empty.ProtectedOfflineText;
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
            ClearTemporaryUploadsCommand = new AsyncCommand(
                ClearTemporaryUploadsAsync,
                LogUnhandledCommandException,
                () => !IsBusy);
            ClearDownloadedFilesCommand = new AsyncCommand(
                ClearDownloadedFilesAsync,
                LogUnhandledCommandException,
                () => !IsBusy);
            ClearAllCommand = new AsyncCommand(ClearAllAsync, LogUnhandledCommandException, () => !IsBusy);
            ShowOnDeviceStorage(CottonOnDeviceStorageSummary.Empty);
            ShowStorageBudget(CottonStorageBudgetSummary.Empty);
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand ClearThumbnailsCommand { get; }

        public AsyncCommand ClearFolderListingsCommand { get; }

        public AsyncCommand ClearTemporaryUploadsCommand { get; }

        public AsyncCommand ClearDownloadedFilesCommand { get; }

        public AsyncCommand ClearAllCommand { get; }

        public ObservableCollection<CottonOnDeviceStorageBucketSnapshot> OnDeviceBuckets { get; } = new();

        public ObservableCollection<CottonStorageBudgetBucketSnapshot> StorageBudgetBuckets { get; } = new();

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

        public string TransferStagingSizeText
        {
            get => _transferStagingSizeText;
            private set => SetProperty(ref _transferStagingSizeText, value);
        }

        public string TransferStagingFileCountText
        {
            get => _transferStagingFileCountText;
            private set => SetProperty(ref _transferStagingFileCountText, value);
        }

        public string OnDeviceSummaryText
        {
            get => _onDeviceSummaryText;
            private set => SetProperty(ref _onDeviceSummaryText, value);
        }

        public string StorageBudgetSummaryText
        {
            get => _storageBudgetSummaryText;
            private set => SetProperty(ref _storageBudgetSummaryText, value);
        }

        public string ProtectedOfflineText
        {
            get => _protectedOfflineText;
            private set => SetProperty(ref _protectedOfflineText, value);
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
                CottonStorageCleanupPolicyText.ClearThumbnailsTitle,
                CottonStorageCleanupPolicyText.ClearThumbnailsMessage,
                CottonStorageCleanupPolicyText.ClearThumbnailsAction,
                _storageManagementService.ClearThumbnailCacheAsync,
                CottonStorageCleanupPolicyText.ThumbnailsClearedStatus);
        }

        private Task ClearDownloadedFilesAsync()
        {
            return ClearAsync(
                CottonStorageCleanupPolicyText.ClearDownloadedFilesTitle,
                CottonStorageCleanupPolicyText.ClearDownloadedFilesMessage,
                CottonStorageCleanupPolicyText.ClearDownloadedFilesAction,
                _storageManagementService.ClearDownloadedFilesAsync,
                CottonStorageCleanupPolicyText.DownloadedFilesClearedStatus);
        }

        private Task ClearFolderListingsAsync()
        {
            return ClearAsync(
                CottonStorageCleanupPolicyText.ClearFolderListingsTitle,
                CottonStorageCleanupPolicyText.ClearFolderListingsMessage,
                CottonStorageCleanupPolicyText.ClearFolderListingsAction,
                _storageManagementService.ClearFolderListingsCacheAsync,
                CottonStorageCleanupPolicyText.FolderListingsClearedStatus);
        }

        private async Task ClearTemporaryUploadsAsync()
        {
            await RunStorageActionAsync(
                async () =>
                {
                    bool confirmed = await _dialogService.ShowConfirmationAsync(
                        CottonStorageCleanupPolicyText.ClearTemporaryUploadsTitle,
                        CottonStorageCleanupPolicyText.ClearTemporaryUploadsMessage,
                        CottonStorageCleanupPolicyText.ClearTemporaryUploadsAction,
                        CottonStorageCleanupPolicyText.CancelAction);
                    if (!confirmed)
                    {
                        Status = null;
                        return;
                    }

                    CottonTransferStagedFileCleanupResult result =
                        await _storageManagementService.ClearTemporaryUploadsAsync(CancellationToken.None);
                    CottonStorageSummary summary = await _storageManagementService.GetSummaryAsync();
                    ShowSummary(summary);
                    Status = CottonStorageCleanupPolicyText.CreateTemporaryUploadsClearedStatus(result);
                },
                "Could not clear temporary uploads.",
                RefreshSummaryAfterClearFailureAsync);
        }

        private Task ClearAllAsync()
        {
            return ClearAsync(
                CottonStorageCleanupPolicyText.ClearAllTitle,
                CottonStorageCleanupPolicyText.ClearAllMessage,
                CottonStorageCleanupPolicyText.ClearAllAction,
                _storageManagementService.ClearAllCachedFilesAsync,
                CottonStorageCleanupPolicyText.AllCachedFilesClearedStatus);
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
                        CottonStorageCleanupPolicyText.CancelAction);
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
            TransferStagingSizeText = CottonFileSizeFormatter.Format(summary.TransferStaging.SizeBytes);
            TransferStagingFileCountText = FormatFileCount(summary.TransferStaging.FileCount);
            ShowOnDeviceStorage(summary.OnDeviceStorage);
            ShowStorageBudget(summary.Budget);
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

        private void ShowStorageBudget(CottonStorageBudgetSummary budget)
        {
            ArgumentNullException.ThrowIfNull(budget);

            StorageBudgetSummaryText = budget.SummaryText;
            ProtectedOfflineText = budget.ProtectedOfflineText;
            StorageBudgetBuckets.Clear();
            foreach (CottonStorageBudgetBucketSnapshot bucket in budget.Buckets)
            {
                StorageBudgetBuckets.Add(bucket);
            }
        }

        private void RaiseCommandStatesChanged()
        {
            LoadCommand.RaiseCanExecuteChanged();
            ClearThumbnailsCommand.RaiseCanExecuteChanged();
            ClearFolderListingsCommand.RaiseCanExecuteChanged();
            ClearTemporaryUploadsCommand.RaiseCanExecuteChanged();
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
