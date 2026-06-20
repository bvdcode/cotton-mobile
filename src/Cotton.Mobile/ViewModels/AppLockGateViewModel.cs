using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class AppLockGateViewModel : ViewModelBase
    {
        private readonly ICottonDeviceUnlockService _deviceUnlockService;
        private readonly ILogger<AppLockGateViewModel> _logger;
        private TaskCompletionSource<CottonDeviceUnlockResult>? _completion;
        private bool _isBusy;
        private string _statusText = "Unlock Cotton to continue.";

        public AppLockGateViewModel(
            ICottonDeviceUnlockService deviceUnlockService,
            ILogger<AppLockGateViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(deviceUnlockService);
            ArgumentNullException.ThrowIfNull(logger);

            _deviceUnlockService = deviceUnlockService;
            _logger = logger;
            UnlockCommand = new AsyncCommand(UnlockAsync, LogUnhandledCommandException, () => CanUnlock);
        }

        public AsyncCommand UnlockCommand { get; }

        public string Title => "Cotton locked";

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string UnlockActionText => IsBusy ? "Unlocking..." : "Unlock";

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(CanUnlock));
                    OnPropertyChanged(nameof(UnlockActionText));
                    UnlockCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanUnlock => !IsBusy;

        public void SetCompletion(TaskCompletionSource<CottonDeviceUnlockResult> completion)
        {
            ArgumentNullException.ThrowIfNull(completion);

            _completion = completion;
        }

        private async Task UnlockAsync()
        {
            if (_completion is null)
            {
                StatusText = "App lock is not ready.";
                return;
            }

            IsBusy = true;
            try
            {
                CottonDeviceUnlockResult result = await _deviceUnlockService.RequestUnlockAsync();
                if (result.IsSucceeded)
                {
                    StatusText = "Unlocked.";
                    _completion.TrySetResult(result);
                    return;
                }

                StatusText = result.DetailText;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to unlock Cotton mobile app lock gate.");
                StatusText = "Could not unlock Cotton.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile app lock gate command exception.");
        }
    }
}
