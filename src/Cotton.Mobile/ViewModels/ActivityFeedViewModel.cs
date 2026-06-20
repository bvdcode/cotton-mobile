using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class ActivityFeedViewModel : ViewModelBase
    {
        private readonly Uri _instanceUri;
        private readonly ICottonActivityFeedService _activityFeedService;
        private readonly ILogger<ActivityFeedViewModel> _logger;
        private bool _isBusy;
        private string _summaryText = "0 items";
        private string _emptyMessage = "No activity yet";
        private string _emptyDetails = "Nothing needs attention right now.";
        private string? _status;

        public ActivityFeedViewModel(
            Uri instanceUri,
            ICottonActivityFeedService activityFeedService,
            ILogger<ActivityFeedViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(activityFeedService);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _activityFeedService = activityFeedService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public ObservableCollection<CottonActivityFeedListItem> Items { get; } = [];

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
                CottonActivityFeedPageSnapshot page = await _activityFeedService.GetPageAsync(
                    _instanceUri,
                    new CottonActivityFeedQuery());
                ShowSnapshot(CottonActivityFeedListSnapshot.Create(page, TimeZoneInfo.Local));
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile activity feed load failed.");
                Status = "Could not load activity.";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private void ShowSnapshot(CottonActivityFeedListSnapshot snapshot)
        {
            Items.Clear();
            foreach (CottonActivityFeedListItem item in snapshot.Items)
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
            _logger.LogError(exception, "Unhandled Cotton mobile activity feed command exception.");
        }
    }
}
