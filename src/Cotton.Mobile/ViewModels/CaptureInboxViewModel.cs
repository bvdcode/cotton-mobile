// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

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
        private readonly ICottonShareTransferEnqueueCoordinator _enqueueCoordinator;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<CaptureInboxViewModel> _logger;

        private bool _isLoadingPlaceholderEnabled;
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
            ICottonShareTransferEnqueueCoordinator enqueueCoordinator,
            IUserDialogService dialogService,
            ILogger<CaptureInboxViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(intakeStore);
            ArgumentNullException.ThrowIfNull(contentStagingStore);
            ArgumentNullException.ThrowIfNull(destinationPickerPageService);
            ArgumentNullException.ThrowIfNull(enqueueCoordinator);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _intakeStore = intakeStore;
            _contentStagingStore = contentStagingStore;
            _destinationPickerPageService = destinationPickerPageService;
            _enqueueCoordinator = enqueueCoordinator;
            _dialogService = dialogService;
            _logger = logger;
            Items = [];
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            DestinationCommand = new AsyncCommand(
                OpenDestinationPickerAsync,
                LogUnhandledCommandException,
                () => !IsBusy && Items.Any(item => item.CanSelectDestination));
            RenameCommand = new AsyncCommand(
                RenameCapturedFileAsync,
                LogUnhandledCommandException,
                () => !IsBusy && Items.Any(item => item.CanRename));
            EnqueueCommand = new AsyncCommand(
                EnqueueCapturedFilesAsync,
                LogUnhandledCommandException,
                () => !IsBusy && Items.Any(item => item.CanEnqueue));
            ClearCommand = new AsyncCommand(ClearAsync, LogUnhandledCommandException, () => !IsBusy && Items.Count > 0);
        }

        public RangeObservableCollection<CottonCaptureInboxListItem> Items { get; }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand DestinationCommand { get; }

        public AsyncCommand RenameCommand { get; }

        public AsyncCommand EnqueueCommand { get; }

        public AsyncCommand ClearCommand { get; }

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
                    DestinationCommand.RaiseCanExecuteChanged();
                    RenameCommand.RaiseCanExecuteChanged();
                    EnqueueCommand.RaiseCanExecuteChanged();
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

        public bool IsHeaderSummaryVisible => Items.Count > 0;

        public bool IsLoadingPlaceholderVisible => _isLoadingPlaceholderEnabled && IsBusy && Items.Count == 0;

        public bool IsListVisible => Items.Count > 0;

        public bool CanChooseDestination => Items.Any(item => item.CanSelectDestination);

        public bool CanRenameCapturedFiles => Items.Any(item => item.CanRename);

        public bool CanEnqueueCapturedFiles => Items.Any(item => item.CanEnqueue);

        public bool IsActionBarVisible => CanChooseDestination || CanRenameCapturedFiles || CanEnqueueCapturedFiles;

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
                _isLoadingPlaceholderEnabled = false;
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

        private async Task RenameCapturedFileAsync()
        {
            if (IsBusy || !CanRenameCapturedFiles)
            {
                return;
            }

            CottonCaptureInboxListItem? selectedItem = await SelectRenameItemAsync();
            if (selectedItem is null)
            {
                return;
            }

            string? requestedName = await _dialogService.ShowPromptAsync(
                "Rename captured file",
                "Choose the upload file name.",
                "Save",
                "Cancel",
                selectedItem.DisplayName,
                maxLength: 160);
            if (requestedName is null)
            {
                return;
            }

            IsBusy = true;
            try
            {
                IReadOnlyList<CottonShareIntakeSnapshot> snapshots = await _intakeStore.LoadAsync();
                CottonShareIntakeSnapshot? targetSnapshot = snapshots
                    .FirstOrDefault(snapshot => snapshot.Id == selectedItem.IntakeId);
                CottonShareIntakeItemSnapshot? targetItem = targetSnapshot?.Items
                    .FirstOrDefault(item => item.Id == selectedItem.ItemId);
                if (targetSnapshot is null || targetItem is null || !CanRename(targetSnapshot, targetItem))
                {
                    Status = "Captured file is no longer available.";
                    ShowSnapshot(CottonCaptureInboxListSnapshot.Create(snapshots));
                    return;
                }

                IReadOnlyList<string> otherUploadNames = snapshots
                    .SelectMany(snapshot => snapshot.Items.Select(item => new { Snapshot = snapshot, Item = item }))
                    .Where(entry => entry.Item.Id != selectedItem.ItemId && CanRename(entry.Snapshot, entry.Item))
                    .Select(entry => CottonShareCaptureUploadName.Create(entry.Item))
                    .ToList();
                if (!CottonShareCaptureUploadName.TryNormalizeRename(
                        targetItem,
                        requestedName,
                        otherUploadNames,
                        out string normalizedName,
                        out string uploadName,
                        out string errorMessage))
                {
                    Status = errorMessage;
                    return;
                }

                if (string.Equals(
                        uploadName,
                        CottonShareCaptureUploadName.Create(targetItem),
                        StringComparison.Ordinal))
                {
                    Status = "Upload name unchanged.";
                    return;
                }

                List<CottonShareIntakeSnapshot> updatedSnapshots = snapshots
                    .Select(snapshot => snapshot.Id == selectedItem.IntakeId
                        ? ReplaceItem(snapshot, targetItem.WithUploadDisplayName(normalizedName))
                        : snapshot)
                    .ToList();
                await _intakeStore.SaveAsync(updatedSnapshots);
                ShowSnapshot(CottonCaptureInboxListSnapshot.Create(updatedSnapshots));
                Status = $"Renamed to {uploadName}.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile capture inbox rename failed.");
                Status = "Could not rename captured file.";
            }
            finally
            {
                IsBusy = false;
                RaiseListStateChanged();
            }
        }

        private async Task EnqueueCapturedFilesAsync()
        {
            if (IsBusy || !CanEnqueueCapturedFiles)
            {
                return;
            }

            IsBusy = true;
            try
            {
                CottonShareTransferEnqueueResult result =
                    await _enqueueCoordinator.EnqueueAsync(_instanceUri);
                IReadOnlyList<CottonShareIntakeSnapshot> snapshots = await _intakeStore.LoadAsync();
                ShowSnapshot(CottonCaptureInboxListSnapshot.Create(snapshots));
                Status = result.HasQueuedTransfers
                    ? result.StatusText
                    : "Choose a destination before queueing uploads.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile capture inbox enqueue failed.");
                Status = "Could not queue captured files.";
            }
            finally
            {
                IsBusy = false;
                RaiseListStateChanged();
            }
        }

        private async Task<CottonCaptureInboxListItem?> SelectRenameItemAsync()
        {
            List<CottonCaptureInboxListItem> renameItems = Items
                .Where(item => item.CanRename)
                .ToList();
            if (renameItems.Count == 0)
            {
                return null;
            }

            if (renameItems.Count == 1)
            {
                return renameItems[0];
            }

            var actionMap = new Dictionary<string, CottonCaptureInboxListItem>(StringComparer.Ordinal);
            for (int index = 0; index < renameItems.Count; index++)
            {
                string action = CreateRenameActionLabel(renameItems[index], index);
                actionMap[action] = renameItems[index];
            }

            string? selectedAction = await _dialogService.ShowActionSheetAsync(
                "Rename captured file",
                "Cancel",
                null,
                actionMap.Keys.ToArray());
            return selectedAction is not null && actionMap.TryGetValue(selectedAction, out CottonCaptureInboxListItem? item)
                ? item
                : null;
        }

        private static string CreateRenameActionLabel(CottonCaptureInboxListItem item, int index)
        {
            string suffix = item.ItemId.ToString("N")[..6];
            return $"{index + 1}. {item.DisplayName} · {suffix}";
        }

        private static bool CanRename(
            CottonShareIntakeSnapshot snapshot,
            CottonShareIntakeItemSnapshot item)
        {
            return snapshot.Status == CottonShareIntakeStatus.Pending
                && item.CanUploadFromCaptureInbox;
        }

        private static CottonShareIntakeSnapshot ReplaceItem(
            CottonShareIntakeSnapshot snapshot,
            CottonShareIntakeItemSnapshot updatedItem)
        {
            return new CottonShareIntakeSnapshot(
                snapshot.Id,
                snapshot.Kind,
                snapshot.Status,
                snapshot.SourceMimeType,
                snapshot.Items
                    .Select(item => item.Id == updatedItem.Id ? updatedItem : item)
                    .ToList(),
                snapshot.FailureMessage,
                snapshot.ReceivedAtUtc,
                snapshot.Destination);
        }

        private void ShowSnapshot(CottonCaptureInboxListSnapshot snapshot)
        {
            Items.ReplaceWith(snapshot.Items);

            SummaryText = snapshot.SummaryText;
            EmptyMessage = snapshot.EmptyMessage;
            EmptyDetails = snapshot.EmptyDetails;
            RaiseListStateChanged();
        }

        private void RaiseListStateChanged()
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsHeaderSummaryVisible));
            OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
            OnPropertyChanged(nameof(IsListVisible));
            OnPropertyChanged(nameof(CanChooseDestination));
            OnPropertyChanged(nameof(CanRenameCapturedFiles));
            OnPropertyChanged(nameof(CanEnqueueCapturedFiles));
            OnPropertyChanged(nameof(IsActionBarVisible));
            DestinationCommand.RaiseCanExecuteChanged();
            RenameCommand.RaiseCanExecuteChanged();
            EnqueueCommand.RaiseCanExecuteChanged();
            ClearCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile capture inbox command exception.");
        }
    }
}
