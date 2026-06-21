using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class TrashViewModel : ViewModelBase
    {
        private const string CancelAction = "Cancel";

        private readonly Uri _instanceUri;
        private readonly ICottonTrashBrowserService _trashBrowserService;
        private readonly ICottonTrashRestoreService _trashRestoreService;
        private readonly ICottonTrashPermanentDeleteService _trashPermanentDeleteService;
        private readonly INetworkAccessService _networkAccess;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<TrashViewModel> _logger;
        private bool _isBusy;
        private string _summaryText = "Trash";
        private string _emptyMessage = "Trash is empty";
        private string _emptyDetails = "Deleted files and folders will appear here.";
        private string? _status;

        public TrashViewModel(
            Uri instanceUri,
            ICottonTrashBrowserService trashBrowserService,
            ICottonTrashRestoreService trashRestoreService,
            ICottonTrashPermanentDeleteService trashPermanentDeleteService,
            INetworkAccessService networkAccess,
            IUserDialogService dialogService,
            ILogger<TrashViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(trashBrowserService);
            ArgumentNullException.ThrowIfNull(trashRestoreService);
            ArgumentNullException.ThrowIfNull(trashPermanentDeleteService);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _trashBrowserService = trashBrowserService;
            _trashRestoreService = trashRestoreService;
            _trashPermanentDeleteService = trashPermanentDeleteService;
            _networkAccess = networkAccess;
            _dialogService = dialogService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            RestoreCommand = new AsyncCommand<CottonFileBrowserEntry>(
                RestoreAsync,
                LogUnhandledCommandException,
                _ => !IsBusy);
            DeleteForeverCommand = new AsyncCommand<CottonFileBrowserEntry>(
                DeleteForeverAsync,
                LogUnhandledCommandException,
                _ => !IsBusy);
        }

        public ObservableCollection<CottonFileBrowserEntry> Items { get; } = [];

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand<CottonFileBrowserEntry> RestoreCommand { get; }

        public AsyncCommand<CottonFileBrowserEntry> DeleteForeverCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoadCommand.RaiseCanExecuteChanged();
                    RestoreCommand.RaiseCanExecuteChanged();
                    DeleteForeverCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(IsListVisible));
                }
            }
        }

        public string SummaryText
        {
            get => _summaryText;
            private set => SetProperty(ref _summaryText, value);
        }

        public string EmptyMessage
        {
            get => _emptyMessage;
            private set => SetProperty(ref _emptyMessage, value);
        }

        public string EmptyDetails
        {
            get => _emptyDetails;
            private set => SetProperty(ref _emptyDetails, value);
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

        public bool IsEmpty => Items.Count == 0 && !IsBusy;

        public bool IsListVisible => Items.Count > 0;

        private async Task LoadAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            Status = CottonTrashListStatusText.LoadingStatus;
            try
            {
                if (!_networkAccess.HasInternetAccess)
                {
                    ShowOfflineState();
                    return;
                }

                CottonTrashListSnapshot snapshot = await LoadSnapshotAsync();
                ShowSnapshot(snapshot);
                Status = CottonTrashListStatusText.CreateLoadedStatus(snapshot.Items.Count);
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogInformation(exception, "Cotton mobile trash load cancelled.");
                Status = CottonTrashListStatusText.CancelledStatus;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile trash load failed.");
                Status = CottonTrashListStatusText.FailedStatus;
                EmptyMessage = "Could not load trash.";
                EmptyDetails = "Refresh to try again.";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private async Task RestoreAsync(CottonFileBrowserEntry item)
        {
            if (IsBusy)
            {
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = CottonTrashRestoreStatusText.OfflineUnavailableStatus;
                return;
            }

            bool confirmed = await _dialogService.ShowConfirmationAsync(
                CottonTrashRestoreStatusText.RestoreTitle,
                CottonTrashRestoreStatusText.CreateConfirmMessage(item.Name),
                CottonTrashRestoreStatusText.RestoreAction,
                CancelAction);
            if (!confirmed)
            {
                Status = CottonTrashRestoreStatusText.CancelledStatus;
                return;
            }

            IsBusy = true;
            try
            {
                await RestoreItemAsync(item, CottonTrashRestoreRetryMode.None);
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogInformation(exception, "Cotton mobile trash restore cancelled {ItemId}.", item.Id);
                Status = CottonTrashRestoreStatusText.CancelledStatus;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile trash restore failed {ItemId}.", item.Id);
                Status = CottonTrashRestoreStatusText.FailedStatus;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private async Task DeleteForeverAsync(CottonFileBrowserEntry item)
        {
            if (IsBusy)
            {
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = CottonTrashPermanentDeleteStatusText.OfflineUnavailableStatus;
                return;
            }

            bool confirmed = await _dialogService.ShowConfirmationAsync(
                CottonTrashPermanentDeleteStatusText.ConfirmTitle,
                CottonTrashPermanentDeleteStatusText.CreateConfirmMessage(item.Name, item.Type),
                CottonTrashPermanentDeleteStatusText.ConfirmAction,
                CancelAction);
            if (!confirmed)
            {
                Status = CottonTrashPermanentDeleteStatusText.CancelledStatus;
                return;
            }

            IsBusy = true;
            try
            {
                Status = CottonTrashPermanentDeleteStatusText.CreateDeletingStatus(item.Name);
                CottonTrashPermanentDeleteResult result = await _trashPermanentDeleteService.DeleteForeverAsync(
                    _instanceUri,
                    item);
                await RefreshAfterDeletedItemAsync(item, result);
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(exception, "Cotton mobile trash permanent delete rejected {ItemId}.", item.Id);
                Status = exception.Message;
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogInformation(exception, "Cotton mobile trash permanent delete cancelled {ItemId}.", item.Id);
                Status = CottonTrashPermanentDeleteStatusText.CancelledStatus;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile trash permanent delete failed {ItemId}.", item.Id);
                Status = CottonTrashPermanentDeleteStatusText.FailedStatus;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private async Task RestoreItemAsync(
            CottonFileBrowserEntry item,
            CottonTrashRestoreRetryMode retryMode)
        {
            if (!_networkAccess.HasInternetAccess)
            {
                Status = CottonTrashRestoreStatusText.OfflineUnavailableStatus;
                return;
            }

            Status = CottonTrashRestoreStatusText.CreateRestoringStatus(item.Name);
            CottonTrashRestoreResult result = await _trashRestoreService.RestoreAsync(
                _instanceUri,
                item.Id,
                item.Type,
                retryMode);
            await HandleTrashRestoreOutcomeAsync(item, result);
        }

        private async Task HandleTrashRestoreOutcomeAsync(
            CottonFileBrowserEntry item,
            CottonTrashRestoreResult result)
        {
            if (result.IsRestored)
            {
                await RefreshAfterRestoredItemAsync(item);
                return;
            }

            if (result.CanRetryWithCreateMissingParents
                && result.RetryMode != CottonTrashRestoreRetryMode.CreateMissingParents)
            {
                bool confirmed = await _dialogService.ShowConfirmationAsync(
                    CottonTrashRestoreStatusText.ParentMissingTitle,
                    CottonTrashRestoreStatusText.CreateParentMissingMessage(item.Name),
                    CottonTrashRestoreStatusText.CreateMissingParentsAction,
                    CancelAction);
                if (confirmed)
                {
                    await RestoreItemAsync(item, CottonTrashRestoreRetryMode.CreateMissingParents);
                    return;
                }

                Status = CottonTrashRestoreStatusText.ParentMissingStatus;
                return;
            }

            if (result.CanRetryWithOverwrite
                && result.RetryMode != CottonTrashRestoreRetryMode.Overwrite)
            {
                bool confirmed = await _dialogService.ShowConfirmationAsync(
                    CottonTrashRestoreStatusText.ConflictTitle,
                    CottonTrashRestoreStatusText.CreateConflictMessage(item.Name),
                    CottonTrashRestoreStatusText.OverwriteAction,
                    CancelAction);
                if (confirmed)
                {
                    await RestoreItemAsync(item, CottonTrashRestoreRetryMode.Overwrite);
                    return;
                }

                Status = CottonTrashRestoreStatusText.ConflictStatus;
                return;
            }

            Status = result.StatusText;
        }

        private async Task RefreshAfterRestoredItemAsync(CottonFileBrowserEntry item)
        {
            try
            {
                CottonTrashListSnapshot snapshot = await LoadSnapshotAsync();
                ShowSnapshot(snapshot);
                Status = CottonTrashRestoreStatusText.CreateRestoredStatus(item.Name);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Cotton mobile trash refresh after restore failed {ItemId}.",
                    item.Id);
                Items.Remove(item);
                Status = CottonTrashRestoreStatusText.CreateRestoredNeedsRefreshStatus(item.Name);
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private async Task RefreshAfterDeletedItemAsync(
            CottonFileBrowserEntry item,
            CottonTrashPermanentDeleteResult result)
        {
            try
            {
                CottonTrashListSnapshot snapshot = await LoadSnapshotAsync();
                ShowSnapshot(snapshot);
                Status = result.StatusText;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Cotton mobile trash refresh after permanent delete failed {ItemId}.",
                    item.Id);
                Items.Remove(item);
                Status = CottonTrashPermanentDeleteStatusText.CreateDeletedNeedsRefreshStatus(item.Name);
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private async Task<CottonTrashListSnapshot> LoadSnapshotAsync()
        {
            CottonFolderContent content = await _trashBrowserService.GetRootAsync(_instanceUri);
            return CottonTrashListSnapshot.Create(content);
        }

        private void ShowSnapshot(CottonTrashListSnapshot snapshot)
        {
            Items.Clear();
            foreach (CottonFileBrowserEntry item in snapshot.Items)
            {
                Items.Add(item);
            }

            SummaryText = snapshot.SummaryText;
            EmptyMessage = snapshot.EmptyMessage;
            EmptyDetails = snapshot.EmptyDetails;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsListVisible));
        }

        private void ShowOfflineState()
        {
            Items.Clear();
            SummaryText = "Trash needs internet";
            EmptyMessage = "Trash unavailable offline";
            EmptyDetails = "Connect and refresh to view deleted items.";
            Status = CottonTrashListStatusText.OfflineUnavailableStatus;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsListVisible));
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile trash command exception.");
        }
    }
}
