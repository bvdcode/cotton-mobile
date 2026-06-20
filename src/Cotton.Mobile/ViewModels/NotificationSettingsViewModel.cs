using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class NotificationSettingsViewModel : ViewModelBase
    {
        private readonly ICottonInstanceStore _instanceStore;
        private readonly ICottonRemotePushPreferenceService _remotePushPreferenceService;
        private readonly ICottonNotificationPermissionService _permissionService;
        private readonly ILogger<NotificationSettingsViewModel> _logger;
        private CottonNotificationSettings _settings = CottonNotificationSettings.Default;
        private CottonRemotePushPreferences? _remotePushPreferences;
        private string? _remotePushFailureStatus;
        private CottonNotificationPermissionDisplayState _permissionDisplay =
            CottonNotificationPermissionDisplayState.Create(
                CottonNotificationSettings.Default,
                CottonNotificationPermissionState.Unavailable);
        private bool _isBusy;
        private string? _status;
        private bool _isStatusAttention;

        public NotificationSettingsViewModel(
            ICottonInstanceStore instanceStore,
            ICottonRemotePushPreferenceService remotePushPreferenceService,
            ICottonNotificationPermissionService permissionService,
            ILogger<NotificationSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceStore);
            ArgumentNullException.ThrowIfNull(remotePushPreferenceService);
            ArgumentNullException.ThrowIfNull(permissionService);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceStore = instanceStore;
            _remotePushPreferenceService = remotePushPreferenceService;
            _permissionService = permissionService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            PermissionActionCommand = new AsyncCommand(
                RunPermissionActionAsync,
                LogUnhandledCommandException,
                () => !IsBusy && IsPermissionActionVisible);
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand PermissionActionCommand { get; }

        public ObservableCollection<RemotePushPreferenceItemViewModel> RemotePushPreferences { get; } = [];

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    UpdateRemotePushToggleAvailability();
                    RaiseCommandStatesChanged();
                }
            }
        }

        public string PermissionTitle => _permissionDisplay.Title;

        public string PermissionStatusText => _permissionDisplay.StatusText;

        public string PermissionDetailText => _permissionDisplay.DetailText;

        public string PermissionActionText => _permissionDisplay.ActionText;

        public bool IsPermissionActionVisible => _permissionDisplay.IsActionVisible;

        public bool CanSendNotifications => _permissionDisplay.CanSendNotifications;

        public bool NeedsAttention => _permissionDisplay.NeedsAttention;

        public string EnabledCategoriesText => _remotePushPreferences is null
            ? $"{_settings.EnabledChannelCount:N0} local categories on"
            : $"{_settings.EnabledChannelCount:N0} local · {CreateRemotePushDisplay().EnabledCategoryCount:N0} server push";

        public bool HasRemotePushPreferences => _remotePushPreferences is not null;

        public bool IsRemotePushUnavailable => _remotePushPreferences is null;

        public string RemotePushUnavailableTitle => _remotePushFailureStatus ?? "Server push preferences not loaded.";

        public string RemotePushUnavailableDetail => _remotePushFailureStatus is null
            ? "Refresh this page to load server push controls."
            : "Local notifications still work. Retry when the server is reachable.";

        public string RemotePushStatusText => _remotePushPreferences is null
            ? _remotePushFailureStatus ?? "Server push preferences not loaded."
            : CreateRemotePushDisplay().SummaryText;

        public string? Status => _status;

        public bool IsStatusVisible => !string.IsNullOrWhiteSpace(Status);

        public bool IsAttentionStatusVisible => IsStatusVisible && _isStatusAttention;

        public bool IsNeutralStatusVisible => IsStatusVisible && !_isStatusAttention;

        private async Task LoadAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            string? failureStatus = null;
            try
            {
                try
                {
                    CottonNotificationPermissionState permissionState =
                        await _permissionService.GetPermissionStateAsync();
                    ShowPermission(permissionState);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to inspect Cotton mobile notification permission.");
                    failureStatus = "Could not inspect notification permission.";
                }

                if (!await TryLoadRemotePushPreferencesAsync())
                {
                    failureStatus = failureStatus is null
                        ? "Could not load push preferences."
                        : "Could not inspect notifications.";
                }

                ShowStatus(failureStatus, failureStatus is not null);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RunPermissionActionAsync()
        {
            await RunNotificationActionAsync(
                async () =>
                {
                    if (_permissionDisplay.ShouldOpenSettings)
                    {
                        await _permissionService.OpenSettingsAsync();
                        ShowStatus("Opened notification settings.");
                        return;
                    }

                    CottonNotificationPermissionState permissionState =
                        await _permissionService.RequestPermissionAsync();
                    ShowPermission(permissionState);
                    if (permissionState == CottonNotificationPermissionState.Allowed
                        || permissionState == CottonNotificationPermissionState.NotRequired)
                    {
                        ShowStatus("Notifications allowed.");
                        return;
                    }

                    ShowStatus("Notifications not allowed.", isAttention: true);
                },
                "Could not update notifications.");
        }

        private async Task LoadRemotePushPreferencesAsync()
        {
            Uri instanceUri = await GetCurrentInstanceUriAsync();
            CottonRemotePushPreferences preferences =
                await _remotePushPreferenceService.GetCurrentAsync(instanceUri);
            ShowRemotePushPreferences(preferences);
        }

        private async Task<bool> TryLoadRemotePushPreferencesAsync()
        {
            try
            {
                await LoadRemotePushPreferencesAsync();
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton mobile push notification preferences.");
                _remotePushPreferences = null;
                _remotePushFailureStatus = "Server push unavailable.";
                RemotePushPreferences.Clear();
                RaiseRemotePushPresentationChanged();
                return false;
            }
        }

        private async void RemotePushPreference_ToggleRequested(object? sender, EventArgs args)
        {
            if (sender is RemotePushPreferenceItemViewModel item)
            {
                await UpdateRemotePushPreferenceAsync(item);
            }
        }

        private async Task UpdateRemotePushPreferenceAsync(RemotePushPreferenceItemViewModel item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (_remotePushPreferences is null || IsBusy)
            {
                item.ApplySourceValue(_remotePushPreferences?.IsEnabled(item.Category) ?? false);
                return;
            }

            CottonRemotePushPreferences previous = _remotePushPreferences;
            bool requestedEnabled = item.IsEnabled;
            if (previous.IsEnabled(item.Category) == requestedEnabled)
            {
                return;
            }

            CottonRemotePushPreferences requested = previous.WithCategory(item.Category, requestedEnabled);
            IsBusy = true;
            try
            {
                Uri instanceUri = await GetCurrentInstanceUriAsync();
                CottonRemotePushPreferences updated =
                    await _remotePushPreferenceService.UpdateCurrentAsync(instanceUri, requested);
                ShowRemotePushPreferences(updated);
                ShowStatus("Push preferences updated.");
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to update Cotton mobile push notification preferences.");
                ShowRemotePushPreferences(previous);
                ShowStatus("Could not update push preferences.", isAttention: true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<Uri> GetCurrentInstanceUriAsync()
        {
            Uri? instanceUri = await _instanceStore.GetAsync();
            return instanceUri
                ?? throw new InvalidOperationException("A signed-in Cotton instance is required.");
        }

        private async Task RunNotificationActionAsync(Func<Task> actionAsync, string failureStatus)
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
                _logger.LogWarning(exception, "Cotton mobile notification settings action failed.");
                ShowStatus(failureStatus, isAttention: true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ShowRemotePushPreferences(CottonRemotePushPreferences preferences)
        {
            ArgumentNullException.ThrowIfNull(preferences);

            _remotePushPreferences = preferences;
            _remotePushFailureStatus = null;
            CottonRemotePushPreferenceDisplayState display = CreateRemotePushDisplay();
            RemotePushPreferences.Clear();
            foreach (CottonRemotePushPreferenceDisplayItem item in display.Items)
            {
                var viewModel = new RemotePushPreferenceItemViewModel(item);
                viewModel.SetCanToggle(!IsBusy);
                viewModel.ToggleRequested += RemotePushPreference_ToggleRequested;
                RemotePushPreferences.Add(viewModel);
            }

            RaiseRemotePushPresentationChanged();
        }

        private CottonRemotePushPreferenceDisplayState CreateRemotePushDisplay()
        {
            return CottonRemotePushPreferenceDisplayState.Create(
                _remotePushPreferences
                    ?? throw new InvalidOperationException("Remote push preferences are not loaded."));
        }

        private void UpdateRemotePushToggleAvailability()
        {
            bool canToggle = !IsBusy && _remotePushPreferences is not null;
            foreach (RemotePushPreferenceItemViewModel item in RemotePushPreferences)
            {
                item.SetCanToggle(canToggle);
            }
        }

        private void ShowPermission(CottonNotificationPermissionState permissionState)
        {
            _permissionDisplay = CottonNotificationPermissionDisplayState.Create(_settings, permissionState);
            OnPropertyChanged(nameof(PermissionTitle));
            OnPropertyChanged(nameof(PermissionStatusText));
            OnPropertyChanged(nameof(PermissionDetailText));
            OnPropertyChanged(nameof(PermissionActionText));
            OnPropertyChanged(nameof(IsPermissionActionVisible));
            OnPropertyChanged(nameof(CanSendNotifications));
            OnPropertyChanged(nameof(NeedsAttention));
            OnPropertyChanged(nameof(EnabledCategoriesText));
            RaiseCommandStatesChanged();
        }

        private void ShowStatus(string? status, bool isAttention = false)
        {
            if (_status == status && _isStatusAttention == isAttention)
            {
                return;
            }

            _status = status;
            _isStatusAttention = isAttention;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsStatusVisible));
            OnPropertyChanged(nameof(IsAttentionStatusVisible));
            OnPropertyChanged(nameof(IsNeutralStatusVisible));
        }

        private void RaiseRemotePushPresentationChanged()
        {
            OnPropertyChanged(nameof(RemotePushStatusText));
            OnPropertyChanged(nameof(EnabledCategoriesText));
            OnPropertyChanged(nameof(HasRemotePushPreferences));
            OnPropertyChanged(nameof(IsRemotePushUnavailable));
            OnPropertyChanged(nameof(RemotePushUnavailableTitle));
            OnPropertyChanged(nameof(RemotePushUnavailableDetail));
        }

        private void RaiseCommandStatesChanged()
        {
            LoadCommand.RaiseCanExecuteChanged();
            PermissionActionCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile notification settings command exception.");
        }
    }
}
