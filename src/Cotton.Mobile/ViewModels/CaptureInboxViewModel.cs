using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class CaptureInboxViewModel : ViewModelBase
    {
        private readonly Uri _instanceUri;
        private readonly ICottonShareIntakeStore _intakeStore;
        private readonly ICottonShareContentStagingStore _contentStagingStore;
        private readonly ICaptureDestinationPickerPageService _destinationPickerPageService;
        private readonly ILogger<CaptureInboxViewModel> _logger;

        private bool _isBusy;
        private string _summaryText = "0 captured items";
        private string _emptyMessage = "No captured items";
        private string _emptyDetails = "Shared files and text will appear here.";
        private string? _status;

        public CaptureInboxViewModel(
            Uri instanceUri,
            ICottonShareIntakeStore intakeStore,
            ICottonShareContentStagingStore contentStagingStore,
            ICaptureDestinationPickerPageService destinationPickerPageService,
            ILogger<CaptureInboxViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(intakeStore);
            ArgumentNullException.ThrowIfNull(contentStagingStore);
            ArgumentNullException.ThrowIfNull(destinationPickerPageService);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _intakeStore = intakeStore;
            _contentStagingStore = contentStagingStore;
            _destinationPickerPageService = destinationPickerPageService;
            _logger = logger;
            Items = [];
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            DestinationCommand = new AsyncCommand(
                OpenDestinationPickerAsync,
                LogUnhandledCommandException,
                () => !IsBusy && Items.Any(item => item.CanSelectDestination));
            ClearCommand = new AsyncCommand(ClearAsync, LogUnhandledCommandException, () => !IsBusy && Items.Count > 0);
        }

        public ObservableCollection<CottonCaptureInboxListItem> Items { get; }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand DestinationCommand { get; }

        public AsyncCommand ClearCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                    LoadCommand.RaiseCanExecuteChanged();
                    DestinationCommand.RaiseCanExecuteChanged();
                    ClearCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SummaryText
        {
            get => _summaryText;
            private set => SetProperty(ref _summaryText, value);
        }

        public string EmptyMessage
        {
            get => _emptyMessage;
            private set => SetProperty(ref _emptyMessage, value);
        }

        public string EmptyDetails
        {
            get => _emptyDetails;
            private set => SetProperty(ref _emptyDetails, value);
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

        public bool IsEmpty => Items.Count == 0 && !IsBusy;

        public bool IsListVisible => Items.Count > 0;

        public bool CanChooseDestination => Items.Any(item => item.CanSelectDestination);

        private async Task LoadAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                IReadOnlyList<CottonShareIntakeSnapshot> snapshots = await _intakeStore.LoadAsync();
                ShowSnapshot(CottonCaptureInboxListSnapshot.Create(snapshots));
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile capture inbox load failed.");
                Status = "Could not load captured items.";
            }
            finally
            {
                IsBusy = false;
                RaiseListStateChanged();
            }
        }

        private async Task ClearAsync()
        {
            if (IsBusy || Items.Count == 0)
            {
                return;
            }

            IsBusy = true;
            try
            {
                await _intakeStore.ClearAsync();
                await _contentStagingStore.CleanupAsync([]);
                ShowSnapshot(CottonCaptureInboxListSnapshot.Create([]));
                Status = "Capture inbox cleared.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile capture inbox clear failed.");
                Status = "Could not clear captured items.";
            }
            finally
            {
                IsBusy = false;
                RaiseListStateChanged();
            }
        }

        private async Task OpenDestinationPickerAsync()
        {
            if (IsBusy || !CanChooseDestination)
            {
                return;
            }

            try
            {
                await _destinationPickerPageService.OpenAsync(_instanceUri);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile capture destination picker open failed.");
                Status = "Could not open destination picker.";
            }
        }

        private void ShowSnapshot(CottonCaptureInboxListSnapshot snapshot)
        {
            Items.Clear();
            foreach (CottonCaptureInboxListItem item in snapshot.Items)
            {
                Items.Add(item);
            }

            SummaryText = snapshot.SummaryText;
            EmptyMessage = snapshot.EmptyMessage;
            EmptyDetails = snapshot.EmptyDetails;
            RaiseListStateChanged();
        }

        private void RaiseListStateChanged()
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsListVisible));
            OnPropertyChanged(nameof(CanChooseDestination));
            DestinationCommand.RaiseCanExecuteChanged();
            ClearCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile capture inbox command exception.");
        }
    }
}
