// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class FileVersionHistoryViewModel : ViewModelBase
    {
        private readonly Uri _instanceUri;
        private readonly CottonFileBrowserEntry _file;
        private readonly ICottonFileVersionHistoryService _versionHistoryService;
        private readonly INetworkAccessService _networkAccess;
        private readonly ILogger<FileVersionHistoryViewModel> _logger;
        private bool _isBusy;
        private string _summaryText;
        private string _emptyMessage = "No versions found.";
        private string _emptyDetails = "This file does not have older versions.";
        private string? _status;

        public FileVersionHistoryViewModel(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            ICottonFileVersionHistoryService versionHistoryService,
            INetworkAccessService networkAccess,
            ILogger<FileVersionHistoryViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(versionHistoryService);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _file = file;
            _versionHistoryService = versionHistoryService;
            _networkAccess = networkAccess;
            _logger = logger;
            _summaryText = CottonFileVersionStatusText.CreateLoadingStatus(_file.Name);
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public Guid FileId => _file.Id;

        public string FileName => _file.Name;

        public ObservableCollection<CottonFileVersionItemSnapshot> Items { get; } = [];

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
            Status = CottonFileVersionStatusText.CreateLoadingStatus(_file.Name);
            try
            {
                if (!_networkAccess.HasInternetAccess)
                {
                    ShowOfflineState();
                    return;
                }

                CottonFileVersionListSnapshot snapshot = await _versionHistoryService.GetVersionsAsync(
                    _instanceUri,
                    _file,
                    TimeZoneInfo.Local);
                ShowSnapshot(snapshot);
                Status = CottonFileVersionStatusText.CreateLoadedStatus(snapshot.Items.Count);
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogInformation(exception, "Cotton mobile file version-history load cancelled {FileId}.", FileId);
                Status = CottonFileVersionStatusText.CancelledStatus;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile file version-history load failed {FileId}.", FileId);
                Status = CottonFileVersionStatusText.FailedStatus;
                EmptyMessage = "Could not load versions.";
                EmptyDetails = "Refresh to try again.";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsListVisible));
            }
        }

        private void ShowSnapshot(CottonFileVersionListSnapshot snapshot)
        {
            Items.Clear();
            foreach (CottonFileVersionItemSnapshot item in snapshot.Items)
            {
                Items.Add(item);
            }

            SummaryText = snapshot.SummaryText;
            EmptyMessage = snapshot.EmptyText;
            EmptyDetails = snapshot.HasItems
                ? string.Empty
                : "Refresh after this file changes.";
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsListVisible));
        }

        private void ShowOfflineState()
        {
            Items.Clear();
            SummaryText = "Version history needs internet.";
            EmptyMessage = "Version history unavailable offline.";
            EmptyDetails = "Connect and refresh to view versions.";
            Status = CottonFileVersionStatusText.OfflineUnavailableStatus;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsListVisible));
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile file version-history command exception.");
        }
    }
}
