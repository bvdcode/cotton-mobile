using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsViewModel : ViewModelBase
    {
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ICottonSyncRootPauseStore _pauseStore;
        private readonly ICottonSyncedFileManifestStore _manifestStore;
        private readonly CottonCloudToDeviceSyncCoordinator _syncCoordinator;
        private readonly INetworkAccessService _networkAccess;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<SyncSettingsViewModel> _logger;
        private Uri? _instanceUri;
        private bool _isBusy;
        private string _summaryText = "No folders syncing";
        private string? _status;
        private bool _isEmptyVisible = true;

        public SyncSettingsViewModel(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonSyncedFileManifestStore manifestStore,
            CottonCloudToDeviceSyncCoordinator syncCoordinator,
            INetworkAccessService networkAccess,
            IUserDialogService dialogService,
            ILogger<SyncSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(manifestStore);
            ArgumentNullException.ThrowIfNull(syncCoordinator);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _manifestStore = manifestStore;
            _syncCoordinator = syncCoordinator;
            _networkAccess = networkAccess;
            _dialogService = dialogService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
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

        public AsyncCommand<CottonSyncRootListItem> RunRootCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> StopRootCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> PauseRootCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> ResumeRootCommand { get; }

        public ObservableCollection<CottonSyncRootListItem> Roots { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoadCommand.RaiseCanExecuteChanged();
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
            private set => SetProperty(ref _isEmptyVisible, value);
        }

        public void Configure(Uri instanceUri)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            _instanceUri = instanceUri;
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
                Status = CottonCloudToDeviceSyncStatusText.OfflineUnavailableStatus;
                return;
            }

            IsBusy = true;
            try
            {
                IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(instanceUri);
                CottonSyncRootSnapshot? root = roots.FirstOrDefault(root => root.Id == item.Id);
                if (root is null)
                {
                    IReadOnlySet<Guid> pausedRootIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
                    ShowRoots(roots, pausedRootIds);
                    Status = "Sync folder is no longer configured.";
                    return;
                }

                IReadOnlySet<Guid> pausedIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
                if (pausedIds.Contains(root.Id))
                {
                    ShowRoots(roots, pausedIds);
                    Status = CottonSyncRootManagementText.RootPausedStatus;
                    return;
                }

                Status = CottonCloudToDeviceSyncStatusText.CreateStartingStatus(root.CloudFolder.FolderName);
                CottonCloudToDeviceSyncRunSummary summary = await _syncCoordinator.RunRootAsync(instanceUri, root);
                IReadOnlyList<CottonSyncRootSnapshot> refreshedRoots = await _rootStore.LoadAsync(instanceUri);
                IReadOnlySet<Guid> refreshedPausedIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
                ShowRoots(refreshedRoots, refreshedPausedIds);
                Status = CottonCloudToDeviceSyncStatusText.CreateCompletedStatus(summary);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to run Cotton mobile sync root.");
                Status = CottonCloudToDeviceSyncStatusText.FailedStatus;
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
                IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(instanceUri);
                CottonSyncRootSnapshot? root = roots.FirstOrDefault(root => root.Id == item.Id);
                if (root is null)
                {
                    IReadOnlySet<Guid> pausedRootIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
                    ShowRoots(roots, pausedRootIds);
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

                bool removed = await _rootStore.RemoveAsync(instanceUri, root.Id);
                await _pauseStore.SetPausedAsync(instanceUri, root.Id, isPaused: false);
                await _manifestStore.ClearAsync(instanceUri, root);
                IReadOnlyList<CottonSyncRootSnapshot> refreshedRoots = await _rootStore.LoadAsync(instanceUri);
                IReadOnlySet<Guid> refreshedPausedIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
                ShowRoots(refreshedRoots, refreshedPausedIds);
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
                IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(instanceUri);
                CottonSyncRootSnapshot? root = roots.FirstOrDefault(root => root.Id == item.Id);
                IReadOnlySet<Guid> pausedRootIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
                if (root is null)
                {
                    ShowRoots(roots, pausedRootIds);
                    Status = CottonSyncRootManagementText.RootMissingStatus;
                    return;
                }

                await _pauseStore.SetPausedAsync(instanceUri, root.Id, isPaused);
                IReadOnlySet<Guid> refreshedPausedIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
                ShowRoots(roots, refreshedPausedIds);
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
            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(instanceUri);
            IReadOnlySet<Guid> pausedRootIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
            ShowRoots(roots, pausedRootIds);
        }

        private void ShowRoots(IReadOnlyList<CottonSyncRootSnapshot> roots, IReadOnlySet<Guid> pausedRootIds)
        {
            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create(roots, pausedRootIds);
            Roots.Clear();
            foreach (CottonSyncRootListItem item in state.Items)
            {
                Roots.Add(item);
            }

            SummaryText = state.SummaryText;
            IsEmptyVisible = state.IsEmptyVisible;
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile sync settings command failure.");
            Status = "Could not update sync settings.";
        }
    }
}
