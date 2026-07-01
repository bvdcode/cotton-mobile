// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

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
        private const string CurrentActionSuffix = " (current)";

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
        private CottonTrashSelectionSnapshot _selection = CottonTrashSelectionSnapshot.Empty;
        private bool _isSelectionModeActive;
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
            ToggleSelectionModeCommand = new AsyncCommand(
                ToggleSelectionModeAsync,
                LogUnhandledCommandException,
                () => !IsBusy);
            EmptyTrashCommand = new AsyncCommand(EmptyTrashAsync, LogUnhandledCommandException, CanEmptyTrash);
            ToggleSelectionCommand = new AsyncCommand<CottonFileBrowserEntry>(
                ToggleSelectionAsync,
                LogUnhandledCommandException,
                _ => !IsBusy);
            CancelSelectionCommand = new AsyncCommand(CancelSelectionAsync, LogUnhandledCommandException, () => !IsBusy);
            RestoreSelectionCommand = new AsyncCommand(
                RestoreSelectionAsync,
                LogUnhandledCommandException,
                () => !IsBusy && Selection.IsActive);
            DeleteForeverSelectionCommand = new AsyncCommand(
                DeleteForeverSelectionAsync,
                LogUnhandledCommandException,
                () => !IsBusy && Selection.IsActive);
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

        public AsyncCommand ToggleSelectionModeCommand { get; }

        public AsyncCommand EmptyTrashCommand { get; }

        public AsyncCommand<CottonFileBrowserEntry> ToggleSelectionCommand { get; }

        public AsyncCommand CancelSelectionCommand { get; }

        public AsyncCommand RestoreSelectionCommand { get; }

        public AsyncCommand DeleteForeverSelectionCommand { get; }

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
                    ToggleSelectionModeCommand.RaiseCanExecuteChanged();
                    EmptyTrashCommand.RaiseCanExecuteChanged();
                    ToggleSelectionCommand.RaiseCanExecuteChanged();
                    CancelSelectionCommand.RaiseCanExecuteChanged();
                    RestoreSelectionCommand.RaiseCanExecuteChanged();
                    DeleteForeverSelectionCommand.RaiseCanExecuteChanged();
                    RestoreCommand.RaiseCanExecuteChanged();
                    DeleteForeverCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(IsListVisible));
                    OnPropertyChanged(nameof(IsTileVisible));
                    OnPropertyChanged(nameof(IsSelectionBarVisible));
                    OnPropertyChanged(nameof(IsBulkActionEnabled));
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

        public CottonTrashSelectionSnapshot Selection
        {
            get => _selection;
            private set
            {
                if (SetProperty(ref _selection, value))
                {
                    OnPropertyChanged(nameof(SelectionTitleText));
                    OnPropertyChanged(nameof(SelectionDetailText));
                    OnPropertyChanged(nameof(IsBulkActionEnabled));
                    RestoreSelectionCommand.RaiseCanExecuteChanged();
                    DeleteForeverSelectionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsSelectionModeActive
        {
            get => _isSelectionModeActive;
            private set
            {
                if (SetProperty(ref _isSelectionModeActive, value))
                {
                    OnPropertyChanged(nameof(IsSelectionBarVisible));
                    OnPropertyChanged(nameof(IsTrashEntryActionsVisible));
                    OnPropertyChanged(nameof(SelectionToolbarText));
                    OnPropertyChanged(nameof(SelectionTitleText));
                    OnPropertyChanged(nameof(SelectionDetailText));
                    EmptyTrashCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsSelectionBarVisible => IsSelectionModeActive && !IsBusy;

        public bool IsTrashEntryActionsVisible => !IsSelectionModeActive;

        public bool IsBulkActionEnabled => Selection.IsActive && !IsBusy;

        public string SelectionToolbarText => IsSelectionModeActive ? "Done" : "Select";

        public string SelectionTitleText => Selection.IsActive ? Selection.TitleText : "Select trash items";

        public string SelectionDetailText => Selection.IsActive ? Selection.DetailText : "Tap items to select them.";

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

        private Task ToggleSelectionModeAsync()
        {
            if (IsSelectionModeActive)
            {
                ClearSelection();
                return Task.CompletedTask;
            }

            IsSelectionModeActive = true;
            Status = "Tap trash items to select them.";
            RefreshSelectionSnapshot();
            return Task.CompletedTask;
        }

        private async Task EmptyTrashAsync()
        {
            CottonFileBrowserEntry[] trashItems = _allItems.ToArray();
            CottonTrashEmptyActionSnapshot emptyAction = CottonTrashEmptyActionSnapshot.Create(
                trashItems.Length,
                IsBusy,
                IsSelectionModeActive);
            if (!emptyAction.IsEnabled)
            {
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = CottonTrashEmptyStatusText.OfflineUnavailableStatus;
                return;
            }

            bool confirmed = await _dialogService.ShowConfirmationAsync(
                CottonTrashEmptyStatusText.ConfirmTitle,
                emptyAction.ConfirmationMessage,
                emptyAction.Label,
                CancelAction);
            if (!confirmed)
            {
                Status = CottonTrashEmptyStatusText.CancelledStatus;
                return;
            }

            ClearSelection();
            IsBusy = true;
            var deletedIds = new HashSet<Guid>();
            try
            {
                Status = CottonTrashEmptyStatusText.CreateDeletingStatus(trashItems.Length);
                for (int index = 0; index < trashItems.Length; index++)
                {
                    CottonFileBrowserEntry item = trashItems[index];
                    Status = CottonTrashEmptyStatusText.CreateDeletingItemStatus(
                        index + 1,
                        trashItems.Length,
                        item.Name);
                    try
                    {
                        await _trashPermanentDeleteService.DeleteForeverAsync(_instanceUri, item);
                        deletedIds.Add(item.Id);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "Cotton mobile trash empty failed {ItemId}.", item.Id);
                    }
                }

                await RefreshAfterEmptyTrashAsync(deletedIds, trashItems.Length);
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogInformation(exception, "Cotton mobile trash empty cancelled.");
                Status = CottonTrashEmptyStatusText.CancelledStatus;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile trash empty failed.");
                Status = CottonTrashEmptyStatusText.FailedStatus;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private Task ToggleSelectionAsync(CottonFileBrowserEntry item)
        {
            if (!IsSelectionModeActive || item is null)
            {
                return Task.CompletedTask;
            }

            HashSet<Guid> selectedIds = CreateSelectedItemIdSet();
            if (!selectedIds.Add(item.Id))
            {
                selectedIds.Remove(item.Id);
            }

            ApplySelection(selectedIds);
            return Task.CompletedTask;
        }

        private Task CancelSelectionAsync()
        {
            ClearSelection();
            Status = CottonTrashBulkStatusText.CancelledStatus;
            return Task.CompletedTask;
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

            ClearSelection();
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

        private async Task RestoreSelectionAsync()
        {
            if (IsBusy || !Selection.IsActive)
            {
                return;
            }

            CottonFileBrowserEntry[] selectedItems = ResolveSelectedItems();
            if (selectedItems.Length == 0)
            {
                ClearSelection();
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = CottonTrashBulkStatusText.RestoreOfflineUnavailableStatus;
                return;
            }

            CottonTrashSelectionSnapshot selection = CottonTrashSelectionSnapshot.Create(selectedItems);
            bool confirmed = await _dialogService.ShowConfirmationAsync(
                CottonTrashBulkStatusText.RestoreTitle,
                CottonTrashBulkStatusText.CreateRestoreConfirmMessage(selection.FileCount, selection.FolderCount),
                CottonTrashBulkStatusText.RestoreAction,
                CancelAction);
            if (!confirmed)
            {
                Status = CottonTrashBulkStatusText.CancelledStatus;
                return;
            }

            IsBusy = true;
            int restoredCount = 0;
            try
            {
                Status = CottonTrashBulkStatusText.CreateRestoringStatus(selectedItems.Length);
                for (int index = 0; index < selectedItems.Length; index++)
                {
                    CottonFileBrowserEntry item = selectedItems[index];
                    Status = CottonTrashBulkStatusText.CreateRestoringItemStatus(
                        index + 1,
                        selectedItems.Length,
                        item.Name);
                    try
                    {
                        CottonTrashRestoreResult result = await _trashRestoreService.RestoreAsync(
                            _instanceUri,
                            item.Id,
                            item.Type,
                            CottonTrashRestoreRetryMode.None);
                        if (result.IsRestored)
                        {
                            restoredCount++;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "Cotton mobile trash bulk restore failed {ItemId}.", item.Id);
                    }
                }

                await RefreshAfterBulkRestoreAsync(restoredCount, selectedItems.Length);
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogInformation(exception, "Cotton mobile trash bulk restore cancelled.");
                Status = CottonTrashBulkStatusText.CancelledStatus;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile trash bulk restore failed.");
                Status = CottonTrashBulkStatusText.RestoreFailedStatus;
            }
            finally
            {
                ClearSelection();
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private async Task DeleteForeverSelectionAsync()
        {
            if (IsBusy || !Selection.IsActive)
            {
                return;
            }

            CottonFileBrowserEntry[] selectedItems = ResolveSelectedItems();
            if (selectedItems.Length == 0)
            {
                ClearSelection();
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = CottonTrashBulkStatusText.DeleteOfflineUnavailableStatus;
                return;
            }

            CottonTrashSelectionSnapshot selection = CottonTrashSelectionSnapshot.Create(selectedItems);
            bool confirmed = await _dialogService.ShowConfirmationAsync(
                CottonTrashBulkStatusText.DeleteForeverTitle,
                CottonTrashBulkStatusText.CreateDeleteForeverConfirmMessage(selection.FileCount, selection.FolderCount),
                CottonTrashBulkStatusText.DeleteForeverAction,
                CancelAction);
            if (!confirmed)
            {
                Status = CottonTrashBulkStatusText.CancelledStatus;
                return;
            }

            IsBusy = true;
            int deletedCount = 0;
            try
            {
                Status = CottonTrashBulkStatusText.CreateDeletingStatus(selectedItems.Length);
                for (int index = 0; index < selectedItems.Length; index++)
                {
                    CottonFileBrowserEntry item = selectedItems[index];
                    Status = CottonTrashBulkStatusText.CreateDeletingItemStatus(
                        index + 1,
                        selectedItems.Length,
                        item.Name);
                    try
                    {
                        await _trashPermanentDeleteService.DeleteForeverAsync(_instanceUri, item);
                        deletedCount++;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "Cotton mobile trash bulk delete failed {ItemId}.", item.Id);
                    }
                }

                await RefreshAfterBulkDeleteAsync(deletedCount, selectedItems.Length);
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogInformation(exception, "Cotton mobile trash bulk delete cancelled.");
                Status = CottonTrashBulkStatusText.CancelledStatus;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile trash bulk delete failed.");
                Status = CottonTrashBulkStatusText.DeleteFailedStatus;
            }
            finally
            {
                ClearSelection();
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

        private async Task RefreshAfterBulkRestoreAsync(int restoredCount, int totalCount)
        {
            try
            {
                CottonTrashListSnapshot snapshot = await LoadSnapshotAsync();
                ShowSnapshot(snapshot);
                Status = CreateBulkRestoreResultStatus(restoredCount, totalCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile trash refresh after bulk restore failed.");
                RemoveSelectedItems();
                Status = CreateBulkRestoreResultStatus(restoredCount, totalCount);
            }
        }

        private async Task RefreshAfterBulkDeleteAsync(int deletedCount, int totalCount)
        {
            try
            {
                CottonTrashListSnapshot snapshot = await LoadSnapshotAsync();
                ShowSnapshot(snapshot);
                Status = CreateBulkDeleteResultStatus(deletedCount, totalCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile trash refresh after bulk delete failed.");
                RemoveSelectedItems();
                Status = CreateBulkDeleteResultStatus(deletedCount, totalCount);
            }
        }

        private async Task RefreshAfterEmptyTrashAsync(IReadOnlySet<Guid> deletedIds, int totalCount)
        {
            try
            {
                CottonTrashListSnapshot snapshot = await LoadSnapshotAsync();
                ShowSnapshot(snapshot);
                Status = CreateEmptyTrashResultStatus(deletedIds.Count, totalCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile trash refresh after empty failed.");
                RemoveItems(deletedIds);
                Status = CreateEmptyTrashResultStatus(deletedIds.Count, totalCount);
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
            Selection = CottonTrashSelectionSnapshot.Empty;
            IsSelectionModeActive = false;
            NotifyPresentationStateChanged();
        }

        private void RemoveItem(CottonFileBrowserEntry item)
        {
            _allItems.RemoveAll(entry => entry.Id == item.Id);
            ApplyPresentationState();
        }

        private void RemoveSelectedItems()
        {
            HashSet<Guid> selectedIds = CreateSelectedItemIdSet();
            RemoveItems(selectedIds);
        }

        private void RemoveItems(IReadOnlySet<Guid> itemIds)
        {
            _allItems.RemoveAll(entry => itemIds.Contains(entry.Id));
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
            RefreshSelectionSnapshot();
            NotifyPresentationStateChanged();
        }

        private void NotifyPresentationStateChanged()
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsListVisible));
            OnPropertyChanged(nameof(IsTileVisible));
            OnPropertyChanged(nameof(IsSearchVisible));
            OnPropertyChanged(nameof(SearchButtonDescription));
            OnPropertyChanged(nameof(IsSortButtonVisible));
            OnPropertyChanged(nameof(IsViewButtonVisible));
            OnPropertyChanged(nameof(SortButtonText));
            OnPropertyChanged(nameof(IsSelectionBarVisible));
            OnPropertyChanged(nameof(IsTrashEntryActionsVisible));
            OnPropertyChanged(nameof(SelectionToolbarText));
            OnPropertyChanged(nameof(SelectionTitleText));
            OnPropertyChanged(nameof(SelectionDetailText));
            OnPropertyChanged(nameof(IsBulkActionEnabled));
            EmptyTrashCommand.RaiseCanExecuteChanged();
        }

        private HashSet<Guid> CreateSelectedItemIdSet()
        {
            return _allItems
                .Where(entry => entry.IsSelected)
                .Select(entry => entry.Id)
                .ToHashSet();
        }

        private void ApplySelection(IReadOnlySet<Guid> selectedIds)
        {
            bool changed = false;
            for (int index = 0; index < _allItems.Count; index++)
            {
                CottonFileBrowserEntry item = _allItems[index];
                bool shouldBeSelected = selectedIds.Contains(item.Id);
                if (item.IsSelected == shouldBeSelected)
                {
                    continue;
                }

                _allItems[index] = item.WithSelection(shouldBeSelected);
                changed = true;
            }

            if (changed)
            {
                ApplyPresentationState();
                return;
            }

            RefreshSelectionSnapshot();
        }

        private void ClearSelection()
        {
            IsSelectionModeActive = false;
            ApplySelection(new HashSet<Guid>());
        }

        private void RefreshSelectionSnapshot()
        {
            Selection = CottonTrashSelectionSnapshot.Create(_allItems.Where(entry => entry.IsSelected));
        }

        private CottonFileBrowserEntry[] ResolveSelectedItems()
        {
            HashSet<Guid> selectedIds = Selection.Entries.Select(entry => entry.Id).ToHashSet();
            return _allItems
                .Where(entry => selectedIds.Contains(entry.Id))
                .ToArray();
        }

        private static string CreateBulkRestoreResultStatus(int restoredCount, int totalCount)
        {
            if (restoredCount == totalCount)
            {
                return CottonTrashBulkStatusText.CreateRestoredStatus(restoredCount);
            }

            return restoredCount == 0
                ? CottonTrashBulkStatusText.RestoreFailedStatus
                : CottonTrashBulkStatusText.CreatePartialRestoreStatus(restoredCount, totalCount);
        }

        private static string CreateBulkDeleteResultStatus(int deletedCount, int totalCount)
        {
            if (deletedCount == totalCount)
            {
                return CottonTrashBulkStatusText.CreateDeletedStatus(deletedCount);
            }

            return deletedCount == 0
                ? CottonTrashBulkStatusText.DeleteFailedStatus
                : CottonTrashBulkStatusText.CreatePartialDeleteStatus(deletedCount, totalCount);
        }

        private static string CreateEmptyTrashResultStatus(int deletedCount, int totalCount)
        {
            if (deletedCount == totalCount)
            {
                return CottonTrashEmptyStatusText.CreateDeletedStatus(deletedCount);
            }

            return deletedCount == 0
                ? CottonTrashEmptyStatusText.FailedStatus
                : CottonTrashEmptyStatusText.CreatePartialDeleteStatus(deletedCount, totalCount);
        }

        private bool CanEmptyTrash()
        {
            return CottonTrashEmptyActionSnapshot.Create(
                    _allItems.Count,
                    IsBusy,
                    IsSelectionModeActive)
                .IsEnabled;
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

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile trash command exception.");
        }
    }
}
