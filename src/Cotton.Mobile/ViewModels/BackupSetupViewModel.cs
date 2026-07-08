// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

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
        private readonly ICottonCameraBackupTransferEnqueueCoordinator _transferEnqueueCoordinator;
        private readonly ICottonTransferActivitySignal _transferActivitySignal;
        private readonly ICottonAndroidBackgroundTransferCoordinator _backgroundTransferCoordinator;
        private readonly IAndroidApiLevelProvider _androidApiLevelProvider;
        private readonly ICottonLocalNotificationService _localNotificationService;
        private readonly ICottonCameraBackupMediaAccessPolicy _mediaAccessPolicy;
        private readonly IUploadDestinationPickerPageService _destinationPickerPageService;
        private readonly ILogger<BackupSetupViewModel> _logger;
        private CottonCameraBackupSettings _settings = CottonCameraBackupSettings.Default;
        private bool _isBusy;
        private bool _photosOnly = true;
        private bool _wifiOnly = true;
        private bool _allowCellular;
        private bool _chargingOnly;
        private bool _isBackupEnabled;
        private bool _canEnableBackup;
        private bool _isDestinationStorageEstimateCurrent;
        private CottonCameraBackupMediaAccessDisplayState _latestMediaAccessDisplay =
            CottonCameraBackupMediaAccessDisplayState.Create(CottonCameraBackupMediaAccessState.NotRequested);
        private CottonCameraBackupDestinationStorageEstimate _latestDestinationStorageEstimate =
            CottonCameraBackupDestinationStorageEstimate.Empty;
        private string _destinationText = "No folder selected";
        private string _executionStatusText = "Choose a folder before camera backup can run.";
        private string _policySummaryText = "Photos only, Wi-Fi only, any battery state.";
        private string _destinationStorageEstimateTitle = "Destination estimate";
        private string _destinationStorageEstimateText = "Choose a folder to estimate backup storage.";
        private string _healthTitle = "Backup Health";
        private string _healthStatusText = "Backup health will appear after background backup is available.";
        private string _healthCountsText = "Pending 0 · Uploaded 0 · Failed 0 · Blocked 0";
        private string _localMediaRetentionText =
            CottonCameraBackupLocalMediaRetentionPolicy.Mvp.SetupSummaryText;
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
            ICottonCameraBackupTransferEnqueueCoordinator transferEnqueueCoordinator,
            ICottonTransferActivitySignal transferActivitySignal,
            ICottonAndroidBackgroundTransferCoordinator backgroundTransferCoordinator,
            IAndroidApiLevelProvider androidApiLevelProvider,
            ICottonLocalNotificationService localNotificationService,
            ICottonCameraBackupMediaAccessPolicy mediaAccessPolicy,
            IUploadDestinationPickerPageService destinationPickerPageService,
            ILogger<BackupSetupViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(settingsStore);
            ArgumentNullException.ThrowIfNull(planningService);
            ArgumentNullException.ThrowIfNull(transferEnqueueCoordinator);
            ArgumentNullException.ThrowIfNull(transferActivitySignal);
            ArgumentNullException.ThrowIfNull(backgroundTransferCoordinator);
            ArgumentNullException.ThrowIfNull(androidApiLevelProvider);
            ArgumentNullException.ThrowIfNull(localNotificationService);
            ArgumentNullException.ThrowIfNull(mediaAccessPolicy);
            ArgumentNullException.ThrowIfNull(destinationPickerPageService);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _settingsStore = settingsStore;
            _planningService = planningService;
            _transferEnqueueCoordinator = transferEnqueueCoordinator;
            _transferActivitySignal = transferActivitySignal;
            _backgroundTransferCoordinator = backgroundTransferCoordinator;
            _androidApiLevelProvider = androidApiLevelProvider;
            _localNotificationService = localNotificationService;
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
            QueueNowCommand = new AsyncCommand(QueueNowAsync, LogUnhandledCommandException, () => !IsBusy);
            SaveCommand = new AsyncCommand(SaveAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand ChooseDestinationCommand { get; }

        public AsyncCommand MediaAccessActionCommand { get; }

        public AsyncCommand QueueNowCommand { get; }

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

        public bool IsBackupEnabled
        {
            get => _isBackupEnabled;
            set
            {
                if (SetProperty(ref _isBackupEnabled, value && CanEnableBackup))
                {
                    RefreshPolicyPreview();
                }
            }
        }

        public bool CanEnableBackup
        {
            get => _canEnableBackup;
            private set
            {
                if (SetProperty(ref _canEnableBackup, value))
                {
                    if (!value && IsBackupEnabled)
                    {
                        IsBackupEnabled = false;
                    }
                }
            }
        }

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

        public string DestinationStorageEstimateTitle
        {
            get => _destinationStorageEstimateTitle;
            private set => SetProperty(ref _destinationStorageEstimateTitle, value);
        }

        public string DestinationStorageEstimateText
        {
            get => _destinationStorageEstimateText;
            private set => SetProperty(ref _destinationStorageEstimateText, value);
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

        public string LocalMediaRetentionText
        {
            get => _localMediaRetentionText;
            private set => SetProperty(ref _localMediaRetentionText, value);
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
                    CottonCameraBackupMediaAccessDisplayState mediaAccess = await LoadMediaAccessAsync();
                    await LoadPlanningHealthAsync(mediaAccess);
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
                    CottonCameraBackupMediaAccessDisplayState mediaAccess = await LoadMediaAccessAsync();
                    await LoadPlanningHealthAsync(mediaAccess);
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
                    CottonCameraBackupMediaAccessDisplayState mediaAccess = await LoadMediaAccessAsync();
                    await LoadPlanningHealthAsync(mediaAccess);
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
                        CottonCameraBackupMediaAccessDisplayState mediaAccess = await LoadMediaAccessAsync();
                        await LoadPlanningHealthAsync(mediaAccess);
                        Status = "Android settings opened.";
                        return;
                    }

                    CottonCameraBackupMediaAccessState state =
                        await _mediaAccessPolicy.RequestAccessAsync();
                    CottonCameraBackupMediaAccessDisplayState display = ShowMediaAccess(state);
                    await LoadPlanningHealthAsync(display);
                    Status = display.CanScanFullLibrary
                        ? "Media access updated."
                        : "Automatic camera backup needs full media access.";
                },
                "Could not update media access.");
        }

        public async Task QueueNowAsync()
        {
            await RunBackupActionAsync(
                async () =>
                {
                    _settings = CreateSettingsFromUi();
                    if (!_settings.HasDestination)
                    {
                        ShowSettings(_settings);
                        Status = "Choose a folder before queueing camera backup.";
                        await ShowBackupBlockedNotificationAsync(Status);
                        return;
                    }

                    await _settingsStore.SaveAsync(_instanceUri, _settings);
                    CottonCameraBackupMediaAccessState accessState =
                        await _mediaAccessPolicy.GetAccessStateAsync();
                    CottonCameraBackupMediaAccessDisplayState mediaDisplay = ShowMediaAccess(accessState);
                    if (!mediaDisplay.CanScanFullLibrary)
                    {
                        Status = CottonCameraBackupQueueStatusText.CreateBlockedAccessStatus(mediaDisplay);
                        await ShowBackupBlockedNotificationAsync(Status);
                        await LoadPlanningHealthAsync(mediaDisplay);
                        return;
                    }

                    CottonCameraBackupTransferEnqueueResult result =
                        await _transferEnqueueCoordinator.EnqueueAsync(_instanceUri, _settings);
                    if (result.QueuedCount > 0 || result.SkippedExistingTransferCount > 0)
                    {
                        _transferActivitySignal.NotifyTransferActivityChanged();
                    }

                    CottonAndroidBackgroundTransferScheduleResult? scheduleResult =
                        await ScheduleQueuedCameraBackupBestEffortAsync(result);
                    await LoadPlanningHealthAsync(mediaDisplay);
                    Status = CreateQueueStatusText(result, scheduleResult);
                },
                "Could not queue camera backup uploads.");
        }

        private async Task<CottonAndroidBackgroundTransferScheduleResult?> ScheduleQueuedCameraBackupBestEffortAsync(
            CottonCameraBackupTransferEnqueueResult result)
        {
            if (result.QueuedCount <= 0 && result.SkippedExistingTransferCount <= 0)
            {
                return null;
            }

            try
            {
                return await _backgroundTransferCoordinator
                    .ScheduleNextQueuedCameraBackupUploadAsync(
                        _instanceUri,
                        _androidApiLevelProvider.CurrentApiLevel)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile camera backup background schedule failed.");
                return null;
            }
        }

        private static string CreateQueueStatusText(
            CottonCameraBackupTransferEnqueueResult enqueueResult,
            CottonAndroidBackgroundTransferScheduleResult? scheduleResult)
        {
            string queueStatus = CottonCameraBackupQueueStatusText.CreateResultStatus(enqueueResult);
            if (scheduleResult is null
                || scheduleResult.Status == CottonAndroidBackgroundTransferScheduleStatus.NoQueuedTransfer)
            {
                return queueStatus;
            }

            if (scheduleResult.IsScheduled)
            {
                return $"{queueStatus} Android will run backup when constraints allow.";
            }

            if (scheduleResult.Status == CottonAndroidBackgroundTransferScheduleStatus.ForegroundRequired)
            {
                return $"{queueStatus} Open Transfers to run waiting uploads.";
            }

            if (scheduleResult.Status == CottonAndroidBackgroundTransferScheduleStatus.Unsupported)
            {
                return $"{queueStatus} Background backup is not available on this device yet.";
            }

            return queueStatus;
        }

        private CottonCameraBackupSettings CreateSettingsFromUi()
        {
            return _settings
                .WithEnabled(IsBackupEnabled && CanEnableBackup)
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
            CanEnableBackup = display.CanEnableBackup;
            IsBackupEnabled = settings.IsEnabled && display.CanEnableBackup;
            DestinationText = display.DestinationText;
            ExecutionStatusText = display.ExecutionStatusText;
            PolicySummaryText = display.PolicySummaryText;
            LocalMediaRetentionText = display.LocalMediaRetentionText;
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
            _isDestinationStorageEstimateCurrent = false;
            ShowDestinationStorageEstimate(
                settings,
                _latestMediaAccessDisplay,
                _latestDestinationStorageEstimate);
        }

        private async Task LoadPlanningHealthAsync(CottonCameraBackupMediaAccessDisplayState mediaAccess)
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
            _latestDestinationStorageEstimate = plan.DestinationStorageEstimate;
            _isDestinationStorageEstimateCurrent = true;
            ShowDestinationStorageEstimate(
                _settings,
                mediaAccess,
                plan.DestinationStorageEstimate);
        }

        private async Task<CottonCameraBackupMediaAccessDisplayState> LoadMediaAccessAsync()
        {
            CottonCameraBackupMediaAccessState state =
                await _mediaAccessPolicy.GetAccessStateAsync();
            return ShowMediaAccess(state);
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
            _latestMediaAccessDisplay = display;
            return display;
        }

        private void ShowDestinationStorageEstimate(
            CottonCameraBackupSettings settings,
            CottonCameraBackupMediaAccessDisplayState mediaAccess,
            CottonCameraBackupDestinationStorageEstimate estimate)
        {
            CottonCameraBackupDestinationStorageEstimateDisplayState display =
                CottonCameraBackupDestinationStorageEstimateDisplayState.Create(
                    settings,
                    mediaAccess,
                    estimate,
                    _isDestinationStorageEstimateCurrent);
            DestinationStorageEstimateTitle = display.Title;
            DestinationStorageEstimateText = display.SummaryText;
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
            QueueNowCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile camera backup setup command exception.");
        }

        private async Task ShowBackupBlockedNotificationAsync(string? reason)
        {
            try
            {
                CottonLocalNotificationSnapshot notification =
                    CottonTransferNotificationFactory.CreateBackupBlocked(reason ?? string.Empty);
                await _localNotificationService.ShowAsync(notification);
            }
            catch
            {
                // Notification delivery must never hide the in-app blocked status.
            }
        }
    }
}
