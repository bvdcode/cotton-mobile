using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class NotificationSettingsViewModel : ViewModelBase
    {
        private readonly ICottonNotificationPermissionService _permissionService;
        private readonly ILogger<NotificationSettingsViewModel> _logger;
        private CottonNotificationSettings _settings = CottonNotificationSettings.Default;
        private CottonNotificationPermissionDisplayState _permissionDisplay =
            CottonNotificationPermissionDisplayState.Create(
                CottonNotificationSettings.Default,
                CottonNotificationPermissionState.Unavailable);
        private bool _isBusy;
        private string? _status;

        public NotificationSettingsViewModel(
            ICottonNotificationPermissionService permissionService,
            ILogger<NotificationSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(permissionService);
            ArgumentNullException.ThrowIfNull(logger);

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

        public string PermissionTitle => _permissionDisplay.Title;

        public string PermissionStatusText => _permissionDisplay.StatusText;

        public string PermissionDetailText => _permissionDisplay.DetailText;

        public string PermissionActionText => _permissionDisplay.ActionText;

        public bool IsPermissionActionVisible => _permissionDisplay.IsActionVisible;

        public bool CanSendNotifications => _permissionDisplay.CanSendNotifications;

        public bool NeedsAttention => _permissionDisplay.NeedsAttention;

        public string EnabledCategoriesText => $"{_settings.EnabledChannelCount:N0} categories on";

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

        private async Task LoadAsync()
        {
            await RunNotificationActionAsync(
                async () =>
                {
                    CottonNotificationPermissionState permissionState =
                        await _permissionService.GetPermissionStateAsync();
                    ShowPermission(permissionState);
                    Status = null;
                },
                "Could not inspect notifications.");
        }

        private async Task RunPermissionActionAsync()
        {
            await RunNotificationActionAsync(
                async () =>
                {
                    if (_permissionDisplay.ShouldOpenSettings)
                    {
                        await _permissionService.OpenSettingsAsync();
                        Status = "Opened notification settings.";
                        return;
                    }

                    CottonNotificationPermissionState permissionState =
                        await _permissionService.RequestPermissionAsync();
                    ShowPermission(permissionState);
                    Status = permissionState == CottonNotificationPermissionState.Allowed
                        || permissionState == CottonNotificationPermissionState.NotRequired
                            ? "Notifications allowed."
                            : "Notifications not allowed.";
                },
                "Could not update notifications.");
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
                Status = failureStatus;
            }
            finally
            {
                IsBusy = false;
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
