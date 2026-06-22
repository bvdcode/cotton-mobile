// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class SecuritySettingsViewModel : ViewModelBase
    {
        private const string RevokeCurrentSessionConfirmationTitle = "Revoke current session?";
        private const string RevokeCurrentSessionClearCacheMessage =
            "This device will be signed out and cached files on this device will be removed.";
        private const string RevokeCurrentSessionKeepCacheMessage =
            "This device will be signed out. Cached files on this device will stay here.";
        private const string RevokeCurrentSessionAction = "Revoke";
        private const string CancelAction = "Cancel";

        private readonly ICottonAppLockSettingsStore _settingsStore;
        private readonly ICottonAppLockRuntimeStateStore _runtimeStateStore;
        private readonly ICottonInstanceStore _instanceStore;
        private readonly ICottonLogoutCacheCleanupSettingsStore _logoutCleanupSettingsStore;
        private readonly ICottonAccountSessionService _accountSessionService;
        private readonly ICottonAppLockCapabilityService _capabilityService;
        private readonly ICottonDeviceUnlockService _deviceUnlockService;
        private readonly ICottonWindowPrivacyService _windowPrivacyService;
        private readonly ICottonNotificationPermissionService _notificationPermissionService;
        private readonly ICottonCameraBackupMediaAccessPolicy _mediaAccessPolicy;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<SecuritySettingsViewModel> _logger;
        private ICottonCurrentSessionRevocationHandler? _currentSessionRevocationHandler;
        private CottonAppLockSettings _settings = CottonAppLockSettings.Disabled;
        private CottonAppLockCapabilitySnapshot _capability =
            CottonAppLockCapabilitySnapshot.Unavailable("App lock status has not been checked.");
        private CottonDeviceUnlockAvailabilitySnapshot _deviceUnlockAvailability =
            CottonDeviceUnlockAvailabilitySnapshot.Unavailable("Device unlock status has not been checked.");
        private CottonLogoutCacheCleanupSettings _logoutCleanupSettings =
            CottonLogoutCacheCleanupSettings.Default;
        private bool _isBusy;
        private bool _isApplyingSourceValue;
        private bool _isApplyingLogoutCleanupSourceValue;
        private bool _isAppLockEnabled;
        private bool _isLogoutCacheCleanupEnabled;
        private string? _status;

        public SecuritySettingsViewModel(
            ICottonAppLockSettingsStore settingsStore,
            ICottonAppLockRuntimeStateStore runtimeStateStore,
            ICottonInstanceStore instanceStore,
            ICottonLogoutCacheCleanupSettingsStore logoutCleanupSettingsStore,
            ICottonAccountSessionService accountSessionService,
            ICottonAppLockCapabilityService capabilityService,
            ICottonDeviceUnlockService deviceUnlockService,
            ICottonWindowPrivacyService windowPrivacyService,
            ICottonNotificationPermissionService notificationPermissionService,
            ICottonCameraBackupMediaAccessPolicy mediaAccessPolicy,
            IUserDialogService dialogService,
            ILogger<SecuritySettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(settingsStore);
            ArgumentNullException.ThrowIfNull(runtimeStateStore);
            ArgumentNullException.ThrowIfNull(instanceStore);
            ArgumentNullException.ThrowIfNull(logoutCleanupSettingsStore);
            ArgumentNullException.ThrowIfNull(accountSessionService);
            ArgumentNullException.ThrowIfNull(capabilityService);
            ArgumentNullException.ThrowIfNull(deviceUnlockService);
            ArgumentNullException.ThrowIfNull(windowPrivacyService);
            ArgumentNullException.ThrowIfNull(notificationPermissionService);
            ArgumentNullException.ThrowIfNull(mediaAccessPolicy);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _settingsStore = settingsStore;
            _runtimeStateStore = runtimeStateStore;
            _instanceStore = instanceStore;
            _logoutCleanupSettingsStore = logoutCleanupSettingsStore;
            _accountSessionService = accountSessionService;
            _capabilityService = capabilityService;
            _deviceUnlockService = deviceUnlockService;
            _windowPrivacyService = windowPrivacyService;
            _notificationPermissionService = notificationPermissionService;
            _mediaAccessPolicy = mediaAccessPolicy;
            _dialogService = dialogService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            VerifyDeviceUnlockCommand = new AsyncCommand(
                VerifyDeviceUnlockAsync,
                LogUnhandledCommandException,
                () => !IsBusy && DeviceUnlockDisplay.CanVerify);
            RevokeCurrentSessionCommand = new AsyncCommand(
                RevokeCurrentSessionAsync,
                LogUnhandledCommandException,
                () => !IsBusy && CanRevokeCurrentSession);
            ShowAppLock(CottonAppLockSettingsDisplayState.Create(_settings, _capability));
            ShowDeviceUnlock(CottonDeviceUnlockDisplayState.Create(_deviceUnlockAvailability));
            ShowLogoutCleanup(CottonLogoutCacheCleanupDisplayState.Create(_logoutCleanupSettings));
            ShowPermissionLedger(
                CottonPermissionLedgerDisplayState.Unavailable("Device access has not been checked."));
            ShowAccountSessions(
                CottonAccountSessionListDisplayState.Unavailable("Account sessions have not been loaded."));
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand VerifyDeviceUnlockCommand { get; }

        public AsyncCommand RevokeCurrentSessionCommand { get; }

        public ObservableCollection<CottonAccountSessionListItem> AccountSessions { get; } = [];

        public ObservableCollection<CottonPermissionLedgerItem> PermissionLedgerItems { get; } = [];

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanToggleAppLock));
                    OnPropertyChanged(nameof(CanToggleLogoutCacheCleanup));
                    OnPropertyChanged(nameof(CanRevokeCurrentSession));
                    LoadCommand.RaiseCanExecuteChanged();
                    VerifyDeviceUnlockCommand.RaiseCanExecuteChanged();
                    RevokeCurrentSessionCommand.RaiseCanExecuteChanged();
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

        public string PermissionLedgerTitle => PermissionLedgerDisplay.Title;

        public string PermissionLedgerStatusText => PermissionLedgerDisplay.StatusText;

        public string PermissionLedgerDetailText => PermissionLedgerDisplay.DetailText;

        public bool IsPermissionLedgerVisible => PermissionLedgerDisplay.HasItems;

        public string LogoutCacheCleanupTitle => LogoutCleanupDisplay.Title;

        public string LogoutCacheCleanupStatusText => LogoutCleanupDisplay.StatusText;

        public string LogoutCacheCleanupDetailText => LogoutCleanupDisplay.DetailText;

        public bool IsLogoutCacheCleanupEnabled
        {
            get => _isLogoutCacheCleanupEnabled;
            set
            {
                if (!SetProperty(ref _isLogoutCacheCleanupEnabled, value)
                    || _isApplyingLogoutCleanupSourceValue)
                {
                    return;
                }

                _ = SaveLogoutCacheCleanupAsync(value);
            }
        }

        public bool CanToggleLogoutCacheCleanup => !IsBusy;

        public string AccountSessionsTitle => AccountSessionDisplay.Title;

        public string AccountSessionsStatusText => AccountSessionDisplay.StatusText;

        public string AccountSessionsDetailText => AccountSessionDisplay.DetailText;

        public string AccountSessionsEmptyTitle => AccountSessionDisplay.EmptyTitle;

        public string AccountSessionsEmptyDetails => AccountSessionDisplay.EmptyDetails;

        public bool IsAccountSessionsListVisible => AccountSessionDisplay.HasItems;

        public bool IsAccountSessionsEmptyVisible => AccountSessionDisplay.IsEmptyVisible;

        public string RevokeCurrentSessionActionText => AccountSessionDisplay.CurrentSessionRevokeActionText;

        public bool IsRevokeCurrentSessionVisible => _currentSessionRevocationHandler is not null
            && AccountSessionDisplay.CanRevokeCurrentSession;

        public bool CanRevokeCurrentSession => IsRevokeCurrentSessionVisible && !IsBusy;

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

        public void SetCurrentSessionRevocationHandler(
            ICottonCurrentSessionRevocationHandler revocationHandler)
        {
            ArgumentNullException.ThrowIfNull(revocationHandler);

            _currentSessionRevocationHandler = revocationHandler;
            OnPropertyChanged(nameof(IsRevokeCurrentSessionVisible));
            OnPropertyChanged(nameof(CanRevokeCurrentSession));
            RevokeCurrentSessionCommand.RaiseCanExecuteChanged();
        }

        private CottonAppLockSettingsDisplayState AppLockDisplay { get; set; } =
            CottonAppLockSettingsDisplayState.Create(
                CottonAppLockSettings.Disabled,
                CottonAppLockCapabilitySnapshot.Unavailable("App lock status has not been checked."));

        private CottonDeviceUnlockDisplayState DeviceUnlockDisplay { get; set; } =
            CottonDeviceUnlockDisplayState.Create(
                CottonDeviceUnlockAvailabilitySnapshot.Unavailable("Device unlock status has not been checked."));

        private CottonLogoutCacheCleanupDisplayState LogoutCleanupDisplay { get; set; } =
            CottonLogoutCacheCleanupDisplayState.Create(CottonLogoutCacheCleanupSettings.Default);

        private CottonPermissionLedgerDisplayState PermissionLedgerDisplay { get; set; } =
            CottonPermissionLedgerDisplayState.Unavailable("Device access has not been checked.");

        private CottonAccountSessionListDisplayState AccountSessionDisplay { get; set; } =
            CottonAccountSessionListDisplayState.Unavailable("Account sessions have not been loaded.");

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
                _logoutCleanupSettings = await _logoutCleanupSettingsStore.GetAsync();
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
                ShowLogoutCleanup(CottonLogoutCacheCleanupDisplayState.Create(_logoutCleanupSettings));
                bool loadedPermissionLedger = await TryLoadPermissionLedgerAsync();
                bool loadedAccountSessions = await TryLoadAccountSessionsAsync();
                Status = disabledUnavailableAppLock
                    ? "App lock was turned off because device unlock is unavailable."
                    : !loadedPermissionLedger
                        ? "Could not inspect device access."
                        : _capability.CanEnable
                            ? loadedAccountSessions ? null : "Could not load account sessions."
                            : "App lock is unavailable.";
                if (disabledUnavailableAppLock)
                {
                    _ = _windowPrivacyService.ApplyAsync();
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
                ShowLogoutCleanup(CottonLogoutCacheCleanupDisplayState.Create(CottonLogoutCacheCleanupSettings.Default));
                ShowPermissionLedger(CottonPermissionLedgerDisplayState.Unavailable(
                    "Could not inspect device access."));
                ShowAccountSessions(CottonAccountSessionListDisplayState.Unavailable(
                    "Could not load signed-in devices."));
                Status = "Could not inspect security settings.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<bool> TryLoadPermissionLedgerAsync()
        {
            try
            {
                CottonNotificationPermissionState notificationPermission =
                    await _notificationPermissionService.GetPermissionStateAsync();
                CottonCameraBackupMediaAccessState mediaAccess =
                    await _mediaAccessPolicy.GetAccessStateAsync();
                ShowPermissionLedger(CottonPermissionLedgerDisplayState.Create(
                    notificationPermission,
                    mediaAccess,
                    _settings,
                    _capability,
                    _deviceUnlockAvailability));
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to inspect Cotton mobile permission ledger.");
                ShowPermissionLedger(CottonPermissionLedgerDisplayState.Unavailable(
                    "Could not inspect device access."));
                return false;
            }
        }

        private async Task<bool> TryLoadAccountSessionsAsync()
        {
            try
            {
                Uri instanceUri = await GetCurrentInstanceUriAsync();
                IReadOnlyList<CottonAccountSessionSnapshot> sessions =
                    await _accountSessionService.GetActiveSessionsAsync(instanceUri);
                ShowAccountSessions(CottonAccountSessionListDisplayState.Create(sessions));
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile account sessions.");
                ShowAccountSessions(CottonAccountSessionListDisplayState.Unavailable(
                    "Could not load signed-in devices."));
                return false;
            }
        }

        private async Task<Uri> GetCurrentInstanceUriAsync()
        {
            Uri? instanceUri = await _instanceStore.GetAsync();
            return instanceUri
                ?? throw new InvalidOperationException("A signed-in Cotton instance is required.");
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
                _ = _windowPrivacyService.ApplyAsync();
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

        private async Task SaveLogoutCacheCleanupAsync(bool isEnabled)
        {
            IsBusy = true;
            try
            {
                _logoutCleanupSettings = _logoutCleanupSettings.WithClearCachedFilesOnLogout(isEnabled);
                await _logoutCleanupSettingsStore.SaveAsync(_logoutCleanupSettings);
                ShowLogoutCleanup(CottonLogoutCacheCleanupDisplayState.Create(_logoutCleanupSettings));
                Status = isEnabled
                    ? "Cache will be cleared on logout."
                    : "Cache will be kept on logout.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to save Cotton mobile logout cache cleanup settings.");
                ApplyLogoutCleanupSourceValue(LogoutCleanupDisplay.IsEnabled);
                Status = "Could not update logout cleanup.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RevokeCurrentSessionAsync()
        {
            string? sessionId = AccountSessionDisplay.CurrentSessionId;
            if (_currentSessionRevocationHandler is null || string.IsNullOrWhiteSpace(sessionId))
            {
                Status = "Current session is unavailable.";
                return;
            }

            bool confirmed = await _dialogService.ShowConfirmationAsync(
                RevokeCurrentSessionConfirmationTitle,
                _logoutCleanupSettings.ClearCachedFilesOnLogout
                    ? RevokeCurrentSessionClearCacheMessage
                    : RevokeCurrentSessionKeepCacheMessage,
                RevokeCurrentSessionAction,
                CancelAction);
            if (!confirmed)
            {
                return;
            }

            IsBusy = true;
            try
            {
                await _currentSessionRevocationHandler.RevokeCurrentSessionAsync(sessionId);
                Status = "Current session revoked.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to revoke the current Cotton mobile account session.");
                Status = "Could not revoke current session.";
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

        private void ShowPermissionLedger(CottonPermissionLedgerDisplayState display)
        {
            ArgumentNullException.ThrowIfNull(display);

            PermissionLedgerDisplay = display;
            PermissionLedgerItems.Clear();
            foreach (CottonPermissionLedgerItem item in display.Items)
            {
                PermissionLedgerItems.Add(item);
            }

            OnPropertyChanged(nameof(PermissionLedgerTitle));
            OnPropertyChanged(nameof(PermissionLedgerStatusText));
            OnPropertyChanged(nameof(PermissionLedgerDetailText));
            OnPropertyChanged(nameof(IsPermissionLedgerVisible));
        }

        private void ShowLogoutCleanup(CottonLogoutCacheCleanupDisplayState display)
        {
            ArgumentNullException.ThrowIfNull(display);

            LogoutCleanupDisplay = display;
            ApplyLogoutCleanupSourceValue(display.IsEnabled);
            OnPropertyChanged(nameof(LogoutCacheCleanupTitle));
            OnPropertyChanged(nameof(LogoutCacheCleanupStatusText));
            OnPropertyChanged(nameof(LogoutCacheCleanupDetailText));
            OnPropertyChanged(nameof(CanToggleLogoutCacheCleanup));
        }

        private void ShowAccountSessions(CottonAccountSessionListDisplayState display)
        {
            ArgumentNullException.ThrowIfNull(display);

            AccountSessionDisplay = display;
            AccountSessions.Clear();
            foreach (CottonAccountSessionListItem item in display.Items)
            {
                AccountSessions.Add(item);
            }

            OnPropertyChanged(nameof(AccountSessionsTitle));
            OnPropertyChanged(nameof(AccountSessionsStatusText));
            OnPropertyChanged(nameof(AccountSessionsDetailText));
            OnPropertyChanged(nameof(AccountSessionsEmptyTitle));
            OnPropertyChanged(nameof(AccountSessionsEmptyDetails));
            OnPropertyChanged(nameof(IsAccountSessionsListVisible));
            OnPropertyChanged(nameof(IsAccountSessionsEmptyVisible));
            OnPropertyChanged(nameof(RevokeCurrentSessionActionText));
            OnPropertyChanged(nameof(IsRevokeCurrentSessionVisible));
            OnPropertyChanged(nameof(CanRevokeCurrentSession));
            RevokeCurrentSessionCommand.RaiseCanExecuteChanged();
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

        private void ApplyLogoutCleanupSourceValue(bool isEnabled)
        {
            _isApplyingLogoutCleanupSourceValue = true;
            try
            {
                IsLogoutCacheCleanupEnabled = isEnabled;
            }
            finally
            {
                _isApplyingLogoutCleanupSourceValue = false;
            }
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile security settings command exception.");
        }
    }
}
