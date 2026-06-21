using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class RecentFilesViewModel : ViewModelBase
    {
        private readonly Uri _instanceUri;
        private readonly ICottonRecentFileStore _recentFileStore;
        private readonly ILogger<RecentFilesViewModel> _logger;
        private bool _isBusy;
        private string _summaryText = "No recent files";
        private string _emptyMessage = "No recent files yet";
        private string _emptyDetails = "Open, download, or share files to build this list.";
        private string? _status;

        public RecentFilesViewModel(
            Uri instanceUri,
            ICottonRecentFileStore recentFileStore,
            ILogger<RecentFilesViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(recentFileStore);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _recentFileStore = recentFileStore;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public ObservableCollection<CottonRecentFileListItem> Items { get; } = [];

        public AsyncCommand LoadCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoadCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
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
                IReadOnlyList<CottonRecentFileSnapshot> recentFiles =
                    await _recentFileStore.LoadAsync(_instanceUri);
                ShowSnapshot(CottonRecentFileListSnapshot.Create(recentFiles));
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile recent files load failed.");
                Status = "Could not load recent files.";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private void ShowSnapshot(CottonRecentFileListSnapshot snapshot)
        {
            Items.Clear();
            foreach (CottonRecentFileListItem item in snapshot.Items)
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
            _logger.LogError(exception, "Unhandled Cotton mobile recent files command exception.");
        }
    }
}
