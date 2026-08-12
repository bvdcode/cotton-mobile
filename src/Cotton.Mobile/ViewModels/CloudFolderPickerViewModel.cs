// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Commands;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class CloudFolderPickerViewModel : ObservableObject
    {
        private readonly Uri _instanceUri;
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly Action<CottonUploadDestinationSnapshot?> _complete;
        private readonly ILogger<CloudFolderPickerViewModel> _logger;
        private readonly List<CottonFolderHandle> _path = [];

        private CottonFolderHandle? _currentFolder;
        private bool _didLoad;
        private bool _isBusy;
        private string _currentFolderName = AppResources.FilesTitle;
        private string _pathText = AppResources.FilesTitle;
        private string? _status;

        public CloudFolderPickerViewModel(
            Uri instanceUri,
            ICottonFileBrowserService fileBrowserService,
            Action<CottonUploadDestinationSnapshot?> complete,
            ILogger<CloudFolderPickerViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(complete);
            ArgumentNullException.ThrowIfNull(logger);

            _instanceUri = instanceUri;
            _fileBrowserService = fileBrowserService;
            _complete = complete;
            _logger = logger;

            LoadCommand = new AsyncRelayCommand(
                () => AsyncCommandExecution.RunAsync(LoadCurrentAsync, LogUnhandledCommandException),
                () => !IsBusy);
            UpCommand = new AsyncRelayCommand(
                () => AsyncCommandExecution.RunAsync(NavigateUpAsync, LogUnhandledCommandException),
                () => !IsBusy && CanNavigateUp);
            ChooseCommand = new AsyncRelayCommand(
                () => AsyncCommandExecution.RunAsync(ChooseCurrentAsync, LogUnhandledCommandException),
                () => !IsBusy && _currentFolder is not null);
            CancelCommand = new AsyncRelayCommand(
                () => AsyncCommandExecution.RunAsync(CancelAsync, LogUnhandledCommandException));
        }

        public RangeObservableCollection<CloudFolderItemViewModel> Folders { get; } = [];

        public IAsyncRelayCommand LoadCommand { get; }

        public IAsyncRelayCommand UpCommand { get; }

        public IAsyncRelayCommand ChooseCommand { get; }

        public IAsyncRelayCommand CancelCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsEmptyVisible));
                    LoadCommand.NotifyCanExecuteChanged();
                    UpCommand.NotifyCanExecuteChanged();
                    ChooseCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string CurrentFolderName
        {
            get => _currentFolderName;
            private set => SetProperty(ref _currentFolderName, value);
        }

        public string PathText
        {
            get => _pathText;
            private set => SetProperty(ref _pathText, value);
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

        public bool IsEmptyVisible => !IsBusy && Folders.Count == 0;

        public bool IsListVisible => Folders.Count > 0;

        public bool CanNavigateUp => _path.Count > 1;

        public async Task LoadOnceAsync()
        {
            if (_didLoad)
            {
                return;
            }

            _didLoad = true;
            await LoadRootAsync();
        }

        public void Cancel()
        {
            _complete(null);
        }

        private async Task LoadCurrentAsync()
        {
            if (_currentFolder is null || _path.Count <= 1)
            {
                await LoadRootAsync();
                return;
            }

            await LoadFolderAsync(_currentFolder, addToPath: false);
        }

        private async Task NavigateUpAsync()
        {
            if (!CanNavigateUp)
            {
                return;
            }

            _path.RemoveAt(_path.Count - 1);
            CottonFolderHandle parent = _path[^1];
            if (_path.Count == 1)
            {
                await LoadRootAsync();
            }
            else
            {
                await LoadFolderAsync(parent, addToPath: false);
            }
        }

        private Task OpenFolderAsync(CottonFolderHandle folder)
        {
            return LoadFolderAsync(folder, addToPath: true);
        }

        private async Task LoadRootAsync()
        {
            await LoadContentAsync(
                () => _fileBrowserService.GetRootAsync(_instanceUri),
                resetPath: true,
                addToPath: false);
        }

        private async Task LoadFolderAsync(CottonFolderHandle folder, bool addToPath)
        {
            await LoadContentAsync(
                () => _fileBrowserService.GetFolderAsync(_instanceUri, folder),
                resetPath: false,
                addToPath);
        }

        private async Task LoadContentAsync(
            Func<Task<CottonFolderContent>> loadAsync,
            bool resetPath,
            bool addToPath)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                CottonFolderContent content = await loadAsync();
                CottonFolderHandle folder = new(content.FolderId, content.FolderName);
                if (resetPath)
                {
                    _path.Clear();
                    _path.Add(folder);
                }
                else if (addToPath)
                {
                    _path.Add(folder);
                }
                else if (_path.Count > 0)
                {
                    _path[^1] = folder;
                }

                ShowContent(content, folder);
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to load Cotton cloud folders for sync setup.");
                Status = AppResources.CloudFoldersLoadFailed;
            }
            finally
            {
                IsBusy = false;
                RaiseFolderStateChanged();
            }
        }

        private void ShowContent(CottonFolderContent content, CottonFolderHandle folder)
        {
            _currentFolder = folder;
            CurrentFolderName = folder.Name;
            PathText = string.Join(" / ", _path.Select(pathFolder => pathFolder.Name));
            Folders.ReplaceWith(
                content.Entries
                    .Where(entry => entry.IsFolder)
                    .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(entry => entry.Id)
                    .Select(entry => new CloudFolderItemViewModel(
                        new CottonFolderHandle(entry.Id, entry.Name),
                        OpenFolderAsync,
                        LogUnhandledCommandException)));
        }

        private Task ChooseCurrentAsync()
        {
            if (_currentFolder is not null)
            {
                _complete(new CottonUploadDestinationSnapshot(
                    _currentFolder.Id,
                    _currentFolder.Name,
                    PathText));
            }

            return Task.CompletedTask;
        }

        private Task CancelAsync()
        {
            Cancel();
            return Task.CompletedTask;
        }

        private void RaiseFolderStateChanged()
        {
            OnPropertyChanged(nameof(IsEmptyVisible));
            OnPropertyChanged(nameof(IsListVisible));
            OnPropertyChanged(nameof(CanNavigateUp));
            UpCommand.NotifyCanExecuteChanged();
            ChooseCommand.NotifyCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled cloud folder picker command failure.");
            Status = AppResources.CloudFolderOpenFailed;
        }
    }
}
