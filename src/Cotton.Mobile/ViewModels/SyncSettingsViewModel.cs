// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsViewModel : ViewModelBase
    {
        private readonly SyncRootManager _rootManager;
        private readonly SyncExecutionWorkflow _executionWorkflow;
        private readonly SyncRootSetupCoordinator _rootSetupCoordinator;
        private readonly INetworkAccessService _networkAccess;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<SyncSettingsViewModel> _logger;
        private Uri? _instanceUri;
        private string? _accountScopeKey;
        private bool _isBusy;
        private bool _canRunAll;
        private string _summaryText = "No folders syncing";
        private string? _status;
        private bool _isEmptyVisible = true;

        public SyncSettingsViewModel(
            SyncRootManager rootManager,
            SyncExecutionWorkflow executionWorkflow,
            SyncRootSetupCoordinator rootSetupCoordinator,
            INetworkAccessService networkAccess,
            IUserDialogService dialogService,
            ILogger<SyncSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(rootManager);
            ArgumentNullException.ThrowIfNull(executionWorkflow);
            ArgumentNullException.ThrowIfNull(rootSetupCoordinator);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _rootManager = rootManager;
            _executionWorkflow = executionWorkflow;
            _rootSetupCoordinator = rootSetupCoordinator;
            _networkAccess = networkAccess;
            _dialogService = dialogService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            AddRootCommand = new AsyncCommand(AddRootAsync, LogUnhandledCommandException, CanAddRoot);
            RunAllCommand = new AsyncCommand(RunAllAsync, LogUnhandledCommandException, CanRunAll);
            RunRootCommand = new AsyncCommand<CottonSyncRootListItem>(
                RunRootAsync,
                LogUnhandledCommandException,
                item => !IsBusy && item.CanRunNow);
            StopRootCommand = new AsyncCommand<CottonSyncRootListItem>(
                StopRootAsync,
                LogUnhandledCommandException,
                item => !IsBusy && item.CanStopSync);
            PauseRootCommand = new AsyncCommand<CottonSyncRootListItem>(
                item => SetRootPausedAsync(item, isPaused: true),
                LogUnhandledCommandException,
                item => !IsBusy && item.CanPauseSync);
            ResumeRootCommand = new AsyncCommand<CottonSyncRootListItem>(
                item => SetRootPausedAsync(item, isPaused: false),
                LogUnhandledCommandException,
                item => !IsBusy && item.CanResumeSync);
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand AddRootCommand { get; }

        public AsyncCommand RunAllCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> RunRootCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> StopRootCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> PauseRootCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> ResumeRootCommand { get; }

        public RangeObservableCollection<CottonSyncRootListItem> Roots { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoadCommand.RaiseCanExecuteChanged();
                    AddRootCommand.RaiseCanExecuteChanged();
                    RunAllCommand.RaiseCanExecuteChanged();
                    RunRootCommand.RaiseCanExecuteChanged();
                    StopRootCommand.RaiseCanExecuteChanged();
                    PauseRootCommand.RaiseCanExecuteChanged();
                    ResumeRootCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SummaryText
        {
            get => _summaryText;
            private set => SetProperty(ref _summaryText, value);
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

        public bool IsEmptyVisible
        {
            get => _isEmptyVisible;
            private set
            {
                if (SetProperty(ref _isEmptyVisible, value))
                {
                    OnPropertyChanged(nameof(IsSummaryVisible));
                    OnPropertyChanged(nameof(IsListVisible));
                }
            }
        }

        public bool IsSummaryVisible => !IsEmptyVisible;

        public bool IsListVisible => !IsEmptyVisible;

        public bool IsRunAllVisible => _canRunAll;

        public void Configure(Uri instanceUri, string accountScopeKey)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);

            _instanceUri = instanceUri;
            _accountScopeKey = accountScopeKey.Trim();
            AddRootCommand.RaiseCanExecuteChanged();
        }

        public async Task LoadForInstanceAsync(Uri instanceUri, string accountScopeKey)
        {
            Configure(instanceUri, accountScopeKey);
            await LoadAsync();
        }

        public void Clear()
        {
            _instanceUri = null;
            _accountScopeKey = null;
            Roots.ReplaceWith([]);
            SummaryText = "No folders syncing";
            Status = null;
            IsEmptyVisible = true;
            _canRunAll = false;
            OnPropertyChanged(nameof(IsRunAllVisible));
            RunAllCommand.RaiseCanExecuteChanged();
            AddRootCommand.RaiseCanExecuteChanged();
        }

        private async Task AddRootAsync()
        {
            Uri? instanceUri = _instanceUri;
            string? accountScopeKey = _accountScopeKey;
            if (instanceUri is null || string.IsNullOrWhiteSpace(accountScopeKey))
            {
                Status = "Could not add a sync folder for this account.";
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = "Connect to the internet to add a sync folder.";
                return;
            }

            IsBusy = true;
            try
            {
                SyncRootSetupResult result = await _rootSetupCoordinator.AddBidirectionalRootAsync(
                    instanceUri,
                    accountScopeKey);
                if (result.Status == SyncRootSetupStatus.Cancelled)
                {
                    Status = null;
                    return;
                }

                await LoadRootsAsync(instanceUri);
                Status = result.Message;
            }
            catch (OperationCanceledException)
            {
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to add Cotton mobile sync root.");
                Status = "Could not add this sync folder.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadAsync()
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = "Could not inspect sync folders.";
                return;
            }

            IsBusy = true;
            try
            {
                await LoadRootsAsync(instanceUri);
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile sync roots.");
                Status = "Could not inspect sync folders.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RunRootAsync(CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = "Could not run sync for this instance.";
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = CottonSyncRootRunRouting.CreateOfflineUnavailableStatus(item.Direction);
                return;
            }

            CottonSyncDirection statusDirection = item.Direction;
            IsBusy = true;
            try
            {
                SyncRootCollectionSnapshot collection = await LoadRootCollectionAsync(instanceUri);
                CottonSyncRootSnapshot? root = collection.Roots.FirstOrDefault(root => root.Id == item.Id);
                if (root is null)
                {
                    ShowRoots(collection);
                    Status = "Sync folder is no longer configured.";
                    return;
                }

                statusDirection = root.Direction;
                if (collection.PausedRootIds.Contains(root.Id))
                {
                    ShowRoots(collection);
                    Status = CottonSyncRootManagementText.RootPausedStatus;
                    return;
                }

                Status = CottonSyncRootRunRouting.CreateStartingStatus(root);
                Status = await _executionWorkflow.RunRootAsync(
                    instanceUri,
                    root,
                    status => Status = status);
                ShowRoots(await LoadRootCollectionAsync(instanceUri));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to run Cotton mobile sync root.");
                Status = CottonSyncRootRunRouting.CreateFailedStatus(statusDirection);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RunAllAsync()
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = "Could not run sync for this instance.";
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = CottonSyncSettingsRunStatusText.OfflineUnavailableStatus;
                return;
            }

            IsBusy = true;
            try
            {
                Status = CottonSyncSettingsRunStatusText.StartingAllStatus;
                SyncRootCollectionSnapshot collection = await LoadRootCollectionAsync(instanceUri);
                IReadOnlyList<CottonSyncRootSnapshot> runnableRoots =
                    CottonSyncRootRunCapability.GetRunnableRoots(
                        collection.Roots,
                        collection.PausedRootIds);
                Status = await _executionWorkflow.RunAllAsync(
                    instanceUri,
                    runnableRoots,
                    status => Status = status);
                ShowRoots(await LoadRootCollectionAsync(instanceUri));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to run Cotton mobile sync roots.");
                Status = CottonSyncSettingsRunStatusText.FailedStatus;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task StopRootAsync(CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = CottonSyncRootManagementText.StopFailedStatus;
                return;
            }

            IsBusy = true;
            try
            {
                SyncRootCollectionSnapshot collection = await LoadRootCollectionAsync(instanceUri);
                CottonSyncRootSnapshot? root = collection.Roots.FirstOrDefault(root => root.Id == item.Id);
                if (root is null)
                {
                    ShowRoots(collection);
                    Status = CottonSyncRootManagementText.RootMissingStatus;
                    return;
                }

                bool confirmed = await _dialogService.ShowConfirmationAsync(
                    CottonSyncRootManagementText.CreateStopTitle(root.CloudFolder.FolderName),
                    CottonSyncRootManagementText.StopMessage,
                    CottonSyncRootManagementText.StopAction,
                    CottonSyncRootManagementText.CancelAction);
                if (!confirmed)
                {
                    Status = null;
                    return;
                }

                bool removed = await _rootManager.StopAsync(instanceUri, root);
                ShowRoots(await LoadRootCollectionAsync(instanceUri));
                Status = removed
                    ? CottonSyncRootManagementText.CreateStoppedStatus(root.CloudFolder.FolderName)
                    : CottonSyncRootManagementText.RootMissingStatus;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to stop Cotton mobile sync root.");
                Status = CottonSyncRootManagementText.StopFailedStatus;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SetRootPausedAsync(CottonSyncRootListItem item, bool isPaused)
        {
            ArgumentNullException.ThrowIfNull(item);

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = isPaused
                    ? CottonSyncRootManagementText.PauseFailedStatus
                    : CottonSyncRootManagementText.ResumeFailedStatus;
                return;
            }

            IsBusy = true;
            try
            {
                SyncRootCollectionSnapshot collection = await LoadRootCollectionAsync(instanceUri);
                CottonSyncRootSnapshot? root = collection.Roots.FirstOrDefault(root => root.Id == item.Id);
                if (root is null)
                {
                    ShowRoots(collection);
                    Status = CottonSyncRootManagementText.RootMissingStatus;
                    return;
                }

                await _rootManager.SetPausedAsync(instanceUri, root, isPaused);
                ShowRoots(await LoadRootCollectionAsync(instanceUri));
                Status = isPaused
                    ? CottonSyncRootManagementText.CreatePausedStatus(root.CloudFolder.FolderName)
                    : CottonSyncRootManagementText.CreateResumedStatus(root.CloudFolder.FolderName);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to update Cotton mobile sync root pause state.");
                Status = isPaused
                    ? CottonSyncRootManagementText.PauseFailedStatus
                    : CottonSyncRootManagementText.ResumeFailedStatus;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadRootsAsync(Uri instanceUri)
        {
            ShowRoots(await LoadRootCollectionAsync(instanceUri));
        }

        private Task<SyncRootCollectionSnapshot> LoadRootCollectionAsync(Uri instanceUri)
        {
            string accountScopeKey = _accountScopeKey
                ?? throw new InvalidOperationException("Sync account is not configured.");
            return _rootManager.LoadAsync(instanceUri, accountScopeKey);
        }

        private void ShowRoots(SyncRootCollectionSnapshot collection)
        {
            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create(
                collection.Roots,
                collection.PausedRootIds);
            Roots.ReplaceWith(state.Items);

            SummaryText = state.SummaryText;
            IsEmptyVisible = state.IsEmptyVisible;
            bool canRunAllChanged = _canRunAll != state.CanRunAny;
            _canRunAll = state.CanRunAny;
            if (canRunAllChanged)
            {
                OnPropertyChanged(nameof(IsRunAllVisible));
            }

            RunAllCommand.RaiseCanExecuteChanged();
        }

        private bool CanRunAll()
        {
            return !IsBusy && _canRunAll;
        }

        private bool CanAddRoot()
        {
            return !IsBusy && _instanceUri is not null && !string.IsNullOrWhiteSpace(_accountScopeKey);
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile sync settings command failure.");
            Status = "Could not update sync settings.";
        }
    }
}
