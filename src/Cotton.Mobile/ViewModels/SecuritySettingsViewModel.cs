using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class SecuritySettingsViewModel : ViewModelBase
    {
        private readonly ICottonAppLockSettingsStore _settingsStore;
        private readonly ICottonAppLockRuntimeStateStore _runtimeStateStore;
        private readonly ICottonAppLockCapabilityService _capabilityService;
        private readonly ICottonDeviceUnlockService _deviceUnlockService;
        private readonly ICottonWindowPrivacyService _windowPrivacyService;
        private readonly ILogger<SecuritySettingsViewModel> _logger;
        private CottonAppLockSettings _settings = CottonAppLockSettings.Disabled;
        private CottonAppLockCapabilitySnapshot _capability =
            CottonAppLockCapabilitySnapshot.Unavailable("App lock status has not been checked.");
        private CottonDeviceUnlockAvailabilitySnapshot _deviceUnlockAvailability =
            CottonDeviceUnlockAvailabilitySnapshot.Unavailable("Device unlock status has not been checked.");
        private bool _isBusy;
        private bool _isApplyingSourceValue;
        private bool _isAppLockEnabled;
        private string? _status;

        public SecuritySettingsViewModel(
            ICottonAppLockSettingsStore settingsStore,
            ICottonAppLockRuntimeStateStore runtimeStateStore,
            ICottonAppLockCapabilityService capabilityService,
            ICottonDeviceUnlockService deviceUnlockService,
            ICottonWindowPrivacyService windowPrivacyService,
            ILogger<SecuritySettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(settingsStore);
            ArgumentNullException.ThrowIfNull(runtimeStateStore);
            ArgumentNullException.ThrowIfNull(capabilityService);
            ArgumentNullException.ThrowIfNull(deviceUnlockService);
            ArgumentNullException.ThrowIfNull(windowPrivacyService);
            ArgumentNullException.ThrowIfNull(logger);

            _settingsStore = settingsStore;
            _runtimeStateStore = runtimeStateStore;
            _capabilityService = capabilityService;
            _deviceUnlockService = deviceUnlockService;
            _windowPrivacyService = windowPrivacyService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            VerifyDeviceUnlockCommand = new AsyncCommand(
                VerifyDeviceUnlockAsync,
                LogUnhandledCommandException,
                () => !IsBusy && DeviceUnlockDisplay.CanVerify);
            ShowAppLock(CottonAppLockSettingsDisplayState.Create(_settings, _capability));
            ShowDeviceUnlock(CottonDeviceUnlockDisplayState.Create(_deviceUnlockAvailability));
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand VerifyDeviceUnlockCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanToggleAppLock));
                    LoadCommand.RaiseCanExecuteChanged();
                    VerifyDeviceUnlockCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string AppLockTitle => AppLockDisplay.Title;

        public string AppLockStatusText => AppLockDisplay.StatusText;

        public string AppLockDetailText => AppLockDisplay.DetailText;

        public string DeviceUnlockTitle => DeviceUnlockDisplay.Title;

        public string DeviceUnlockStatusText => DeviceUnlockDisplay.StatusText;

        public string DeviceUnlockDetailText => DeviceUnlockDisplay.DetailText;

        public string DeviceUnlockActionText => DeviceUnlockDisplay.ActionText;

        public bool CanVerifyDeviceUnlock => !IsBusy && DeviceUnlockDisplay.CanVerify;

        public bool IsDeviceUnlockActionVisible => DeviceUnlockDisplay.IsActionVisible;

        public bool IsAppLockEnabled
        {
            get => _isAppLockEnabled;
            set
            {
                if (!SetProperty(ref _isAppLockEnabled, value) || _isApplyingSourceValue)
                {
                    return;
                }

                _ = SaveAppLockAsync(value);
            }
        }

        public bool CanToggleAppLock => !IsBusy && AppLockDisplay.CanToggle;

        public string SummaryText => AppLockDisplay.IsEnabled
            ? "App lock on"
            : "App lock off";

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

        private CottonAppLockSettingsDisplayState AppLockDisplay { get; set; } =
            CottonAppLockSettingsDisplayState.Create(
                CottonAppLockSettings.Disabled,
                CottonAppLockCapabilitySnapshot.Unavailable("App lock status has not been checked."));

        private CottonDeviceUnlockDisplayState DeviceUnlockDisplay { get; set; } =
            CottonDeviceUnlockDisplayState.Create(
                CottonDeviceUnlockAvailabilitySnapshot.Unavailable("Device unlock status has not been checked."));

        private async Task LoadAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                _settings = await _settingsStore.GetAsync();
                _capability = await _capabilityService.GetCapabilityAsync();
                _deviceUnlockAvailability = await _deviceUnlockService.GetAvailabilityAsync();
                bool disabledUnavailableAppLock = false;
                if (_settings.IsEnabled && !_capability.CanEnable)
                {
                    _settings = _settings.WithEnabled(false);
                    await _settingsStore.SaveAsync(_settings);
                    disabledUnavailableAppLock = true;
                }

                ShowAppLock(CottonAppLockSettingsDisplayState.Create(_settings, _capability));
                ShowDeviceUnlock(CottonDeviceUnlockDisplayState.Create(_deviceUnlockAvailability));
                Status = disabledUnavailableAppLock
                    ? "App lock was turned off because device unlock is unavailable."
                    : _capability.CanEnable ? null : "App lock is unavailable.";
                if (disabledUnavailableAppLock)
                {
                    await _windowPrivacyService.ApplyAsync();
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to inspect Cotton mobile security settings.");
                ShowAppLock(CottonAppLockSettingsDisplayState.Create(
                    CottonAppLockSettings.Disabled,
                    CottonAppLockCapabilitySnapshot.Unavailable("Could not inspect app lock.")));
                ShowDeviceUnlock(CottonDeviceUnlockDisplayState.Create(
                    CottonDeviceUnlockAvailabilitySnapshot.Unavailable("Could not inspect device unlock.")));
                Status = "Could not inspect security settings.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAppLockAsync(bool isEnabled)
        {
            if (!_capability.CanEnable)
            {
                ApplyAppLockSourceValue(false);
                Status = "App lock is unavailable.";
                return;
            }

            IsBusy = true;
            try
            {
                if (isEnabled)
                {
                    CottonDeviceUnlockResult unlockResult = await _deviceUnlockService.RequestUnlockAsync();
                    ShowDeviceUnlock(CottonDeviceUnlockDisplayState.Create(
                        _deviceUnlockAvailability,
                        unlockResult));
                    if (!unlockResult.IsSucceeded)
                    {
                        ApplyAppLockSourceValue(false);
                        Status = unlockResult.Result switch
                        {
                            CottonDeviceUnlockResultKind.Cancelled => "App lock was not enabled.",
                            CottonDeviceUnlockResultKind.Unavailable => "Device unlock is unavailable.",
                            _ => "Could not enable app lock."
                        };
                        return;
                    }

                    CottonAppLockRuntimeState runtimeState = await _runtimeStateStore.GetAsync();
                    await _runtimeStateStore.SaveAsync(runtimeState.WithUnlockedAt(DateTimeOffset.UtcNow));
                }

                _settings = _settings.WithEnabled(isEnabled);
                await _settingsStore.SaveAsync(_settings);
                await _windowPrivacyService.ApplyAsync();
                ShowAppLock(CottonAppLockSettingsDisplayState.Create(_settings, _capability));
                Status = isEnabled ? "App lock enabled." : "App lock disabled.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save Cotton mobile app lock settings.");
                ApplyAppLockSourceValue(AppLockDisplay.IsEnabled);
                Status = "Could not update app lock.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task VerifyDeviceUnlockAsync()
        {
            if (!_deviceUnlockAvailability.CanVerify)
            {
                ShowDeviceUnlock(CottonDeviceUnlockDisplayState.Create(_deviceUnlockAvailability));
                Status = "Device unlock is unavailable.";
                return;
            }

            IsBusy = true;
            try
            {
                CottonDeviceUnlockResult result = await _deviceUnlockService.RequestUnlockAsync();
                ShowDeviceUnlock(CottonDeviceUnlockDisplayState.Create(_deviceUnlockAvailability, result));
                Status = result.Result switch
                {
                    CottonDeviceUnlockResultKind.Succeeded => "Device unlock verified.",
                    CottonDeviceUnlockResultKind.Cancelled => "Device unlock was cancelled.",
                    CottonDeviceUnlockResultKind.Unavailable => "Device unlock is unavailable.",
                    _ => "Could not verify device unlock."
                };
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to verify Cotton mobile device unlock.");
                ShowDeviceUnlock(CottonDeviceUnlockDisplayState.Create(
                    _deviceUnlockAvailability,
                    CottonDeviceUnlockResult.Failed("Could not verify device unlock.")));
                Status = "Could not verify device unlock.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ShowAppLock(CottonAppLockSettingsDisplayState display)
        {
            ArgumentNullException.ThrowIfNull(display);

            AppLockDisplay = display;
            ApplyAppLockSourceValue(display.IsEnabled);
            OnPropertyChanged(nameof(AppLockTitle));
            OnPropertyChanged(nameof(AppLockStatusText));
            OnPropertyChanged(nameof(AppLockDetailText));
            OnPropertyChanged(nameof(CanToggleAppLock));
            OnPropertyChanged(nameof(SummaryText));
        }

        private void ShowDeviceUnlock(CottonDeviceUnlockDisplayState display)
        {
            ArgumentNullException.ThrowIfNull(display);

            DeviceUnlockDisplay = display;
            OnPropertyChanged(nameof(DeviceUnlockTitle));
            OnPropertyChanged(nameof(DeviceUnlockStatusText));
            OnPropertyChanged(nameof(DeviceUnlockDetailText));
            OnPropertyChanged(nameof(DeviceUnlockActionText));
            OnPropertyChanged(nameof(CanVerifyDeviceUnlock));
            OnPropertyChanged(nameof(IsDeviceUnlockActionVisible));
            VerifyDeviceUnlockCommand.RaiseCanExecuteChanged();
        }

        private void ApplyAppLockSourceValue(bool isEnabled)
        {
            _isApplyingSourceValue = true;
            try
            {
                IsAppLockEnabled = isEnabled;
            }
            finally
            {
                _isApplyingSourceValue = false;
            }
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile security settings command exception.");
        }
    }
}
