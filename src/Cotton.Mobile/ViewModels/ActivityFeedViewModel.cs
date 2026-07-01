// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class ActivityFeedViewModel : ViewModelBase
    {
        private const int PageSize = 20;

        private readonly Uri _instanceUri;
        private readonly ICottonActivityFeedService _activityFeedService;
        private readonly ILogger<ActivityFeedViewModel> _logger;
        private bool _isLoadingPlaceholderEnabled;
        private bool _isBusy;
        private CottonActivityFeedPagingState _pagingState = CottonActivityFeedPagingState.Empty;
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
            LoadMoreCommand = new AsyncCommand(LoadMoreAsync, LogUnhandledCommandException, CanLoadMore);
        }

        public ObservableCollection<CottonActivityFeedListItem> Items { get; } = [];

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand LoadMoreCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoadCommand.RaiseCanExecuteChanged();
                    LoadMoreCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
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

        public bool IsLoadingPlaceholderVisible => _isLoadingPlaceholderEnabled && IsBusy && Items.Count == 0;

        public bool IsListVisible => Items.Count > 0;

        public bool IsLoadMoreVisible => Items.Count > 0 && _pagingState.MayHaveMore;

        private async Task LoadAsync()
        {
            if (IsBusy)
            {
                return;
            }

            _isLoadingPlaceholderEnabled = Items.Count == 0;
            IsBusy = true;
            try
            {
                CottonActivityFeedPageSnapshot page = await _activityFeedService.GetPageAsync(
                    _instanceUri,
                    new CottonActivityFeedQuery(page: 1, pageSize: PageSize));
                ShowSnapshot(CottonActivityFeedListSnapshot.Create(page, TimeZoneInfo.Local), append: false);
                ShowPagingState(_pagingState.ApplyRefresh(page));
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
                _isLoadingPlaceholderEnabled = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                OnPropertyChanged(nameof(IsListVisible));
                OnPropertyChanged(nameof(IsLoadMoreVisible));
            }
        }

        private async Task LoadMoreAsync()
        {
            if (!CanLoadMore())
            {
                return;
            }

            IsBusy = true;
            try
            {
                CottonActivityFeedPageSnapshot page = await _activityFeedService.GetPageAsync(
                    _instanceUri,
                    new CottonActivityFeedQuery(page: _pagingState.NextPage, pageSize: PageSize));
                ShowSnapshot(CottonActivityFeedListSnapshot.Create(page, TimeZoneInfo.Local), append: true);
                ShowPagingState(_pagingState.ApplyAppend(page));
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile activity feed load more failed.");
                Status = "Could not load more activity.";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                OnPropertyChanged(nameof(IsListVisible));
                OnPropertyChanged(nameof(IsLoadMoreVisible));
            }
        }

        private void ShowSnapshot(CottonActivityFeedListSnapshot snapshot, bool append)
        {
            if (!append)
            {
                Items.Clear();
            }

            foreach (CottonActivityFeedListItem item in snapshot.Items)
            {
                Items.Add(item);
            }

            EmptyMessage = snapshot.EmptyMessage;
            EmptyDetails = snapshot.EmptyDetails;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
            OnPropertyChanged(nameof(IsListVisible));
        }

        private void ShowPagingState(CottonActivityFeedPagingState pagingState)
        {
            _pagingState = pagingState;
            SummaryText = CottonActivityFeedListSnapshot.CreateSummaryText(
                Items.ToArray(),
                _pagingState.TotalItemCount);
            OnPropertyChanged(nameof(IsLoadMoreVisible));
            LoadMoreCommand.RaiseCanExecuteChanged();
        }

        private bool CanLoadMore()
        {
            return !IsBusy && IsLoadMoreVisible;
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile activity feed command exception.");
        }
    }
}
