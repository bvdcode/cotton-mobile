using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class TransfersViewModel : ViewModelBase
    {
        private readonly Uri _instanceUri;
        private readonly ICottonTransferMetadataStore _metadataStore;
        private readonly ILogger<TransfersViewModel> _logger;

        private bool _isBusy;
        private string _summaryText = "0 transfers";
        private string _emptyMessage = "No transfers yet";
        private string _emptyDetails = "Nothing is waiting right now.";
        private string? _status;

        public TransfersViewModel(
            Uri instanceUri,
            ICottonTransferMetadataStore metadataStore,
            ILogger<TransfersViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(metadataStore);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _metadataStore = metadataStore;
            _logger = logger;
            Items = [];
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public ObservableCollection<CottonTransferListItem> Items { get; }

        public AsyncCommand LoadCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                    LoadCommand.RaiseCanExecuteChanged();
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

        private async Task LoadAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                IReadOnlyList<CottonTransferQueueItem> transfers = await _metadataStore.LoadAsync(_instanceUri);
                ShowSnapshot(CottonTransferListSnapshot.Create(transfers));
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile transfer list load failed.");
                Status = "Could not load transfers.";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private void ShowSnapshot(CottonTransferListSnapshot snapshot)
        {
            Items.Clear();
            foreach (CottonTransferListItem item in snapshot.Items)
            {
                Items.Add(item);
            }

            SummaryText = snapshot.SummaryText;
            EmptyMessage = snapshot.EmptyMessage;
            EmptyDetails = snapshot.EmptyDetails;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsListVisible));
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile transfers command exception.");
        }
    }
}
