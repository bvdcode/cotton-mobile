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
        private readonly CottonDeviceToCloudSyncCoordinator _deviceToCloudSyncCoordinator;
        private readonly CottonBidirectionalSyncCoordinator _bidirectionalSyncCoordinator;
        private readonly INetworkAccessService _networkAccess;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<SyncSettingsViewModel> _logger;
        private Uri? _instanceUri;
        private bool _isBusy;
        private bool _canRunAll;
        private string _summaryText = "No folders syncing";
        private string? _status;
        private bool _isEmptyVisible = true;

        public SyncSettingsViewModel(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonSyncedFileManifestStore manifestStore,
            CottonCloudToDeviceSyncCoordinator syncCoordinator,
            CottonDeviceToCloudSyncCoordinator deviceToCloudSyncCoordinator,
            CottonBidirectionalSyncCoordinator bidirectionalSyncCoordinator,
            INetworkAccessService networkAccess,
            IUserDialogService dialogService,
            ILogger<SyncSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(manifestStore);
            ArgumentNullException.ThrowIfNull(syncCoordinator);
            ArgumentNullException.ThrowIfNull(deviceToCloudSyncCoordinator);
            ArgumentNullException.ThrowIfNull(bidirectionalSyncCoordinator);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _manifestStore = manifestStore;
            _syncCoordinator = syncCoordinator;
            _deviceToCloudSyncCoordinator = deviceToCloudSyncCoordinator;
            _bidirectionalSyncCoordinator = bidirectionalSyncCoordinator;
            _networkAccess = networkAccess;
            _dialogService = dialogService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            RunAllCommand = new AsyncCommand(RunAllAsync, LogUnhandledCommandException, CanRunAll);
            OpenFilesCommand = new AsyncCommand(OpenFilesAsync, LogUnhandledCommandException, () => !IsBusy);
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

        public AsyncCommand RunAllCommand { get; }

        public AsyncCommand OpenFilesCommand { get; }

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
                    RunAllCommand.RaiseCanExecuteChanged();
                    OpenFilesCommand.RaiseCanExecuteChanged();
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
                }
            }
        }

        public bool IsSummaryVisible => !IsEmptyVisible;

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
                Status = CottonSyncRootRunRouting.CreateOfflineUnavailableStatus(item.Direction);
                return;
            }

            CottonSyncDirection statusDirection = item.Direction;
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

                statusDirection = root.Direction;
                IReadOnlySet<Guid> pausedIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
                if (pausedIds.Contains(root.Id))
                {
                    ShowRoots(roots, pausedIds);
                    Status = CottonSyncRootManagementText.RootPausedStatus;
                    return;
                }

                Status = CottonSyncRootRunRouting.CreateStartingStatus(root);
                string completedStatus = await RunRootAndCreateCompletedStatusAsync(instanceUri, root);
                IReadOnlyList<CottonSyncRootSnapshot> refreshedRoots = await _rootStore.LoadAsync(instanceUri);
                IReadOnlySet<Guid> refreshedPausedIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
                ShowRoots(refreshedRoots, refreshedPausedIds);
                Status = completedStatus;
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
                IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(instanceUri);
                (CottonCloudToDeviceSyncRunSummary CloudToDeviceSummary,
                    CottonDeviceToCloudSyncRunSummary DeviceToCloudSummary,
                    CottonBidirectionalSyncRunSummary BidirectionalSummary) summaries =
                    await RunAllRootsAsync(instanceUri, roots);
                IReadOnlyList<CottonSyncRootSnapshot> refreshedRoots = await _rootStore.LoadAsync(instanceUri);
                IReadOnlySet<Guid> refreshedPausedIds = await _pauseStore.LoadPausedRootIdsAsync(instanceUri);
                ShowRoots(refreshedRoots, refreshedPausedIds);
                Status = CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                    summaries.CloudToDeviceSummary,
                    summaries.DeviceToCloudSummary,
                    summaries.BidirectionalSummary);
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

        private async Task OpenFilesAsync()
        {
            var navigation = Shell.Current.Navigation;
            if (navigation.NavigationStack.Count <= 1)
            {
                Status = "Open Files from the main screen.";
                return;
            }

            await navigation.PopAsync();
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

        private async Task<string> RunRootAndCreateCompletedStatusAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root)
        {
            switch (CottonSyncRootRunRouting.CreateRoute(root))
            {
                case CottonSyncRootRunRoute.CloudToDevice:
                    CottonCloudToDeviceSyncRunSummary cloudSummary =
                        await _syncCoordinator.RunRootAsync(instanceUri, root);
                    return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(cloudSummary);

                case CottonSyncRootRunRoute.DeviceToCloud:
                    CottonDeviceToCloudSyncRunSummary deviceSummary =
                        await RunDeviceToCloudRootAsync(instanceUri, root);
                    return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(deviceSummary);

                case CottonSyncRootRunRoute.Bidirectional:
                    CottonBidirectionalSyncRunSummary bidirectionalSummary =
                        await RunBidirectionalRootAsync(instanceUri, root);
                    return CottonSyncSettingsSingleRootRunStatusText.CreateFinishedStatus(bidirectionalSummary);

                default:
                    throw new ArgumentOutOfRangeException(nameof(root), "Sync direction is not supported.");
            }
        }

        private async Task<(
            CottonCloudToDeviceSyncRunSummary CloudToDeviceSummary,
            CottonDeviceToCloudSyncRunSummary DeviceToCloudSummary,
            CottonBidirectionalSyncRunSummary BidirectionalSummary)> RunAllRootsAsync(
            Uri instanceUri,
            IReadOnlyList<CottonSyncRootSnapshot> roots)
        {
            var cloudResults = new List<CottonCloudToDeviceSyncRootRunResult>();
            var deviceResults = new List<CottonDeviceToCloudSyncRootRunResult>();
            var bidirectionalResults = new List<CottonBidirectionalSyncRootRunResult>();
            foreach (CottonSyncRootSnapshot root in roots)
            {
                switch (CottonSyncRootRunRouting.CreateRoute(root))
                {
                    case CottonSyncRootRunRoute.CloudToDevice:
                        CottonCloudToDeviceSyncRunSummary summary =
                            await _syncCoordinator.RunRootAsync(instanceUri, root);
                        cloudResults.AddRange(summary.RootResults);
                        break;

                    case CottonSyncRootRunRoute.DeviceToCloud:
                        CottonDeviceToCloudSyncRunSummary deviceSummary =
                            await RunDeviceToCloudRootAsync(instanceUri, root);
                        deviceResults.AddRange(deviceSummary.RootResults);
                        break;

                    case CottonSyncRootRunRoute.Bidirectional:
                        CottonBidirectionalSyncRunSummary bidirectionalSummary =
                            await RunBidirectionalRootAsync(instanceUri, root);
                        bidirectionalResults.AddRange(bidirectionalSummary.RootResults);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(root),
                            "Sync run route is not supported.");
                }
            }

            return (
                new CottonCloudToDeviceSyncRunSummary(cloudResults),
                new CottonDeviceToCloudSyncRunSummary(deviceResults),
                new CottonBidirectionalSyncRunSummary(bidirectionalResults));
        }

        private async Task<CottonDeviceToCloudSyncRunSummary> RunDeviceToCloudRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root)
        {
            CottonDeviceToCloudSyncRunSummary summary =
                await _deviceToCloudSyncCoordinator.RunRootAsync(instanceUri, root);
            if (!summary.NeedsDestructiveReview)
            {
                return summary;
            }

            Status = CottonDeviceToCloudSyncStatusText.DestructiveReviewRequiredStatus;
            bool confirmed = await ConfirmDeviceToCloudRemoteDeletesAsync(
                summary.DestructiveReviewRemoteDeleteCount);
            if (!confirmed)
            {
                return summary;
            }

            return await _deviceToCloudSyncCoordinator.RunRootAsync(
                instanceUri,
                root,
                CottonDeviceToCloudSyncRunOptions.AllowRemoteDeletes);
        }

        private async Task<bool> ConfirmDeviceToCloudRemoteDeletesAsync(int fileCount)
        {
            return await _dialogService.ShowConfirmationAsync(
                CottonDeviceToCloudSyncStatusText.ConfirmRemoteDeleteTitle,
                CottonDeviceToCloudSyncStatusText.CreateConfirmRemoteDeleteMessage(fileCount),
                CottonDeviceToCloudSyncStatusText.ConfirmRemoteDeleteAction,
                CottonSyncRootManagementText.CancelAction);
        }

        private async Task<CottonBidirectionalSyncRunSummary> RunBidirectionalRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root)
        {
            CottonBidirectionalSyncRunSummary summary =
                await _bidirectionalSyncCoordinator.RunRootAsync(instanceUri, root);
            if (!summary.NeedsDestructiveReview)
            {
                return summary;
            }

            Status = CottonBidirectionalSyncStatusText.DestructiveReviewRequiredStatus;
            bool confirmed = await ConfirmBidirectionalDestructiveChangesAsync(
                summary.DestructiveReviewLocalDeleteCount,
                summary.DestructiveReviewRemoteDeleteCount);
            if (!confirmed)
            {
                return summary;
            }

            return await _bidirectionalSyncCoordinator.RunRootAsync(
                instanceUri,
                root,
                CottonBidirectionalSyncRunOptions.AllowDestructiveDeletes);
        }

        private async Task<bool> ConfirmBidirectionalDestructiveChangesAsync(
            int localDeleteCount,
            int remoteDeleteCount)
        {
            return await _dialogService.ShowConfirmationAsync(
                CottonBidirectionalSyncStatusText.ConfirmDestructiveTitle,
                CottonBidirectionalSyncStatusText.CreateConfirmDestructiveMessage(
                    localDeleteCount,
                    remoteDeleteCount),
                CottonBidirectionalSyncStatusText.ConfirmDestructiveAction,
                CottonSyncRootManagementText.CancelAction);
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
            _canRunAll = state.CanRunAny;
            RunAllCommand.RaiseCanExecuteChanged();
        }

        private bool CanRunAll()
        {
            return !IsBusy && _canRunAll;
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile sync settings command failure.");
            Status = "Could not update sync settings.";
        }
    }
}
