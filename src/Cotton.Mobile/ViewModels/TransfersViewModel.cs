// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class TransfersViewModel : ViewModelBase
    {
        private readonly Uri _instanceUri;
        private readonly ICottonTransferMetadataStore _metadataStore;
        private readonly ICottonAndroidBackgroundTransferCoordinator _backgroundTransferCoordinator;
        private readonly IAndroidApiLevelProvider _androidApiLevelProvider;
        private readonly ICottonQueuedUploadExecutor _queuedUploadExecutor;
        private readonly ICottonTransferStagingStore _stagingStore;
        private readonly ICottonTransferActivitySignal _transferActivitySignal;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<TransfersViewModel> _logger;

        private bool _isLoadingPlaceholderEnabled;
        private bool _isBusy;
        private bool _canClearHistory;
        private string _summaryText = "0 transfers";
        private string _emptyMessage = "No transfers yet";
        private string _emptyDetails = "Nothing is waiting right now.";
        private string? _status;

        public TransfersViewModel(
            Uri instanceUri,
            ICottonTransferMetadataStore metadataStore,
            ICottonAndroidBackgroundTransferCoordinator backgroundTransferCoordinator,
            IAndroidApiLevelProvider androidApiLevelProvider,
            ICottonQueuedUploadExecutor queuedUploadExecutor,
            ICottonTransferStagingStore stagingStore,
            ICottonTransferActivitySignal transferActivitySignal,
            IUserDialogService dialogService,
            ILogger<TransfersViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(metadataStore);
            ArgumentNullException.ThrowIfNull(backgroundTransferCoordinator);
            ArgumentNullException.ThrowIfNull(androidApiLevelProvider);
            ArgumentNullException.ThrowIfNull(queuedUploadExecutor);
            ArgumentNullException.ThrowIfNull(stagingStore);
            ArgumentNullException.ThrowIfNull(transferActivitySignal);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _metadataStore = metadataStore;
            _backgroundTransferCoordinator = backgroundTransferCoordinator;
            _androidApiLevelProvider = androidApiLevelProvider;
            _queuedUploadExecutor = queuedUploadExecutor;
            _stagingStore = stagingStore;
            _transferActivitySignal = transferActivitySignal;
            _dialogService = dialogService;
            _logger = logger;
            Items = [];
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            RunWaitingCommand = new AsyncCommand(RunWaitingAsync, LogUnhandledCommandException, () => !IsBusy);
            ClearHistoryCommand = new AsyncCommand(
                ClearHistoryAsync,
                LogUnhandledCommandException,
                () => !IsBusy && _canClearHistory);
        }

        public RangeObservableCollection<CottonTransferListItem> Items { get; }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand RunWaitingCommand { get; }

        public AsyncCommand ClearHistoryCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                    LoadCommand.RaiseCanExecuteChanged();
                    RunWaitingCommand.RaiseCanExecuteChanged();
                    ClearHistoryCommand.RaiseCanExecuteChanged();
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
                IReadOnlyList<CottonTransferQueueItem> transfers = await _metadataStore.LoadAsync(_instanceUri);
                ShowTransfers(transfers);
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
                _isLoadingPlaceholderEnabled = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private async Task RunWaitingAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                CottonAndroidBackgroundTransferScheduleResult scheduleResult =
                    await _backgroundTransferCoordinator.ScheduleNextQueuedUploadAsync(
                        _instanceUri,
                        _androidApiLevelProvider.CurrentApiLevel);
                if (scheduleResult.IsScheduled
                    || scheduleResult.Status == CottonAndroidBackgroundTransferScheduleStatus.NoQueuedTransfer)
                {
                    IReadOnlyList<CottonTransferQueueItem> scheduledTransfers =
                        await _metadataStore.LoadAsync(_instanceUri);
                    ShowTransfers(scheduledTransfers);
                    Status = scheduleResult.Status == CottonAndroidBackgroundTransferScheduleStatus.NoQueuedTransfer
                        ? "No waiting uploads."
                        : scheduleResult.StatusText;
                    return;
                }

                Status = "Running next waiting upload...";
                CottonQueuedUploadExecutionResult result =
                    await _queuedUploadExecutor.ExecuteNextAsync(_instanceUri);
                IReadOnlyList<CottonTransferQueueItem> transfers = await _metadataStore.LoadAsync(_instanceUri);
                ShowTransfers(transfers);
                Status = CreateExecutionStatusText(result);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile queued upload execution failed.");
                Status = "Could not run waiting upload.";
                IReadOnlyList<CottonTransferQueueItem> transfers = await _metadataStore.LoadAsync(_instanceUri);
                ShowTransfers(transfers);
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private async Task ClearHistoryAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                IReadOnlyList<CottonTransferQueueItem> transfers = await _metadataStore.LoadAsync(_instanceUri);
                CottonTransferHistoryCleanupPlan plan = CottonTransferHistoryCleanupPolicy.CreatePlan(transfers);
                ShowSnapshot(CottonTransferListSnapshot.Create(transfers), plan.HasRemovedItems);
                if (!plan.HasRemovedItems)
                {
                    Status = CottonTransferHistoryCleanupText.CreateClearedStatus(plan);
                    return;
                }

                bool confirmed = await _dialogService.ShowConfirmationAsync(
                    CottonTransferHistoryCleanupText.ClearHistoryTitle,
                    CottonTransferHistoryCleanupText.ClearHistoryMessage,
                    CottonTransferHistoryCleanupText.ClearHistoryAction,
                    CottonTransferHistoryCleanupText.CancelAction);
                if (!confirmed)
                {
                    Status = null;
                    return;
                }

                await _metadataStore.SaveAsync(_instanceUri, plan.RetainedItems);
                await _stagingStore.CleanupAsync(_instanceUri, plan.RetainedItems);
                _transferActivitySignal.NotifyTransferActivityChanged();
                ShowSnapshot(CottonTransferListSnapshot.Create(plan.RetainedItems), canClearHistory: false);
                Status = CottonTransferHistoryCleanupText.CreateClearedStatus(plan);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile transfer history cleanup failed.");
                Status = "Could not clear transfer history.";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private void ShowTransfers(IReadOnlyList<CottonTransferQueueItem> transfers)
        {
            CottonTransferHistoryCleanupPlan plan = CottonTransferHistoryCleanupPolicy.CreatePlan(transfers);
            ShowSnapshot(CottonTransferListSnapshot.Create(transfers), plan.HasRemovedItems);
        }

        private void ShowSnapshot(CottonTransferListSnapshot snapshot, bool canClearHistory)
        {
            Items.ReplaceWith(snapshot.Items);

            SetCanClearHistory(canClearHistory);
            SummaryText = snapshot.SummaryText;
            EmptyMessage = snapshot.EmptyMessage;
            EmptyDetails = snapshot.EmptyDetails;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
            OnPropertyChanged(nameof(IsListVisible));
        }

        private void SetCanClearHistory(bool canClearHistory)
        {
            if (_canClearHistory == canClearHistory)
            {
                return;
            }

            _canClearHistory = canClearHistory;
            ClearHistoryCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile transfers command exception.");
        }

        private static string CreateExecutionStatusText(CottonQueuedUploadExecutionResult result)
        {
            return result.Status switch
            {
                CottonQueuedUploadExecutionStatus.NoQueuedUpload => "No waiting uploads.",
                CottonQueuedUploadExecutionStatus.Completed => $"Uploaded {result.Transfer?.DisplayName ?? "upload"}.",
                CottonQueuedUploadExecutionStatus.MissingDestination => result.FailureMessage ?? "Upload destination is missing.",
                CottonQueuedUploadExecutionStatus.MissingStagedFile => result.FailureMessage ?? "Upload file is no longer available.",
                CottonQueuedUploadExecutionStatus.Failed => result.FailureMessage ?? "Upload failed.",
                _ => "Transfer updated.",
            };
        }
    }
}
