using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsViewModel : ViewModelBase
    {
        private readonly ICottonSyncRootStore _rootStore;
        private readonly CottonCloudToDeviceSyncCoordinator _syncCoordinator;
        private readonly INetworkAccessService _networkAccess;
        private readonly ILogger<SyncSettingsViewModel> _logger;
        private Uri? _instanceUri;
        private bool _isBusy;
        private string _summaryText = "No folders syncing";
        private string? _status;
        private bool _isEmptyVisible = true;

        public SyncSettingsViewModel(
            ICottonSyncRootStore rootStore,
            CottonCloudToDeviceSyncCoordinator syncCoordinator,
            INetworkAccessService networkAccess,
            ILogger<SyncSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(syncCoordinator);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(logger);

            _rootStore = rootStore;
            _syncCoordinator = syncCoordinator;
            _networkAccess = networkAccess;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            RunRootCommand = new AsyncCommand<CottonSyncRootListItem>(
                RunRootAsync,
                LogUnhandledCommandException,
                item => !IsBusy && item.CanRunNow);
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> RunRootCommand { get; }

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
                    ShowRoots(roots);
                    Status = "Sync folder is no longer configured.";
                    return;
                }

                Status = CottonCloudToDeviceSyncStatusText.CreateStartingStatus(root.CloudFolder.FolderName);
                CottonCloudToDeviceSyncRunSummary summary = await _syncCoordinator.RunRootAsync(instanceUri, root);
                IReadOnlyList<CottonSyncRootSnapshot> refreshedRoots = await _rootStore.LoadAsync(instanceUri);
                ShowRoots(refreshedRoots);
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

        private async Task LoadRootsAsync(Uri instanceUri)
        {
            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(instanceUri);
            ShowRoots(roots);
        }

        private void ShowRoots(IReadOnlyList<CottonSyncRootSnapshot> roots)
        {
            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create(roots);
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
