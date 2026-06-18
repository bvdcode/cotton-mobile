namespace Cotton.Mobile.ViewModels
{
    using System.Collections.ObjectModel;
    using Cotton.Mobile.Services;

    public class MainPageDisplayState : ViewModelBase
    {
        public const string RootFilesTitle = "Files";

        private readonly List<CottonFileBrowserEntry> _allFileEntries = [];

        private MainPageViewState _state = MainPageViewState.SignIn;
        private string _instanceUrl = string.Empty;
        private string _loadingMessage = "Restoring session...";
        private string? _status;
        private string _authorizationProgressMessage = "Approve the request in your browser, then return to Cotton Cloud.";
        private string _profileName = string.Empty;
        private string? _profileEmail;
        private string _profileInstance = string.Empty;
        private string? _profileStatus;
        private string _filesTitle = "Files";
        private string _filesPath = string.Empty;
        private string? _filesStatus;
        private string _filesEmptyMessage = "No files in this folder.";
        private string _filesEmptyDetails = string.Empty;
        private string? _filesNoticeTitle;
        private string? _filesNoticeMessage;
        private string _fileSearchText = string.Empty;
        private CottonFileBrowserViewMode _fileViewMode = CottonFileBrowserViewMode.List;
        private CottonFileBrowserSortMode _fileSortMode = CottonFileBrowserSortMode.Name;
        private bool _isInputEnabled = true;
        private bool _isCancelAuthorizationEnabled;
        private bool _isLogoutEnabled;
        private bool _isFilesLoading;
        private bool _isFilesRefreshing;
        private bool _isFileActionInProgress;
        private bool _isFileSearchOpen;
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

        public event EventHandler? FileSearchTextChanged;

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
            private set
            {
                if (SetProperty(ref _profileName, value))
                {
                    OnPropertyChanged(nameof(ProfileInitials));
                }
            }
        }

        public string ProfileInitials => CreateProfileInitials(ProfileName);

        public string? ProfileEmail
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

        public string ProfileSummary
        {
            get
            {
                string[] parts =
                [
                    ProfileEmail ?? string.Empty,
                    ProfileInstance,
                ];

                string summary = string.Join(
                    " · ",
                    parts.Where(part => !string.IsNullOrWhiteSpace(part)));
                return string.IsNullOrWhiteSpace(summary) ? "Signed in" : summary;
            }
        }

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

        public int TotalFileEntryCount => _allFileEntries.Count;

        public int VisibleFileEntryCount => FileEntries.Count;

        public string FilesEmptyMessage
        {
            get => _filesEmptyMessage;
            private set => SetProperty(ref _filesEmptyMessage, value);
        }

        public string FilesEmptyDetails
        {
            get => _filesEmptyDetails;
            private set
            {
                if (SetProperty(ref _filesEmptyDetails, value))
                {
                    OnPropertyChanged(nameof(IsFilesEmptyDetailsVisible));
                }
            }
        }

        public bool IsFilesEmptyDetailsVisible => !string.IsNullOrWhiteSpace(FilesEmptyDetails);

        public string? FilesNoticeTitle
        {
            get => _filesNoticeTitle;
            private set
            {
                if (SetProperty(ref _filesNoticeTitle, value))
                {
                    OnPropertyChanged(nameof(IsFilesNoticeVisible));
                    OnPropertyChanged(nameof(IsFilesEmptyVisible));
                }
            }
        }

        public string? FilesNoticeMessage
        {
            get => _filesNoticeMessage;
            private set
            {
                if (SetProperty(ref _filesNoticeMessage, value))
                {
                    OnPropertyChanged(nameof(IsFilesNoticeVisible));
                    OnPropertyChanged(nameof(IsFilesEmptyVisible));
                }
            }
        }

        public bool IsFilesNoticeVisible =>
            !string.IsNullOrWhiteSpace(FilesNoticeTitle)
            || !string.IsNullOrWhiteSpace(FilesNoticeMessage);

        public string FileSearchText
        {
            get => _fileSearchText;
            set
            {
                if (SetProperty(ref _fileSearchText, value ?? string.Empty))
                {
                    NotifyFileSearchStateChanged();
                    FileSearchTextChanged?.Invoke(this, EventArgs.Empty);
                    ApplyFileFilters();
                }
            }
        }

        public bool IsFileSearchVisible => _isFileSearchOpen || !string.IsNullOrWhiteSpace(FileSearchText);

        public bool IsFileSearchOpen => _isFileSearchOpen;

        public bool IsFileSearchActive => !string.IsNullOrWhiteSpace(FileSearchText);

        public string FileSearchButtonText
        {
            get
            {
                return _isFileSearchOpen || !string.IsNullOrWhiteSpace(FileSearchText)
                    ? "×"
                    : "⌕";
            }
        }

        public string FileSearchButtonDescription
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FileSearchText))
                {
                    return "Clear file search";
                }

                return _isFileSearchOpen ? "Close file search" : "Search files";
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
                    OnPropertyChanged(nameof(FileViewButtonText));
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
                    OnPropertyChanged(nameof(FileSortButtonText));
                }
            }
        }

        public bool IsFileListViewVisible => FileViewMode == CottonFileBrowserViewMode.List;

        public bool IsFileTileViewVisible => FileViewMode == CottonFileBrowserViewMode.Tiles;

        public string FileViewButtonText => FileViewMode == CottonFileBrowserViewMode.List ? "☰" : "▦";

        public bool IsFileUpButtonVisible => CanNavigateFilesUp;

        public string FileSortButtonText => FileSortMode switch
        {
            CottonFileBrowserSortMode.Name => "A-Z",
            CottonFileBrowserSortMode.Updated => "New",
            CottonFileBrowserSortMode.Type => "Type",
            CottonFileBrowserSortMode.Size => "Size",
            _ => FileSortMode.ToString(),
        };

        public bool IsFilesLoading
        {
            get => _isFilesLoading;
            private set
            {
                if (SetProperty(ref _isFilesLoading, value))
                {
                    NotifyFileBrowserChromeStateChanged();
                }
            }
        }

        public bool IsFilesRefreshing
        {
            get => _isFilesRefreshing;
            set
            {
                bool nextValue = value && IsProfileVisible;
                if (SetProperty(ref _isFilesRefreshing, nextValue))
                {
                    NotifyFileBrowserChromeStateChanged();
                }
                else if (value != nextValue)
                {
                    OnPropertyChanged(nameof(IsFilesRefreshing));
                }
            }
        }

        public bool CanNavigateFilesUp
        {
            get => _canNavigateFilesUp;
            private set
            {
                if (SetProperty(ref _canNavigateFilesUp, value))
                {
                    OnPropertyChanged(nameof(FileUpButtonOpacity));
                    OnPropertyChanged(nameof(IsFileUpButtonVisible));
                    OnPropertyChanged(nameof(IsFileUpButtonEnabled));
                }
            }
        }

        public bool IsFileUpButtonEnabled => CanNavigateFilesUp && IsFileBrowserChromeEnabled;

        public double FileUpButtonOpacity => IsFileUpButtonEnabled ? 1 : 0.35;

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

        public bool IsFilesEmptyVisible => !IsFilesLoading && !IsFilesNoticeVisible && FileEntries.Count == 0;

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

        public bool IsAccountActionEnabled => IsProfileVisible && !IsFileActionInProgress;

        public bool IsFileBrowserChromeEnabled => IsProfileVisible && !IsFileBrowserBusy;

        public bool IsBrandHeaderVisible => _state != MainPageViewState.Profile;

        public bool IsLegalFooterVisible => _state != MainPageViewState.Profile;

        public bool IsLoadingIndicatorRunning => _state == MainPageViewState.Loading;

        public bool IsAuthorizationProgressIndicatorRunning => _state == MainPageViewState.AuthorizationProgress;

        private bool IsFileActionInProgress
        {
            get => _isFileActionInProgress;
            set
            {
                if (SetProperty(ref _isFileActionInProgress, value))
                {
                    OnPropertyChanged(nameof(IsAccountActionEnabled));
                }
            }
        }

        public void ShowLoading(string message)
        {
            SetState(MainPageViewState.Loading);
            LoadingMessage = message;
            IsInputEnabled = false;
            IsCancelAuthorizationEnabled = false;
            IsLogoutEnabled = false;
            IsFilesLoading = false;
            IsFilesRefreshing = false;
            IsFileActionInProgress = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            ProfileStatus = null;
        }

        public void ShowSignIn(string? status)
        {
            ClearSignedOutPresentationState();
            SetState(MainPageViewState.SignIn);
            IsInputEnabled = true;
            SetStatus(status);
        }

        public void ShowAuthorizationProgress(Uri instanceUri)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            SetState(MainPageViewState.AuthorizationProgress);
            IsCancelAuthorizationEnabled = true;
            IsLogoutEnabled = false;
            IsFileActionInProgress = false;
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
            FilesTitle = RootFilesTitle;
            FilesPath = string.Empty;
            FilesStatus = "Loading files...";
            ClearFilesNotice();
            IsFilesLoading = true;
            IsFilesRefreshing = false;
            IsFileActionInProgress = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            CanNavigateFilesUp = false;
            _allFileEntries.Clear();
            FileEntries.Clear();
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
            IsLogoutEnabled = true;
            IsCancelAuthorizationEnabled = false;
            IsInputEnabled = false;
        }

        private void ClearSignedOutPresentationState()
        {
            ProfileName = string.Empty;
            ProfileEmail = null;
            ProfileInstance = string.Empty;
            ProfileStatus = null;
            FilesTitle = RootFilesTitle;
            FilesPath = string.Empty;
            FilesStatus = null;
            FilesEmptyMessage = "No files in this folder.";
            FilesEmptyDetails = string.Empty;
            ClearFilesNotice();
            IsFilesLoading = false;
            IsFilesRefreshing = false;
            IsFileActionInProgress = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            CanNavigateFilesUp = false;
            IsCancelAuthorizationEnabled = false;
            IsLogoutEnabled = false;
            _allFileEntries.Clear();
            FileEntries.Clear();
            ClearFileSearch();
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ShowProfileError(string status)
        {
            SetState(MainPageViewState.Profile);
            ProfileStatus = status;
            IsLogoutEnabled = true;
            IsCancelAuthorizationEnabled = false;
            IsFileActionInProgress = false;
            IsInputEnabled = false;
        }

        public void ShowFilesLoading(string status)
        {
            IsFilesLoading = true;
            IsFilesRefreshing = false;
            IsFileActionInProgress = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            FilesStatus = status;
            ClearFilesNotice();
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ShowFileActionLoading(string status)
        {
            IsFilesLoading = true;
            IsFilesRefreshing = false;
            IsFileActionInProgress = true;
            CanCancelFileAction = true;
            CanRetryFileAction = false;
            FilesStatus = status;
            ClearFilesNotice();
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
            IsFileActionInProgress = false;
            CanCancelFileAction = false;
            CanRetryFileAction = true;
            FilesStatus = status;
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ClearFileActionRetry()
        {
            bool wasRetryVisible = CanRetryFileAction;
            CanRetryFileAction = false;
            if (wasRetryVisible && !IsFileBrowserBusy)
            {
                FilesStatus = CreateFilesStatus();
            }
        }

        public void ShowFilesRefreshing(string status)
        {
            IsFilesLoading = false;
            IsFilesRefreshing = true;
            IsFileActionInProgress = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            FilesStatus = status;
            ClearFilesNotice();
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void StopFilesRefreshing()
        {
            IsFilesRefreshing = false;
        }

        public void ShowFiles(CottonFolderContent content, bool isRoot, bool canNavigateUp, string path)
        {
            ArgumentNullException.ThrowIfNull(content);

            FilesTitle = isRoot ? RootFilesTitle : content.FolderName;
            FilesPath = isRoot || string.Equals(path, FilesTitle, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : path;
            _allFileEntries.Clear();
            foreach (CottonFileBrowserEntry entry in content.Entries)
            {
                _allFileEntries.Add(entry);
            }

            IsFilesLoading = false;
            IsFilesRefreshing = false;
            IsFileActionInProgress = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            CanNavigateFilesUp = canNavigateUp;
            ClearFilesNotice();
            ApplyFileFilters();
            FilesStatus = CreateFilesStatus();
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ShowFilesStatus(string status)
        {
            IsFilesLoading = false;
            IsFilesRefreshing = false;
            IsFileActionInProgress = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            FilesStatus = status;
            ClearFilesNotice();
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ShowOfflineFilesNotice()
        {
            IsFilesLoading = false;
            IsFilesRefreshing = false;
            IsFileActionInProgress = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            FilesStatus = CreateFilesStatus();
            FilesNoticeTitle = "Offline";
            FilesNoticeMessage = _allFileEntries.Count == 0
                ? "Reconnect to load this folder."
                : "Files marked On device can still open.";
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ShowFilesSummary()
        {
            IsFilesLoading = false;
            IsFilesRefreshing = false;
            IsFileActionInProgress = false;
            CanCancelFileAction = false;
            CanRetryFileAction = false;
            FilesStatus = CreateFilesStatus();
            ClearFilesNotice();
            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        public void ShowFileLocalCopy(CottonFileBrowserEntry file, CottonLocalFileSnapshot localFile)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(localFile);

            int allIndex = FindEntryIndex(_allFileEntries, file.Id);
            if (allIndex < 0)
            {
                return;
            }

            CottonFileBrowserEntry updatedEntry = _allFileEntries[allIndex].WithLocalFile(localFile);
            _allFileEntries[allIndex] = updatedEntry;

            int visibleIndex = FindEntryIndex(FileEntries, file.Id);
            if (visibleIndex >= 0)
            {
                FileEntries[visibleIndex] = updatedEntry;
            }
        }

        public bool ClearFileLocalCopy(CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(file);

            int allIndex = FindEntryIndex(_allFileEntries, file.Id);
            if (allIndex < 0 || _allFileEntries[allIndex].LocalFile is null)
            {
                return false;
            }

            CottonFileBrowserEntry updatedEntry = _allFileEntries[allIndex].WithoutLocalFile();
            _allFileEntries[allIndex] = updatedEntry;

            int visibleIndex = FindEntryIndex(FileEntries, file.Id);
            if (visibleIndex >= 0)
            {
                FileEntries[visibleIndex] = updatedEntry;
            }

            return true;
        }

        public bool RefreshFileLocalCopies(Func<CottonFileBrowserEntry, CottonLocalFileSnapshot?> resolveLocalFile)
        {
            ArgumentNullException.ThrowIfNull(resolveLocalFile);

            bool changed = false;
            for (int index = 0; index < _allFileEntries.Count; index++)
            {
                CottonFileBrowserEntry entry = _allFileEntries[index];
                if (entry.Type != CottonFileBrowserEntryType.File)
                {
                    continue;
                }

                CottonLocalFileSnapshot? localFile = resolveLocalFile(entry);
                if (HasSameLocalFile(entry.LocalFile, localFile))
                {
                    continue;
                }

                _allFileEntries[index] = localFile is null
                    ? entry.WithoutLocalFile()
                    : entry.WithLocalFile(localFile);
                changed = true;
            }

            if (changed)
            {
                ApplyFileFilters();
            }

            return changed;
        }

        public void ClearFileLocalCopies()
        {
            bool changed = false;
            for (int index = 0; index < _allFileEntries.Count; index++)
            {
                CottonFileBrowserEntry entry = _allFileEntries[index];
                if (entry.LocalFile is null)
                {
                    continue;
                }

                _allFileEntries[index] = entry.WithoutLocalFile();
                changed = true;
            }

            if (changed)
            {
                ApplyFileFilters();
            }
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

        public void ToggleFileSearch()
        {
            if (!string.IsNullOrWhiteSpace(FileSearchText))
            {
                FileSearchText = string.Empty;
                return;
            }

            if (_isFileSearchOpen)
            {
                _isFileSearchOpen = false;
            }
            else
            {
                _isFileSearchOpen = true;
            }

            NotifyFileSearchStateChanged();
        }

        public void ClearFileSearch()
        {
            bool wasFileSearchOpen = _isFileSearchOpen;
            _isFileSearchOpen = false;

            if (!string.IsNullOrWhiteSpace(FileSearchText))
            {
                FileSearchText = string.Empty;
                return;
            }

            if (wasFileSearchOpen)
            {
                NotifyFileSearchStateChanged();
            }
        }

        public void RestoreFileSearch(string? searchText, bool isOpen)
        {
            string? previousFilesStatus = FilesStatus;
            bool wasFileSearchOpen = _isFileSearchOpen;
            _isFileSearchOpen = isOpen;

            string normalizedSearchText = searchText ?? string.Empty;
            if (!string.Equals(FileSearchText, normalizedSearchText, StringComparison.Ordinal))
            {
                FileSearchText = normalizedSearchText;
                FilesStatus = previousFilesStatus;
                return;
            }

            if (wasFileSearchOpen != isOpen)
            {
                NotifyFileSearchStateChanged();
            }
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
            OnPropertyChanged(nameof(IsAccountActionEnabled));
            OnPropertyChanged(nameof(IsFileBrowserChromeEnabled));
            OnPropertyChanged(nameof(IsFileUpButtonEnabled));
            OnPropertyChanged(nameof(IsBrandHeaderVisible));
            OnPropertyChanged(nameof(IsLegalFooterVisible));
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

            ResolveFilesEmptyState(visibleEntries.Count);
            if (!IsFileBrowserBusy)
            {
                FilesStatus = CreateFilesStatus();
            }

            OnPropertyChanged(nameof(IsFilesEmptyVisible));
        }

        private void NotifyFileSearchStateChanged()
        {
            OnPropertyChanged(nameof(IsFileSearchVisible));
            OnPropertyChanged(nameof(IsFileSearchOpen));
            OnPropertyChanged(nameof(FileSearchButtonText));
            OnPropertyChanged(nameof(FileSearchButtonDescription));
        }

        private void NotifyFileBrowserChromeStateChanged()
        {
            OnPropertyChanged(nameof(IsFileBrowserChromeEnabled));
            OnPropertyChanged(nameof(IsFileUpButtonEnabled));
            OnPropertyChanged(nameof(FileUpButtonOpacity));
        }

        private bool IsFileBrowserBusy => IsFilesLoading || IsFilesRefreshing;

        private IEnumerable<CottonFileBrowserEntry> SortEntries(IEnumerable<CottonFileBrowserEntry> entries)
        {
            return FileSortMode switch
            {
                CottonFileBrowserSortMode.Type => entries
                    .OrderBy(entry => entry.IsFolder ? 0 : 1)
                    .ThenBy(entry => entry.Kind, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase),
                CottonFileBrowserSortMode.Updated => entries
                    .OrderBy(entry => entry.IsFolder ? 0 : 1)
                    .ThenByDescending(entry => entry.UpdatedAtUtc)
                    .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase),
                CottonFileBrowserSortMode.Size => entries
                    .OrderBy(entry => entry.IsFolder ? 0 : 1)
                    .ThenBy(entry => entry.IsFolder ? entry.Name : string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(entry => entry.SizeBytes.HasValue ? 0 : 1)
                    .ThenByDescending(entry => entry.SizeBytes ?? 0)
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
                return string.Empty;
            }

            string count = visibleCount == totalCount
                ? FormatItemCount(totalCount)
                : CreateFilteredCount(visibleCount);
            return $"{count} · {FileSortMode}";
        }

        private string CreateFilteredCount(int visibleCount)
        {
            if (IsFileSearchActive)
            {
                return visibleCount == 1 ? "1 match" : $"{visibleCount:N0} matches";
            }

            return $"{FormatItemCount(visibleCount)} shown";
        }

        private static string FormatItemCount(int count)
        {
            return count == 1 ? "1 item" : $"{count} items";
        }

        private static bool HasSameLocalFile(CottonLocalFileSnapshot? current, CottonLocalFileSnapshot? next)
        {
            if (current is null || next is null)
            {
                return current is null && next is null;
            }

            return current.SizeBytes == next.SizeBytes
                && current.UpdatedAtUtc == next.UpdatedAtUtc
                && string.Equals(current.FileName, next.FileName, StringComparison.Ordinal);
        }

        private static int FindEntryIndex(IList<CottonFileBrowserEntry> entries, Guid id)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].Id == id)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string CreateProfileInitials(string profileName)
        {
            string[] parts = profileName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
            }

            if (parts.Length == 1)
            {
                return parts[0].Length == 1
                    ? parts[0].ToUpperInvariant()
                    : parts[0][..2].ToUpperInvariant();
            }

            return "CC";
        }

        private void ResolveFilesEmptyState(int visibleCount)
        {
            if (_allFileEntries.Count == 0)
            {
                FilesEmptyMessage = "This folder is empty";
                FilesEmptyDetails = "Files added here will appear automatically.";
                return;
            }

            if (visibleCount == 0)
            {
                FilesEmptyMessage = "No matching files";
                FilesEmptyDetails = "Try another name, type, or extension.";
                return;
            }

            FilesEmptyMessage = string.Empty;
            FilesEmptyDetails = string.Empty;
        }

        private void ClearFilesNotice()
        {
            FilesNoticeTitle = null;
            FilesNoticeMessage = null;
        }
    }
}
