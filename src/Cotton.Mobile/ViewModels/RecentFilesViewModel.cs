// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class RecentFilesViewModel : ViewModelBase
    {
        private readonly Uri _instanceUri;
        private readonly ICottonRecentFileStore _recentFileStore;
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly IFilePreviewService _filePreviewService;
        private readonly IFileInteractionService _fileInteractionService;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<RecentFilesViewModel> _logger;
        private bool _isLoadingPlaceholderEnabled;
        private bool _isBusy;
        private string _summaryText = "No recent files";
        private string _emptyMessage = "No recent files yet";
        private string _emptyDetails = "Open, download, or share files to build this list.";
        private string? _status;

        public RecentFilesViewModel(
            Uri instanceUri,
            ICottonRecentFileStore recentFileStore,
            ICottonFileBrowserService fileBrowserService,
            IFilePreviewService filePreviewService,
            IFileInteractionService fileInteractionService,
            IUserDialogService dialogService,
            ILogger<RecentFilesViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(recentFileStore);
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(filePreviewService);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _recentFileStore = recentFileStore;
            _fileBrowserService = fileBrowserService;
            _filePreviewService = filePreviewService;
            _fileInteractionService = fileInteractionService;
            _dialogService = dialogService;
            _logger = logger;
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
            ClearRecentFilesCommand = new AsyncCommand(
                ClearRecentFilesAsync,
                LogUnhandledCommandException,
                () => CanClearRecentFiles);
            OpenRecentFileCommand = new AsyncCommand<CottonRecentFileListItem>(
                OpenRecentFileAsync,
                LogUnhandledCommandException,
                _ => !IsBusy);
            RemoveRecentFileCommand = new AsyncCommand<CottonRecentFileListItem>(
                RemoveRecentFileAsync,
                LogUnhandledCommandException,
                _ => !IsBusy);
        }

        public RangeObservableCollection<CottonRecentFileListItem> Items { get; } = [];

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand ClearRecentFilesCommand { get; }

        public AsyncCommand<CottonRecentFileListItem> OpenRecentFileCommand { get; }

        public AsyncCommand<CottonRecentFileListItem> RemoveRecentFileCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoadCommand.RaiseCanExecuteChanged();
                    ClearRecentFilesCommand.RaiseCanExecuteChanged();
                    OpenRecentFileCommand.RaiseCanExecuteChanged();
                    RemoveRecentFileCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                    OnPropertyChanged(nameof(CanClearRecentFiles));
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

        public bool CanClearRecentFiles => Items.Count > 0 && !IsBusy;

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
                _isLoadingPlaceholderEnabled = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private async Task ClearRecentFilesAsync()
        {
            if (!CanClearRecentFiles)
            {
                return;
            }

            bool confirmed = await _dialogService.ShowConfirmationAsync(
                "Clear recent files?",
                "This removes the recent-file list on this device. Cached and offline files stay available.",
                "Clear",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            IsBusy = true;
            try
            {
                await _recentFileStore.ClearAsync(_instanceUri);
                ShowSnapshot(CottonRecentFileListSnapshot.Create([]));
                Status = "Recent files cleared.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile recent files clear failed.");
                Status = "Could not clear recent files.";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                OnPropertyChanged(nameof(IsListVisible));
                OnPropertyChanged(nameof(CanClearRecentFiles));
            }
        }

        private async Task OpenRecentFileAsync(CottonRecentFileListItem item)
        {
            if (IsBusy)
            {
                return;
            }

            CottonFileBrowserEntry file = CreateFileEntry(item);
            IsBusy = true;
            Status = null;
            try
            {
                CottonFileDownloadResult localOrDownloadedFile = await PrepareFileForOpenAsync(file);
                if (_filePreviewService.CanPreview(file, localOrDownloadedFile))
                {
                    await _filePreviewService.OpenAsync(file, localOrDownloadedFile);
                }
                else
                {
                    await _fileInteractionService.OpenAsync(localOrDownloadedFile);
                }

                await _recentFileStore.RecordAsync(
                    _instanceUri,
                    CottonRecentFileSnapshot.Create(file, CottonRecentFileActionKind.Opened, DateTime.UtcNow));
                IReadOnlyList<CottonRecentFileSnapshot> recentFiles = await _recentFileStore.LoadAsync(_instanceUri);
                ShowSnapshot(CottonRecentFileListSnapshot.Create(recentFiles));
                Status = null;
            }
            catch (FileOpenUnavailableException exception)
            {
                _logger.LogWarning(exception, "Cotton mobile recent file open unavailable {FileId}.", file.Id);
                Status = CottonFileOpenRouter.CreateRoute(file, item.SizeBytes).UnavailableStatus;
            }
            catch (FileNotFoundException exception)
            {
                _logger.LogWarning(exception, "Cotton mobile recent file local copy missing {FileId}.", file.Id);
                Status = "File no longer available on this device.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile recent file open failed {FileId}.", file.Id);
                Status = "Could not open recent file.";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                OnPropertyChanged(nameof(IsListVisible));
                OnPropertyChanged(nameof(CanClearRecentFiles));
            }
        }

        private async Task RemoveRecentFileAsync(CottonRecentFileListItem item)
        {
            if (IsBusy || item is null)
            {
                return;
            }

            IsBusy = true;
            Status = CottonRecentFileRemoveStatusText.CreateRemovingStatus(item.FileName);
            try
            {
                bool removed = await _recentFileStore.RemoveAsync(_instanceUri, item.FileId);
                IReadOnlyList<CottonRecentFileSnapshot> recentFiles = await _recentFileStore.LoadAsync(_instanceUri);
                ShowSnapshot(CottonRecentFileListSnapshot.Create(recentFiles));
                Status = removed
                    ? CottonRecentFileRemoveStatusText.CreateRemovedStatus(item.FileName)
                    : CottonRecentFileRemoveStatusText.AlreadyRemovedStatus;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile recent file remove failed {FileId}.", item.FileId);
                Status = CottonRecentFileRemoveStatusText.FailedStatus;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
                OnPropertyChanged(nameof(IsListVisible));
                OnPropertyChanged(nameof(CanClearRecentFiles));
            }
        }

        private async Task<CottonFileDownloadResult> PrepareFileForOpenAsync(CottonFileBrowserEntry file)
        {
            CottonFileDownloadResult? localFile = _fileBrowserService.GetReusableLocalDownload(_instanceUri, file);
            if (localFile is not null)
            {
                return localFile;
            }

            Status = $"Downloading {file.Name}...";
            return await _fileBrowserService.DownloadAsync(_instanceUri, file);
        }

        private void ShowSnapshot(CottonRecentFileListSnapshot snapshot)
        {
            Items.ReplaceWith(snapshot.Items);

            SummaryText = snapshot.SummaryText;
            EmptyMessage = snapshot.EmptyMessage;
            EmptyDetails = snapshot.EmptyDetails;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
            OnPropertyChanged(nameof(IsListVisible));
            OnPropertyChanged(nameof(CanClearRecentFiles));
            ClearRecentFilesCommand.RaiseCanExecuteChanged();
            RemoveRecentFileCommand.RaiseCanExecuteChanged();
        }

        private static CottonFileBrowserEntry CreateFileEntry(CottonRecentFileListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            string details = item.SizeBytes.HasValue
                ? $"{CottonFileSizeFormatter.Format(item.SizeBytes.Value)} · {item.Kind}"
                : item.Kind;
            return CottonFileBrowserEntry.CreateCached(
                item.FileId,
                CottonFileBrowserEntryType.File,
                item.FileName,
                item.Kind,
                details,
                CottonFileOpenRouter.OpenActionLabel,
                item.BadgeText,
                item.RemoteUpdatedAtUtc,
                item.SizeBytes,
                item.ContentType,
                previewHashEncryptedHex: null,
                eTag: null);
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile recent files command exception.");
        }
    }
}
