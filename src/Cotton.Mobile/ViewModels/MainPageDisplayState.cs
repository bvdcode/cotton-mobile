namespace Cotton.Mobile.ViewModels
{
    public class MainPageDisplayState : ViewModelBase
    {
        private MainPageViewState _state = MainPageViewState.SignIn;
        private string _instanceUrl = string.Empty;
        private string _loadingMessage = "Restoring session...";
        private string? _status;
        private string _authorizationProgressMessage = "Approve the request in your browser, then return to Cotton Cloud.";
        private string _profileName = string.Empty;
        private string _profileEmail = string.Empty;
        private string _profileInstance = string.Empty;
        private string? _profileStatus;
        private bool _isInputEnabled = true;
        private bool _isCancelAuthorizationEnabled = true;
        private bool _isLogoutEnabled = true;

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
            private set => SetProperty(ref _profileEmail, value);
        }

        public string ProfileInstance
        {
            get => _profileInstance;
            private set => SetProperty(ref _profileInstance, value);
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
            IsLogoutEnabled = true;
            IsInputEnabled = false;
        }

        public void ShowProfileError(string status)
        {
            SetState(MainPageViewState.Profile);
            ProfileStatus = status;
            IsLogoutEnabled = true;
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
            OnPropertyChanged(nameof(IsLoadingIndicatorRunning));
            OnPropertyChanged(nameof(IsAuthorizationProgressIndicatorRunning));
        }
    }
}
