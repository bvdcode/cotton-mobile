namespace Cotton.Mobile.ViewModels
{
    using System.Collections.ObjectModel;
    using Cotton.Mobile.Services;

    public class MainPageDisplayState : ViewModelBase
    {
        private readonly List<CottonFileBrowserEntry> _allFileEntries = [];

        private MainPageViewState _state = MainPageViewState.SignIn;
        private string _instanceUrl = string.Empty;
        private string _loadingMessage = "Restoring session...";
        private string? _status;
        private string _authorizationProgressMessage = "Approve the request in your browser, then return to Cotton Cloud.";
        private string _profileName = string.Empty;
        private string _profileEmail = string.Empty;
        private string _profileInstance = string.Empty;
        private string? _profileStatus;
        private string _filesTitle = "Files";
        private string _filesPath = string.Empty;
        private string? _filesStatus;
        private string _filesEmptyMessage = "No files in this folder.";
        private string _fileSearchText = string.Empty;
        private CottonFileBrowserViewMode _fileViewMode = CottonFileBrowserViewMode.List;
        private CottonFileBrowserSortMode _fileSortMode = CottonFileBrowserSortMode.Name;
        private bool _isInputEnabled = true;
        private bool _isCancelAuthorizationEnabled = true;
        private bool _isLogoutEnabled = true;
        private bool _isFilesLoading;
        private bool _isFilesRefreshing;
        private bool _canNavigateFilesUp;
        private bool _canCancelFileAction;
        private bool _canRetryFileAction;

        public MainPageDisplayState(string defaultInstanceUrl)
        {
            if (string.IsNullOrWhiteSpace(defaultInstanceUrl))
            {
                throw new ArgumentException("Default instance URL is required.", nameof(defaultInstanceUrl));
            }

            InstanceUrl = defaultInstanceUrl;
        }

        public string InstanceUrl
        {
            get => _instanceUrl;
            set => SetProperty(ref _instanceUrl, value);
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            private set => SetProperty(ref _loadingMessage, value);
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

        public string AuthorizationProgressMessage
        {
            get => _authorizationProgressMessage;
            private set => SetProperty(ref _authorizationProgressMessage, value);
        }

        public string ProfileName
        {
            get => _profileName;
            private set => SetProperty(ref _profileName, value);
        }

        public string ProfileEmail
        {
            get => _profileEmail;
            private set
            {
                if (SetProperty(ref _profileEmail, value))
                {
                    OnPropertyChanged(nameof(ProfileSummary));
                }
            }
        }

        public string ProfileInstance
        {
            get => _profileInstance;
            private set
            {
                if (SetProperty(ref _profileInstance, value))
                {
                    OnPropertyChanged(nameof(ProfileSummary));
                }
            }
        }

        public string ProfileSummary => $"{ProfileEmail} · {ProfileInstance}";

        public string? ProfileStatus
        {
            get => _profileStatus;
            private set
            {
                if (SetProperty(ref _profileStatus, value))
                {
                    OnPropertyChanged(nameof(IsProfileStatusVisible));
                }
            }
        }

        public bool IsProfileStatusVisible => !string.IsNullOrWhiteSpace(ProfileStatus);

        public string FilesTitle
        {
            get => _filesTitle;
            private set => SetProperty(ref _filesTitle, value);
        }

        public string? FilesStatus
        {
            get => _filesStatus;
            private set
            {
                if (SetProperty(ref _filesStatus, value))
                {
                    OnPropertyChanged(nameof(IsFilesStatusVisible));
                }
            }
        }

        public bool IsFilesStatusVisible => !string.IsNullOrWhiteSpace(FilesStatus);

        public string FilesPath
        {
            get => _filesPath;
            private set
            {
                if (SetProperty(ref _filesPath, value))
                {
                    OnPropertyChanged(nameof(IsFilesPathVisible));
                }
            }
        }

        public bool IsFilesPathVisible => !string.IsNullOrWhiteSpace(FilesPath);

        public ObservableCollection<CottonFileBrowserEntry> FileEntries { get; } = [];

        public string FilesEmptyMessage
        {
            get => _filesEmptyMessage;
            private set => SetProperty(ref _filesEmptyMessage, value);
        }

        public string FileSearchText
        {
            get => _fileSearchText;
            set
            {
                if (SetProperty(ref _fileSearchText, value ?? string.Empty))
                {
                    ApplyFileFilters();
                }
            }
        }

        public CottonFileBrowserViewMode FileViewMode
        {
            get => _fileViewMode;
            private set
            {
                if (SetProperty(ref _fileViewMode, value))
                {
                    OnPropertyChanged(nameof(IsFileListViewVisible));
                    OnPropertyChanged(nameof(IsFileTileViewVisible));
                    OnPropertyChanged(nameof(FileViewToggleText));
                }
            }
        }

        public CottonFileBrowserSortMode FileSortMode
        {
            get => _fileSortMode;
            private set
            {
                if (SetProperty(ref _fileSortMode, value))
                {
                    OnPropertyChanged(nameof(CanSortByName));
                    OnPropertyChanged(nameof(CanSortByType));
                    OnPropertyChanged(nameof(CanSortBySize));
                }
            }
        }

        public bool IsFileListViewVisible => FileViewMode == CottonFileBrowserViewMode.List;

        public bool IsFileTileViewVisible => FileViewMode == CottonFileBrowserViewMode.Tiles;

        public string FileViewToggleText => FileViewMode == CottonFileBrowserViewMode.List ? "Tiles" : "List";

        public bool CanSortByName => FileSortMode != CottonFileBrowserSortMode.Name;

        public bool CanSortByType => FileSortMode != CottonFileBrowserSortMode.Type;

        public bool CanSortBySize => FileSortMode != CottonFileBrowserSortMode.Size;

        public bool IsFilesLoading
        {
            get => _isFilesLoading;
            private set => SetProperty(ref _isFilesLoading, value);
        }

        public bool IsFilesRefreshing
        {
            get => _isFilesRefreshing;
            set => SetProperty(ref _isFilesRefreshing, value);
        }

        public bool CanNavigateFilesUp
        {
            get => _canNavigateFilesUp;
            private set => SetProperty(ref _canNavigateFilesUp, value);
        }

        public bool CanCancelFileAction
        {
            get => _canCancelFileAction;
            private set => SetProperty(ref _canCancelFileAction, value);
        }

        public bool CanRetryFileAction
        {
            get => _canRetryFileAction;
            private set => SetProperty(ref _canRetryFileAction, value);
        }

        public bool IsFilesEmptyVisible => !IsFilesLoading && FileEntries.Count == 0;

        public bool IsInputEnabled
        {
            get => _isInputEnabled;
            private set => SetProperty(ref _isInputEnabled, value);
        }

        public bool IsCancelAuthorizationEnabled
        {
            get => _isCancelAuthorizationEnabled;
            private set => SetProperty(ref _isCancelAuthorizationEnabled, value);
        }

        public bool IsLogoutEnabled
        {
            get => _isLogoutEnabled;
            private set => SetProperty(ref _isLogoutEnabled, value);
        }

        public bool IsLoadingVisible => _state == MainPageViewState.Loading;

        public bool IsSignInVisible => _state == MainPageViewState.SignIn;

        public bool IsAuthorizationProgressVisible => _state == MainPageViewState.AuthorizationProgress;

        public bool IsProfileVisible => _state == MainPageViewState.Profile;

        public bool IsBrandHeaderVisible => _state != MainPageViewState.Profile;

        public bool IsLoadingIndicatorRunning => _state == MainPageViewState.Loading;

        public bool IsAuthorizationProgressIndicatorRunning => _state == MainPageViewState.AuthorizationProgress;

        public void ShowLoading(string message)
        {
            SetState(MainPageViewState.Loading);
            LoadingMessage = message;
            IsInputEnabled = false;
            ProfileStatus = null;
        }

        public void ShowSignIn(string? status)
        {
            SetState(MainPageViewState.SignIn);
            IsInputEnabled = true;
            SetStatus(status);
        }

        public void ShowAuthorizationProgress(Uri instanceUri)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            SetState(MainPageViewState.AuthorizationProgress);
            IsCancelAuthorizationEnabled = true;
            AuthorizationProgressMessage = $"Approve the request for {instanceUri.Host}, then return to Cotton Cloud.";
            IsInputEnabled = false;
        }

        public void ShowAuthorizationCancelling()
        {
            IsCancelAuthorizationEnabled = false;
            AuthorizationProgressMessage = "Cancelling authorization...";
        }

        public void ShowProfile(MainPageProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            SetState(MainPageViewState.Profile);
            ProfileName = profile.Name;
            ProfileEmail = profile.Email;
            ProfileInstance = profile.Instance;
            ProfileStatus = null;
            FilesTitle = "Files";
            FilesPath = string.Empty;
            FilesStatus = "Loading files...";
            IsFilesLoading = true;
            IsFilesRefreshing = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            CanNavigateFilesUp = false;
            _allFileEntries.Clear();
            FileEntries.Clear();
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
            IsLogoutEnabled = true;
            IsInputEnabled = false;
        }

        public void ShowProfileError(string status)
        {
            SetState(MainPageViewState.Profile);
            ProfileStatus = status;
            IsLogoutEnabled = true;
        }

        public void ShowFilesLoading(string status)
        {
            IsFilesLoading = true;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            FilesStatus = status;
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ShowFileActionLoading(string status)
        {
            IsFilesLoading = true;
            IsFilesRefreshing = false;
            CanCancelFileAction = true;
            CanRetryFileAction = false;
            FilesStatus = status;
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ShowFileActionCancelling(string status)
        {
            CanCancelFileAction = false;
            FilesStatus = status;
        }

        public void ShowFileActionRetry(string status)
        {
            IsFilesLoading = false;
            IsFilesRefreshing = false;
            CanCancelFileAction = false;
            CanRetryFileAction = true;
            FilesStatus = status;
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ClearFileActionRetry()
        {
            CanRetryFileAction = false;
        }

        public void ShowFilesRefreshing(string status)
        {
            IsFilesLoading = false;
            IsFilesRefreshing = true;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            FilesStatus = status;
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ShowFiles(CottonFolderContent content, bool canNavigateUp, string path)
        {
            ArgumentNullException.ThrowIfNull(content);

            FilesTitle = content.FolderName;
            FilesPath = path;
            _allFileEntries.Clear();
            foreach (CottonFileBrowserEntry entry in content.Entries)
            {
                _allFileEntries.Add(entry);
            }

            IsFilesLoading = false;
            IsFilesRefreshing = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            CanNavigateFilesUp = canNavigateUp;
            ApplyFileFilters();
            FilesStatus = CreateFilesStatus();
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ShowFilesStatus(string status)
        {
            IsFilesLoading = false;
            IsFilesRefreshing = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            FilesStatus = status;
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ApplyFileBrowserPreferences(CottonFileBrowserPreferences preferences)
        {
            ArgumentNullException.ThrowIfNull(preferences);

            FileViewMode = preferences.ViewMode;
            FileSortMode = preferences.SortMode;
            ApplyFileFilters();
        }

        public void ShowFileViewMode(CottonFileBrowserViewMode viewMode)
        {
            FileViewMode = viewMode;
        }

        public void ShowFileSortMode(CottonFileBrowserSortMode sortMode)
        {
            FileSortMode = sortMode;
            ApplyFileFilters();
            FilesStatus = CreateFilesStatus();
        }

        private void SetStatus(string? status)
        {
            Status = status;
        }

        private void SetState(MainPageViewState state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            OnPropertyChanged(nameof(IsLoadingVisible));
            OnPropertyChanged(nameof(IsSignInVisible));
            OnPropertyChanged(nameof(IsAuthorizationProgressVisible));
            OnPropertyChanged(nameof(IsProfileVisible));
            OnPropertyChanged(nameof(IsBrandHeaderVisible));
            OnPropertyChanged(nameof(IsLoadingIndicatorRunning));
            OnPropertyChanged(nameof(IsAuthorizationProgressIndicatorRunning));
        }

        private void ApplyFileFilters()
        {
            List<CottonFileBrowserEntry> visibleEntries = SortEntries(
                    _allFileEntries.Where(entry => entry.Matches(FileSearchText)))
                .ToList();

            FileEntries.Clear();
            foreach (CottonFileBrowserEntry entry in visibleEntries)
            {
                FileEntries.Add(entry);
            }

            FilesEmptyMessage = ResolveFilesEmptyMessage(visibleEntries.Count);
            FilesStatus = CreateFilesStatus();
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        private IEnumerable<CottonFileBrowserEntry> SortEntries(IEnumerable<CottonFileBrowserEntry> entries)
        {
            return FileSortMode switch
            {
                CottonFileBrowserSortMode.Type => entries
                    .OrderBy(entry => entry.IsFolder ? 0 : 1)
                    .ThenBy(entry => entry.Kind, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase),
                CottonFileBrowserSortMode.Size => entries
                    .OrderBy(entry => entry.IsFolder ? 0 : 1)
                    .ThenBy(entry => entry.IsFolder ? entry.Name : string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(entry => entry.SizeBytes ?? 0)
                    .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase),
                _ => entries
                    .OrderBy(entry => entry.IsFolder ? 0 : 1)
                    .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase),
            };
        }

        private string CreateFilesStatus()
        {
            int totalCount = _allFileEntries.Count;
            int visibleCount = FileEntries.Count;
            if (totalCount == 0)
            {
                return "This folder is empty.";
            }

            string count = visibleCount == totalCount
                ? $"{totalCount} item(s)"
                : $"{visibleCount} of {totalCount} item(s)";
            return $"{count} · {FileSortMode}";
        }

        private string ResolveFilesEmptyMessage(int visibleCount)
        {
            if (_allFileEntries.Count == 0)
            {
                return "This folder is empty.";
            }

            return visibleCount == 0 ? "No matching files." : string.Empty;
        }
    }
}
