using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsViewModel : ViewModelBase
    {
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ILogger<SyncSettingsViewModel> _logger;
        private Uri? _instanceUri;
        private bool _isBusy;
        private string _summaryText = "No folders syncing";
        private string? _status;
        private bool _isEmptyVisible = true;

        public SyncSettingsViewModel(
            ICottonSyncRootStore rootStore,
            ILogger<SyncSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(logger);

            _rootStore = rootStore;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public AsyncCommand LoadCommand { get; }

        public ObservableCollection<CottonSyncRootListItem> Roots { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoadCommand.RaiseCanExecuteChanged();
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
                IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore.LoadAsync(instanceUri);
                CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create(roots);
                Roots.Clear();
                foreach (CottonSyncRootListItem item in state.Items)
                {
                    Roots.Add(item);
                }

                SummaryText = state.SummaryText;
                IsEmptyVisible = state.IsEmptyVisible;
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

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile sync settings command failure.");
            Status = "Could not update sync settings.";
        }
    }
}
