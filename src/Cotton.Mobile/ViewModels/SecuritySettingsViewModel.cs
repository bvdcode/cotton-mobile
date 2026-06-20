using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class SecuritySettingsViewModel : ViewModelBase
    {
        private readonly ICottonAppLockSettingsStore _settingsStore;
        private readonly ICottonAppLockCapabilityService _capabilityService;
        private readonly ILogger<SecuritySettingsViewModel> _logger;
        private CottonAppLockSettings _settings = CottonAppLockSettings.Disabled;
        private CottonAppLockCapabilitySnapshot _capability =
            CottonAppLockCapabilitySnapshot.Unavailable("App lock status has not been checked.");
        private bool _isBusy;
        private bool _isApplyingSourceValue;
        private bool _isAppLockEnabled;
        private string? _status;

        public SecuritySettingsViewModel(
            ICottonAppLockSettingsStore settingsStore,
            ICottonAppLockCapabilityService capabilityService,
            ILogger<SecuritySettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(settingsStore);
            ArgumentNullException.ThrowIfNull(capabilityService);
            ArgumentNullException.ThrowIfNull(logger);

            _settingsStore = settingsStore;
            _capabilityService = capabilityService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            ShowAppLock(CottonAppLockSettingsDisplayState.Create(_settings, _capability));
        }

        public AsyncCommand LoadCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanToggleAppLock));
                    LoadCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string AppLockTitle => AppLockDisplay.Title;

        public string AppLockStatusText => AppLockDisplay.StatusText;

        public string AppLockDetailText => AppLockDisplay.DetailText;

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
                ShowAppLock(CottonAppLockSettingsDisplayState.Create(_settings, _capability));
                Status = _capability.CanEnable ? null : "App lock is unavailable.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to inspect Cotton mobile security settings.");
                ShowAppLock(CottonAppLockSettingsDisplayState.Create(
                    CottonAppLockSettings.Disabled,
                    CottonAppLockCapabilitySnapshot.Unavailable("Could not inspect app lock.")));
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
                _settings = _settings.WithEnabled(isEnabled);
                await _settingsStore.SaveAsync(_settings);
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
