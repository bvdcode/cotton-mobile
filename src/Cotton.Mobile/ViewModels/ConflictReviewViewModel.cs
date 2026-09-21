// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public partial class ConflictReviewViewModel : ObservableObject, IDisposable
    {
        private readonly CottonSyncRootSnapshot _root;
        private readonly CottonRemoteConflictResolutionService _service;
        private readonly ICottonAutomaticSyncBackgroundScheduler _scheduler;
        private readonly IUserDialogService _dialogs;
        private readonly ILogger<ConflictReviewViewModel> _logger;
        private readonly Action _close;
        private readonly CancellationTokenSource _lifetime = new();
        private bool _isBusy;
        private bool _isWorking;
        private string? _status;
        private bool _batchSelection;

        public ConflictReviewViewModel(CottonSyncRootSnapshot root, CottonRemoteConflictResolutionService service,
            ICottonAutomaticSyncBackgroundScheduler scheduler, IUserDialogService dialogs,
            ILogger<ConflictReviewViewModel> logger, Action close,
            CottonSyncProgressHub progressHub, ICottonAutomaticSyncStatusStore statusStore)
        {
            _root = root;
            _service = service;
            _scheduler = scheduler;
            _dialogs = dialogs;
            _logger = logger;
            _close = close;
            RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
            ReplaceCommand = new AsyncRelayCommand(ReplaceAsync, () => !IsBusy && (Items.Any(item => item.IsSelected) || Items.Any(item => item.IsQueued)));
            SelectAllCommand = new RelayCommand(SelectAll, () => !IsBusy && Items.Any(item => item.CanSelect));
            CloseCommand = new RelayCommand(Close);
            _progressHub = progressHub;
            _statusStore = statusStore;
            _progressHub.ProgressChanged += OnProgressChanged;
            _statusStore.StatusesChanged += OnStatusesChanged;
        }

        public RangeObservableCollection<ConflictReviewListItem> Items { get; } = [];
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand ReplaceCommand { get; }
        public IRelayCommand SelectAllCommand { get; }
        public IRelayCommand CloseCommand { get; }
        public string FolderName => _root.CloudFolder.FolderName;
        public bool IsEmpty => !IsBusy && Items.Count == 0 && Status is null;
        public string SelectAllText => Items.Any(item => item.CanSelect && !item.IsSelected)
            ? ConflictReviewResources.SelectAll : ConflictReviewResources.ClearSelection;
        public string ReplaceText => !Items.Any(item => item.IsSelected) && Items.Any(item => item.IsQueued)
            ? ConflictReviewResources.RetryQueue
            : string.Format(CultureInfo.CurrentCulture, ConflictReviewResources.Replace, Items.Count(item => item.IsSelected));
        public bool IsStatusVisible => !string.IsNullOrWhiteSpace(Status);
        public bool IsWorking
        {
            get => _isWorking;
            private set => SetProperty(ref _isWorking, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                SetProperty(ref _isBusy, value);
                NotifyActions();
            }
        }

        public string? Status
        {
            get => _status;
            private set
            {
                SetProperty(ref _status, value);
                OnPropertyChanged(nameof(IsStatusVisible));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        public async Task RefreshAsync()
        {
            IsBusy = true;
            IsWorking = true;
            Status = ConflictReviewResources.Loading;
            try
            {
                CottonSyncReviewState state = await Task.Run(() => _service.ScanAsync(_root, _lifetime.Token), _lifetime.Token);
                SetItems(state);

                Status = null;
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
            {
                Status = null;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Could not load upload conflicts.", exception);
                Status = ConflictReviewResources.Failed;
            }
            finally
            {
                IsWorking = false;
                IsBusy = false;
            }
        }

        private async Task ReplaceAsync()
        {
            IsBusy = true;
            try
            {
                CottonSyncConflictSnapshot[] selected = [.. Items.Where(item => item.IsSelected).Select(item => item.Conflict)];
                if (selected.Length > 0)
                {
                    bool confirmed = await _dialogs.ShowConfirmationAsync(
                        string.Format(CultureInfo.CurrentCulture, ConflictReviewResources.ConfirmTitle, selected.Length),
                        ConflictReviewResources.ConfirmBody, ConflictReviewResources.Confirm, ConflictReviewResources.Cancel);
                    if (!confirmed)
                    {
                        return;
                    }

                    IsWorking = true;
                    await Task.Run(() => _service.ApproveAsync(_root, selected, _lifetime.Token), _lifetime.Token);
                    _batchSelection = true;
                    foreach (ConflictReviewListItem item in Items.Where(item => item.IsSelected).ToArray())
                    {
                        item.IsQueued = true;
                    }
                    _batchSelection = false;
                }

                IsWorking = true;
                await _scheduler.ScheduleRootRetriesAsync([_root.Id], _lifetime.Token);
                Status = ConflictReviewResources.QueuedStatus;
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
            {
                Status = null;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Could not schedule selected upload replacements.", exception);
                Status = ConflictReviewResources.Failed;
            }
            finally
            {
                IsWorking = false;
                IsBusy = false;
            }
        }

        private void SelectAll()
        {
            bool select = Items.Any(item => item.CanSelect && !item.IsSelected);
            _batchSelection = true;
            foreach (ConflictReviewListItem item in Items.Where(item => item.CanSelect))
            {
                item.IsSelected = select;
            }
            _batchSelection = false;
            NotifyActions();
        }

        private void OnSelectionChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (!_batchSelection)
            {
                NotifyActions();
            }
        }

        private void NotifyActions()
        {
            RefreshCommand.NotifyCanExecuteChanged();
            ReplaceCommand.NotifyCanExecuteChanged();
            SelectAllCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(SelectAllText));
            OnPropertyChanged(nameof(ReplaceText));
        }

        public void Close()
        {
            _lifetime.Cancel();
            _close();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _progressHub.ProgressChanged -= OnProgressChanged;
            _statusStore.StatusesChanged -= OnStatusesChanged;
            _lifetime.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
