using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class BackupSetupViewModel : ViewModelBase
    {
        private readonly Uri _instanceUri;
        private readonly ICottonCameraBackupSettingsStore _settingsStore;
        private readonly ICottonCameraBackupPlanningService _planningService;
        private readonly ICottonCameraBackupMediaAccessPolicy _mediaAccessPolicy;
        private readonly IUploadDestinationPickerPageService _destinationPickerPageService;
        private readonly ILogger<BackupSetupViewModel> _logger;
        private CottonCameraBackupSettings _settings = CottonCameraBackupSettings.Default;
        private bool _isBusy;
        private bool _photosOnly = true;
        private bool _wifiOnly = true;
        private bool _allowCellular;
        private bool _chargingOnly;
        private string _destinationText = "No folder selected";
        private string _executionStatusText = "Choose a folder before camera backup can run.";
        private string _policySummaryText = "Photos only, Wi-Fi only, any battery state.";
        private string _healthTitle = "Backup Health";
        private string _healthStatusText = "Backup health will appear after background backup is available.";
        private string _healthCountsText = "Pending 0 · Uploaded 0 · Failed 0 · Blocked 0";
        private string _mediaAccessTitle = "Media Access";
        private string _mediaAccessStatusText = "Not requested";
        private string _mediaAccessDetailText = "Cotton will ask before scanning photos or videos.";
        private string _mediaAccessActionText = "Allow";
        private bool _isMediaAccessActionVisible = true;
        private bool _mediaAccessShouldOpenSettings;
        private bool _reloadOnNextAppearing;
        private string? _status;

        public BackupSetupViewModel(
            Uri instanceUri,
            ICottonCameraBackupSettingsStore settingsStore,
            ICottonCameraBackupPlanningService planningService,
            ICottonCameraBackupMediaAccessPolicy mediaAccessPolicy,
            IUploadDestinationPickerPageService destinationPickerPageService,
            ILogger<BackupSetupViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(settingsStore);
            ArgumentNullException.ThrowIfNull(planningService);
            ArgumentNullException.ThrowIfNull(mediaAccessPolicy);
            ArgumentNullException.ThrowIfNull(destinationPickerPageService);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _settingsStore = settingsStore;
            _planningService = planningService;
            _mediaAccessPolicy = mediaAccessPolicy;
            _destinationPickerPageService = destinationPickerPageService;
            _logger = logger;

            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            ChooseDestinationCommand = new AsyncCommand(
                ChooseDestinationAsync,
                LogUnhandledCommandException,
                () => !IsBusy);
            MediaAccessActionCommand = new AsyncCommand(
                RunMediaAccessActionAsync,
                LogUnhandledCommandException,
                () => !IsBusy && IsMediaAccessActionVisible);
            SaveCommand = new AsyncCommand(SaveAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand ChooseDestinationCommand { get; }

        public AsyncCommand MediaAccessActionCommand { get; }

        public AsyncCommand SaveCommand { get; }

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

        public bool IsBackupEnabled => false;

        public bool CanEnableBackup => false;

        public bool PhotosOnly
        {
            get => _photosOnly;
            set
            {
                if (SetProperty(ref _photosOnly, value))
                {
                    RefreshPolicyPreview();
                }
            }
        }

        public bool WifiOnly
        {
            get => _wifiOnly;
            set
            {
                if (SetProperty(ref _wifiOnly, value))
                {
                    if (value && _allowCellular)
                    {
                        _allowCellular = false;
                        OnPropertyChanged(nameof(AllowCellular));
                    }

                    RefreshPolicyPreview();
                }
            }
        }

        public bool AllowCellular
        {
            get => _allowCellular;
            set
            {
                if (SetProperty(ref _allowCellular, value))
                {
                    if (value && _wifiOnly)
                    {
                        _wifiOnly = false;
                        OnPropertyChanged(nameof(WifiOnly));
                    }

                    RefreshPolicyPreview();
                }
            }
        }

        public bool ChargingOnly
        {
            get => _chargingOnly;
            set
            {
                if (SetProperty(ref _chargingOnly, value))
                {
                    RefreshPolicyPreview();
                }
            }
        }

        public string DestinationText
        {
            get => _destinationText;
            private set => SetProperty(ref _destinationText, value);
        }

        public string ExecutionStatusText
        {
            get => _executionStatusText;
            private set => SetProperty(ref _executionStatusText, value);
        }

        public string PolicySummaryText
        {
            get => _policySummaryText;
            private set => SetProperty(ref _policySummaryText, value);
        }

        public string HealthTitle
        {
            get => _healthTitle;
            private set => SetProperty(ref _healthTitle, value);
        }

        public string HealthStatusText
        {
            get => _healthStatusText;
            private set => SetProperty(ref _healthStatusText, value);
        }

        public string HealthCountsText
        {
            get => _healthCountsText;
            private set => SetProperty(ref _healthCountsText, value);
        }

        public string MediaAccessTitle
        {
            get => _mediaAccessTitle;
            private set => SetProperty(ref _mediaAccessTitle, value);
        }

        public string MediaAccessStatusText
        {
            get => _mediaAccessStatusText;
            private set => SetProperty(ref _mediaAccessStatusText, value);
        }

        public string MediaAccessDetailText
        {
            get => _mediaAccessDetailText;
            private set => SetProperty(ref _mediaAccessDetailText, value);
        }

        public string MediaAccessActionText
        {
            get => _mediaAccessActionText;
            private set => SetProperty(ref _mediaAccessActionText, value);
        }

        public bool IsMediaAccessActionVisible
        {
            get => _isMediaAccessActionVisible;
            private set
            {
                if (SetProperty(ref _isMediaAccessActionVisible, value))
                {
                    MediaAccessActionCommand.RaiseCanExecuteChanged();
                }
            }
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

        public bool ConsumeReloadOnAppearing()
        {
            if (!_reloadOnNextAppearing)
            {
                return false;
            }

            _reloadOnNextAppearing = false;
            return true;
        }

        private async Task LoadAsync()
        {
            await RunBackupActionAsync(
                async () =>
                {
                    _settings = await _settingsStore.GetAsync(_instanceUri);
                    ShowSettings(_settings);
                    await LoadPlanningHealthAsync();
                    await LoadMediaAccessAsync();
                    Status = null;
                },
                "Could not load camera backup setup.");
        }

        private async Task ChooseDestinationAsync()
        {
            await RunBackupActionAsync(
                async () =>
                {
                    CottonUploadDestinationSnapshot? destination =
                        await _destinationPickerPageService.PickAsync(_instanceUri);
                    if (destination is null)
                    {
                        return;
                    }

                    _settings = CreateSettingsFromUi().WithDestination(destination);
                    await _settingsStore.SaveAsync(_instanceUri, _settings);
                    ShowSettings(_settings);
                    await LoadPlanningHealthAsync();
                    await LoadMediaAccessAsync();
                    Status = "Camera backup setup saved.";
                },
                "Could not choose backup folder.");
        }

        private async Task SaveAsync()
        {
            await RunBackupActionAsync(
                async () =>
                {
                    _settings = CreateSettingsFromUi();
                    await _settingsStore.SaveAsync(_instanceUri, _settings);
                    ShowSettings(_settings);
                    await LoadPlanningHealthAsync();
                    await LoadMediaAccessAsync();
                    Status = "Camera backup setup saved.";
                },
                "Could not save camera backup setup.");
        }

        private async Task RunMediaAccessActionAsync()
        {
            await RunBackupActionAsync(
                async () =>
                {
                    if (_mediaAccessShouldOpenSettings)
                    {
                        _reloadOnNextAppearing = true;
                        await _mediaAccessPolicy.OpenSettingsAsync();
                        await LoadMediaAccessAsync();
                        Status = "Android settings opened.";
                        return;
                    }

                    CottonCameraBackupMediaAccessState state =
                        await _mediaAccessPolicy.RequestAccessAsync();
                    CottonCameraBackupMediaAccessDisplayState display = ShowMediaAccess(state);
                    Status = display.CanScanFullLibrary
                        ? "Media access updated."
                        : "Automatic camera backup needs full media access.";
                },
                "Could not update media access.");
        }

        private CottonCameraBackupSettings CreateSettingsFromUi()
        {
            return _settings
                .WithPhotosOnly(PhotosOnly)
                .WithWifiOnly(WifiOnly)
                .WithAllowCellular(AllowCellular)
                .WithChargingOnly(ChargingOnly);
        }

        private void ShowSettings(CottonCameraBackupSettings settings)
        {
            _settings = settings;
            PhotosOnly = settings.PhotosOnly;
            WifiOnly = settings.WifiOnly;
            AllowCellular = settings.AllowCellular;
            ChargingOnly = settings.ChargingOnly;
            CottonCameraBackupSetupDisplayState display =
                CottonCameraBackupSetupDisplayState.Create(settings);
            DestinationText = display.DestinationText;
            ExecutionStatusText = display.ExecutionStatusText;
            PolicySummaryText = display.PolicySummaryText;
            ShowHealth(settings);
        }

        private void RefreshPolicyPreview()
        {
            CottonCameraBackupSettings preview = _settings
                .WithPhotosOnly(PhotosOnly)
                .WithWifiOnly(WifiOnly)
                .WithAllowCellular(AllowCellular)
                .WithChargingOnly(ChargingOnly);
            PolicySummaryText = CottonCameraBackupSetupDisplayState.Create(preview).PolicySummaryText;
            ShowHealth(preview);
        }

        private void ShowHealth(CottonCameraBackupSettings settings)
        {
            CottonCameraBackupHealthDisplayState health =
                CottonCameraBackupHealthDisplayState.Create(settings, CottonCameraBackupHealthSnapshot.Empty);
            HealthTitle = health.Title;
            HealthStatusText = health.StatusText;
            HealthCountsText = health.CountsText;
        }

        private async Task LoadPlanningHealthAsync()
        {
            CottonCameraBackupPlanSnapshot plan =
                await _planningService.PlanAsync(_instanceUri, _settings);
            CottonCameraBackupHealthDisplayState health =
                CottonCameraBackupHealthDisplayState.Create(
                    _settings,
                    plan.Health);
            HealthTitle = health.Title;
            HealthStatusText = health.StatusText;
            HealthCountsText = health.CountsText;
        }

        private async Task LoadMediaAccessAsync()
        {
            CottonCameraBackupMediaAccessState state =
                await _mediaAccessPolicy.GetAccessStateAsync();
            _ = ShowMediaAccess(state);
        }

        private CottonCameraBackupMediaAccessDisplayState ShowMediaAccess(CottonCameraBackupMediaAccessState state)
        {
            CottonCameraBackupMediaAccessDisplayState display =
                CottonCameraBackupMediaAccessDisplayState.Create(state);
            MediaAccessTitle = display.Title;
            MediaAccessStatusText = display.StatusText;
            MediaAccessDetailText = display.DetailText;
            MediaAccessActionText = display.ActionText;
            IsMediaAccessActionVisible = display.IsActionVisible;
            _mediaAccessShouldOpenSettings = display.ShouldOpenSettings;
            return display;
        }

        private async Task RunBackupActionAsync(Func<Task> actionAsync, string failureStatus)
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
                _logger.LogWarning(exception, "Cotton mobile camera backup setup action failed.");
                Status = failureStatus;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RaiseCommandStatesChanged()
        {
            LoadCommand.RaiseCanExecuteChanged();
            ChooseDestinationCommand.RaiseCanExecuteChanged();
            MediaAccessActionCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile camera backup setup command exception.");
        }
    }
}
