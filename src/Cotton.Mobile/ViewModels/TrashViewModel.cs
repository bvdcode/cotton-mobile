using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class TrashViewModel : ViewModelBase
    {
        private const string CancelAction = "Cancel";
        private const string SortNameAction = "Name";
        private const string SortUpdatedAction = "Newest";
        private const string SortSizeAction = "Size";
        private const string SortTypeAction = "Type";
        private const string ViewListAction = "List";
        private const string ViewTilesAction = "Tiles";
        private const string CurrentActionPrefix = "✓ ";

        private readonly Uri _instanceUri;
        private readonly ICottonTrashBrowserService _trashBrowserService;
        private readonly ICottonTrashRestoreService _trashRestoreService;
        private readonly ICottonTrashPermanentDeleteService _trashPermanentDeleteService;
        private readonly IFileBrowserPreferenceStore _preferenceStore;
        private readonly INetworkAccessService _networkAccess;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<TrashViewModel> _logger;
        private readonly List<CottonFileBrowserEntry> _allItems = [];
        private bool _isBusy;
        private string _searchText = string.Empty;
        private bool _isSearchOpen;
        private CottonFileBrowserSortMode _sortMode = CottonFileBrowserSortMode.Name;
        private CottonFileBrowserViewMode _viewMode = CottonFileBrowserViewMode.List;
        private string _summaryText = "Trash";
        private string _emptyMessage = "Trash is empty";
        private string _emptyDetails = "Deleted files and folders will appear here.";
        private string? _status;

        public TrashViewModel(
            Uri instanceUri,
            ICottonTrashBrowserService trashBrowserService,
            ICottonTrashRestoreService trashRestoreService,
            ICottonTrashPermanentDeleteService trashPermanentDeleteService,
            IFileBrowserPreferenceStore preferenceStore,
            INetworkAccessService networkAccess,
            IUserDialogService dialogService,
            ILogger<TrashViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(trashBrowserService);
            ArgumentNullException.ThrowIfNull(trashRestoreService);
            ArgumentNullException.ThrowIfNull(trashPermanentDeleteService);
            ArgumentNullException.ThrowIfNull(preferenceStore);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _trashBrowserService = trashBrowserService;
            _trashRestoreService = trashRestoreService;
            _trashPermanentDeleteService = trashPermanentDeleteService;
            _preferenceStore = preferenceStore;
            _networkAccess = networkAccess;
            _dialogService = dialogService;
            _logger = logger;
            ApplyPreferences(LoadPreferences());
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            ToggleSearchCommand = new AsyncCommand(ToggleSearchAsync, LogUnhandledCommandException, () => !IsBusy);
            ShowSortActionsCommand = new AsyncCommand(ShowSortActionsAsync, LogUnhandledCommandException, () => !IsBusy);
            ShowViewActionsCommand = new AsyncCommand(ShowViewActionsAsync, LogUnhandledCommandException, () => !IsBusy);
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

        public AsyncCommand ToggleSearchCommand { get; }

        public AsyncCommand ShowSortActionsCommand { get; }

        public AsyncCommand ShowViewActionsCommand { get; }

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
                    ToggleSearchCommand.RaiseCanExecuteChanged();
                    ShowSortActionsCommand.RaiseCanExecuteChanged();
                    ShowViewActionsCommand.RaiseCanExecuteChanged();
                    RestoreCommand.RaiseCanExecuteChanged();
                    DeleteForeverCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(IsListVisible));
                    OnPropertyChanged(nameof(IsTileVisible));
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value ?? string.Empty))
                {
                    ApplyPresentationState();
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

        public bool IsListVisible => Items.Count > 0 && _viewMode == CottonFileBrowserViewMode.List;

        public bool IsTileVisible => Items.Count > 0 && _viewMode == CottonFileBrowserViewMode.Tiles;

        public bool IsSearchVisible => _isSearchOpen || !string.IsNullOrWhiteSpace(SearchText);

        public string SearchButtonText => IsSearchVisible ? "×" : "⌕";

        public string SearchButtonDescription
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    return "Clear trash search";
                }

                return _isSearchOpen ? "Close trash search" : "Search trash";
            }
        }

        public bool IsSortButtonVisible => !IsSearchVisible;

        public bool IsViewButtonVisible => !IsSearchVisible;

        public string SortButtonText => CottonTrashListDisplayState.FormatSortButtonText(_sortMode);

        public string ViewButtonText => _viewMode == CottonFileBrowserViewMode.List ? "☰" : "▦";

        private Task ToggleSearchAsync()
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                SearchText = string.Empty;
                return Task.CompletedTask;
            }

            _isSearchOpen = !_isSearchOpen;
            ApplyPresentationState();
            return Task.CompletedTask;
        }

        private async Task ShowSortActionsAsync()
        {
            string nameAction = CreateCurrentActionLabel(SortNameAction, _sortMode == CottonFileBrowserSortMode.Name);
            string updatedAction = CreateCurrentActionLabel(
                SortUpdatedAction,
                _sortMode == CottonFileBrowserSortMode.Updated);
            string typeAction = CreateCurrentActionLabel(SortTypeAction, _sortMode == CottonFileBrowserSortMode.Type);
            string sizeAction = CreateCurrentActionLabel(SortSizeAction, _sortMode == CottonFileBrowserSortMode.Size);
            string? action = await _dialogService.ShowActionSheetAsync(
                "Sort trash by",
                CancelAction,
                null,
                nameAction,
                updatedAction,
                typeAction,
                sizeAction);

            switch (NormalizeAction(action))
            {
                case SortNameAction:
                    SetSortMode(CottonFileBrowserSortMode.Name);
                    break;
                case SortUpdatedAction:
                    SetSortMode(CottonFileBrowserSortMode.Updated);
                    break;
                case SortTypeAction:
                    SetSortMode(CottonFileBrowserSortMode.Type);
                    break;
                case SortSizeAction:
                    SetSortMode(CottonFileBrowserSortMode.Size);
                    break;
            }
        }

        private async Task ShowViewActionsAsync()
        {
            string listAction = CreateCurrentActionLabel(ViewListAction, _viewMode == CottonFileBrowserViewMode.List);
            string tilesAction = CreateCurrentActionLabel(ViewTilesAction, _viewMode == CottonFileBrowserViewMode.Tiles);
            string? action = await _dialogService.ShowActionSheetAsync(
                "View trash as",
                CancelAction,
                null,
                listAction,
                tilesAction);

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
                RemoveItem(item);
                Status = CottonTrashRestoreStatusText.CreateRestoredNeedsRefreshStatus(item.Name);
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
                RemoveItem(item);
                Status = CottonTrashPermanentDeleteStatusText.CreateDeletedNeedsRefreshStatus(item.Name);
            }
        }

        private async Task<CottonTrashListSnapshot> LoadSnapshotAsync()
        {
            CottonFolderContent content = await _trashBrowserService.GetRootAsync(_instanceUri);
            return CottonTrashListSnapshot.Create(content);
        }

        private void ShowSnapshot(CottonTrashListSnapshot snapshot)
        {
            _allItems.Clear();
            foreach (CottonFileBrowserEntry item in snapshot.Items)
            {
                _allItems.Add(item);
            }

            ApplyPresentationState();
        }

        private void ShowOfflineState()
        {
            _allItems.Clear();
            Items.Clear();
            SummaryText = "Trash needs internet";
            EmptyMessage = "Trash unavailable offline";
            EmptyDetails = "Connect and refresh to view deleted items.";
            Status = CottonTrashListStatusText.OfflineUnavailableStatus;
            NotifyPresentationStateChanged();
        }

        private void RemoveItem(CottonFileBrowserEntry item)
        {
            _allItems.RemoveAll(entry => entry.Id == item.Id);
            ApplyPresentationState();
        }

        private void ApplyPresentationState()
        {
            CottonTrashListDisplayState state = CottonTrashListDisplayState.Create(
                _allItems,
                SearchText,
                _isSearchOpen,
                _sortMode,
                _viewMode);
            Items.Clear();
            foreach (CottonFileBrowserEntry item in state.Items)
            {
                Items.Add(item);
            }

            SummaryText = state.SummaryText;
            EmptyMessage = state.EmptyMessage;
            EmptyDetails = state.EmptyDetails;
            NotifyPresentationStateChanged();
        }

        private void NotifyPresentationStateChanged()
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsListVisible));
            OnPropertyChanged(nameof(IsTileVisible));
            OnPropertyChanged(nameof(IsSearchVisible));
            OnPropertyChanged(nameof(SearchButtonText));
            OnPropertyChanged(nameof(SearchButtonDescription));
            OnPropertyChanged(nameof(IsSortButtonVisible));
            OnPropertyChanged(nameof(IsViewButtonVisible));
            OnPropertyChanged(nameof(SortButtonText));
            OnPropertyChanged(nameof(ViewButtonText));
        }

        private CottonFileBrowserPreferences LoadPreferences()
        {
            try
            {
                return _preferenceStore.Get();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile trash browser preferences.");
                return new CottonFileBrowserPreferences(
                    CottonFileBrowserViewMode.List,
                    CottonFileBrowserSortMode.Name);
            }
        }

        private void ApplyPreferences(CottonFileBrowserPreferences preferences)
        {
            _viewMode = preferences.ViewMode;
            _sortMode = preferences.SortMode;
        }

        private void SetSortMode(CottonFileBrowserSortMode sortMode)
        {
            _sortMode = sortMode;
            ApplyPresentationState();
            SaveSortModePreference(sortMode);
        }

        private void SetViewMode(CottonFileBrowserViewMode viewMode)
        {
            _viewMode = viewMode;
            ApplyPresentationState();
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
                _logger.LogWarning(exception, "Failed to save Cotton mobile trash sort mode preference.");
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
                _logger.LogWarning(exception, "Failed to save Cotton mobile trash view mode preference.");
            }
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

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile trash command exception.");
        }
    }
}
