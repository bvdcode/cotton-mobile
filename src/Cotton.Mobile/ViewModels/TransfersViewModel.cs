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
        private readonly ICottonAndroidBackgroundTransferCoordinator _backgroundTransferCoordinator;
        private readonly IAndroidApiLevelProvider _androidApiLevelProvider;
        private readonly ICottonQueuedUploadExecutor _queuedUploadExecutor;
        private readonly ILogger<TransfersViewModel> _logger;

        private bool _isBusy;
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
            ILogger<TransfersViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(metadataStore);
            ArgumentNullException.ThrowIfNull(backgroundTransferCoordinator);
            ArgumentNullException.ThrowIfNull(androidApiLevelProvider);
            ArgumentNullException.ThrowIfNull(queuedUploadExecutor);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _metadataStore = metadataStore;
            _backgroundTransferCoordinator = backgroundTransferCoordinator;
            _androidApiLevelProvider = androidApiLevelProvider;
            _queuedUploadExecutor = queuedUploadExecutor;
            _logger = logger;
            Items = [];
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            RunWaitingCommand = new AsyncCommand(RunWaitingAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public ObservableCollection<CottonTransferListItem> Items { get; }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand RunWaitingCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                    LoadCommand.RaiseCanExecuteChanged();
                    RunWaitingCommand.RaiseCanExecuteChanged();
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
                    ShowSnapshot(CottonTransferListSnapshot.Create(scheduledTransfers));
                    Status = scheduleResult.Status == CottonAndroidBackgroundTransferScheduleStatus.NoQueuedTransfer
                        ? "No waiting uploads."
                        : scheduleResult.StatusText;
                    return;
                }

                Status = "Running next waiting upload...";
                CottonQueuedUploadExecutionResult result =
                    await _queuedUploadExecutor.ExecuteNextAsync(_instanceUri);
                IReadOnlyList<CottonTransferQueueItem> transfers = await _metadataStore.LoadAsync(_instanceUri);
                ShowSnapshot(CottonTransferListSnapshot.Create(transfers));
                Status = CreateExecutionStatusText(result);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile queued upload execution failed.");
                Status = "Could not run waiting upload.";
                IReadOnlyList<CottonTransferQueueItem> transfers = await _metadataStore.LoadAsync(_instanceUri);
                ShowSnapshot(CottonTransferListSnapshot.Create(transfers));
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
