// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Collections.ObjectModel;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class CaptureDestinationPickerViewModel : ViewModelBase
    {
        private readonly Uri _instanceUri;
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly ILogger<CaptureDestinationPickerViewModel> _logger;
        private readonly Func<CottonUploadDestinationSnapshot, Task<bool>> _chooseDestinationAsync;
        private readonly string _emptySelectionStatus;
        private readonly Func<CottonUploadDestinationSnapshot, string> _successStatusFactory;
        private readonly List<CottonFolderHandle> _path = [];

        private bool _isLoadingPlaceholderEnabled;
        private bool _isBusy;
        private string _currentFolderName = "Files";
        private string _pathText = "Files";
        private string _summaryText = "Loading folders...";
        private string? _status;
        private CottonFolderHandle? _currentFolder;

        public CaptureDestinationPickerViewModel(
            Uri instanceUri,
            ICottonFileBrowserService fileBrowserService,
            ICottonShareIntakeStore intakeStore,
            ILogger<CaptureDestinationPickerViewModel> logger)
            : this(
                instanceUri,
                fileBrowserService,
                logger,
                destination => SaveCaptureDestinationAsync(intakeStore, destination),
                "No staged files need a destination.",
                destination => $"Destination set to {destination.Path}.")
        {
            ArgumentNullException.ThrowIfNull(intakeStore);
        }

        public CaptureDestinationPickerViewModel(
            Uri instanceUri,
            ICottonFileBrowserService fileBrowserService,
            ILogger<CaptureDestinationPickerViewModel> logger,
            Func<CottonUploadDestinationSnapshot, Task<bool>> chooseDestinationAsync,
            string emptySelectionStatus,
            Func<CottonUploadDestinationSnapshot, string> successStatusFactory)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(chooseDestinationAsync);
            ArgumentException.ThrowIfNullOrWhiteSpace(emptySelectionStatus);
            ArgumentNullException.ThrowIfNull(successStatusFactory);

            _instanceUri = instanceUri;
            _fileBrowserService = fileBrowserService;
            _logger = logger;
            _chooseDestinationAsync = chooseDestinationAsync;
            _emptySelectionStatus = emptySelectionStatus.Trim();
            _successStatusFactory = successStatusFactory;
            Folders = [];
            LoadCommand = new AsyncCommand(LoadCurrentAsync, LogUnhandledCommandException, () => !IsBusy);
            UpCommand = new AsyncCommand(NavigateUpAsync, LogUnhandledCommandException, () => !IsBusy && CanNavigateUp);
            ChooseCommand = new AsyncCommand(ChooseCurrentAsync, LogUnhandledCommandException, () => !IsBusy && _currentFolder is not null);
        }

        public ObservableCollection<CaptureDestinationFolderItemViewModel> Folders { get; }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand UpCommand { get; }

        public AsyncCommand ChooseCommand { get; }

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
                    UpCommand.RaiseCanExecuteChanged();
                    ChooseCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string CurrentFolderName
        {
            get => _currentFolderName;
            private set
            {
                if (SetProperty(ref _currentFolderName, value))
                {
                    OnPropertyChanged(nameof(IsPathTextVisible));
                }
            }
        }

        public string PathText
        {
            get => _pathText;
            private set
            {
                if (SetProperty(ref _pathText, value))
                {
                    OnPropertyChanged(nameof(IsPathTextVisible));
                }
            }
        }

        public string SummaryText
        {
            get => _summaryText;
            private set => SetProperty(ref _summaryText, value);
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

        public bool IsPathTextVisible =>
            !string.Equals(CurrentFolderName, PathText, StringComparison.OrdinalIgnoreCase);

        public bool CanNavigateUp => _path.Count > 1;

        public bool IsEmpty => Folders.Count == 0 && !IsBusy;

        public bool IsLoadingPlaceholderVisible => _isLoadingPlaceholderEnabled && IsBusy && Folders.Count == 0;

        public bool IsListVisible => Folders.Count > 0;

        private async Task LoadCurrentAsync()
        {
            if (IsBusy)
            {
                return;
            }

            if (_currentFolder is null)
            {
                await LoadRootAsync();
                return;
            }

            if (_path.Count <= 1)
            {
                await LoadRootAsync();
                return;
            }

            await LoadFolderAsync(_currentFolder, preserveExistingPath: true);
        }

        private async Task NavigateUpAsync()
        {
            if (IsBusy || !CanNavigateUp)
            {
                return;
            }

            _path.RemoveAt(_path.Count - 1);
            CottonFolderHandle folder = _path[^1];
            if (_path.Count == 1)
            {
                await LoadRootAsync();
            }
            else
            {
                await LoadFolderAsync(folder, preserveExistingPath: true);
            }
        }

        private async Task OpenFolderAsync(CottonFolderHandle folder)
        {
            if (IsBusy)
            {
                return;
            }

            await LoadFolderAsync(folder, preserveExistingPath: false);
        }

        private async Task LoadRootAsync()
        {
            if (IsBusy)
            {
                return;
            }

            _isLoadingPlaceholderEnabled = Folders.Count == 0;
            IsBusy = true;
            try
            {
                SummaryText = "Loading folders...";
                CottonFolderContent content = await _fileBrowserService.GetRootAsync(_instanceUri);
                var folder = new CottonFolderHandle(content.FolderId, content.FolderName);
                _path.Clear();
                _path.Add(folder);
                ShowContent(content);
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile capture destination root load failed.");
                Status = "Could not load folders.";
            }
            finally
            {
                IsBusy = false;
                _isLoadingPlaceholderEnabled = false;
                RaiseFolderStateChanged();
            }
        }

        private async Task LoadFolderAsync(CottonFolderHandle folder, bool preserveExistingPath)
        {
            _isLoadingPlaceholderEnabled = Folders.Count == 0;
            IsBusy = true;
            try
            {
                SummaryText = $"Loading {folder.Name}...";
                CottonFolderContent content = await _fileBrowserService.GetFolderAsync(_instanceUri, folder);
                var loadedFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
                if (preserveExistingPath)
                {
                    ReplaceCurrentPathFolder(loadedFolder);
                }
                else
                {
                    _path.Add(loadedFolder);
                }

                ShowContent(content);
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile capture destination folder load failed.");
                Status = "Could not load this folder.";
            }
            finally
            {
                IsBusy = false;
                _isLoadingPlaceholderEnabled = false;
                RaiseFolderStateChanged();
            }
        }

        private async Task ChooseCurrentAsync()
        {
            if (IsBusy || _currentFolder is null)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var destination = new CottonUploadDestinationSnapshot(
                    _currentFolder.Id,
                    _currentFolder.Name,
                    PathText);
                bool didChoose = await _chooseDestinationAsync(destination);
                if (!didChoose)
                {
                    Status = _emptySelectionStatus;
                    return;
                }

                Status = _successStatusFactory(destination);
                await Shell.Current.Navigation.PopAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Cotton mobile capture destination save failed.");
                Status = "Could not save destination.";
            }
            finally
            {
                IsBusy = false;
                RaiseFolderStateChanged();
            }
        }

        private void ShowContent(CottonFolderContent content)
        {
            _currentFolder = new CottonFolderHandle(content.FolderId, content.FolderName);
            CurrentFolderName = _currentFolder.Name;
            PathText = CreatePathText();
            Folders.Clear();

            foreach (CottonFileBrowserEntry folder in content.Entries
                         .Where(entry => entry.Type == CottonFileBrowserEntryType.Folder)
                         .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(entry => entry.Id))
            {
                Folders.Add(
                    new CaptureDestinationFolderItemViewModel(
                        new CottonFolderHandle(folder.Id, folder.Name),
                        OpenFolderAsync,
                        LogUnhandledCommandException));
            }

            SummaryText = Folders.Count == 1 ? "1 folder" : $"{Folders.Count:N0} folders";
            RaiseFolderStateChanged();
        }

        private void ReplaceCurrentPathFolder(CottonFolderHandle folder)
        {
            if (_path.Count == 0)
            {
                _path.Add(folder);
                return;
            }

            _path[^1] = folder;
        }

        private string CreatePathText()
        {
            if (_path.Count == 0)
            {
                return "Files";
            }

            return string.Join(" / ", _path.Select(folder => folder.Name));
        }

        private void RaiseFolderStateChanged()
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsLoadingPlaceholderVisible));
            OnPropertyChanged(nameof(IsListVisible));
            OnPropertyChanged(nameof(CanNavigateUp));
            UpCommand.RaiseCanExecuteChanged();
            ChooseCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile capture destination command exception.");
        }

        private static async Task<bool> SaveCaptureDestinationAsync(
            ICottonShareIntakeStore intakeStore,
            CottonUploadDestinationSnapshot uploadDestination)
        {
            ArgumentNullException.ThrowIfNull(intakeStore);
            ArgumentNullException.ThrowIfNull(uploadDestination);

            var destination = new CottonShareDestinationSnapshot(
                uploadDestination.FolderId,
                uploadDestination.FolderName,
                uploadDestination.Path);
            IReadOnlyList<CottonShareIntakeSnapshot> snapshots = await intakeStore.LoadAsync();
            List<CottonShareIntakeSnapshot> updatedSnapshots = snapshots
                .Select(snapshot => snapshot.CanSelectCaptureDestination
                    ? snapshot.WithDestination(destination)
                    : snapshot)
                .ToList();
            int updatedCount = updatedSnapshots.Count(snapshot =>
                snapshot.Destination?.FolderId == destination.FolderId
                && snapshot.CanSelectCaptureDestination);
            if (updatedCount == 0)
            {
                return false;
            }

            await intakeStore.SaveAsync(updatedSnapshots);
            return true;
        }
    }
}
