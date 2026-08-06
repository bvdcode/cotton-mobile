// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cotton.Mobile.ViewModels
{
    public class MainPageDisplayState : ObservableObject
    {
        private MainPageViewState _state = MainPageViewState.Loading;
        private AppNavigationDestination _selectedDestination = AppNavigationDestination.Sync;
        private string _instanceUrl = string.Empty;
        private string _loadingMessage = string.Empty;
        private string? _status;
        private string _authorizationProgressMessage = CoreResources.AuthorizationInstruction;
        private string _profileName = string.Empty;
        private string? _profileEmail;
        private string _profileInstance = string.Empty;
        private string? _profileStatus;
        private string? _profileAvatarUrl;
        private bool _isInputEnabled = true;
        private bool _isCancelAuthorizationEnabled;
        private bool _isLogoutEnabled;

        public MainPageDisplayState(string defaultInstanceUrl)
        {
            if (string.IsNullOrWhiteSpace(defaultInstanceUrl))
            {
                throw new ArgumentException("Default instance URL is required.", nameof(defaultInstanceUrl));
            }

            DefaultInstanceUrl = defaultInstanceUrl.Trim();
        }

        public string InstanceUrl
        {
            get => _instanceUrl;
            set
            {
                if (SetProperty(ref _instanceUrl, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(EffectiveInstanceUrl));
                }
            }
        }

        public string DefaultInstanceUrl { get; }

        public string InstanceUrlPlaceholder => CoreResources.CustomServerUrl;

        public string EffectiveInstanceUrl => string.IsNullOrWhiteSpace(InstanceUrl)
            ? DefaultInstanceUrl
            : InstanceUrl;

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

        public string? ProfileEmail
        {
            get => _profileEmail;
            private set
            {
                if (SetProperty(ref _profileEmail, value))
                {
                    OnPropertyChanged(nameof(IsProfileEmailVisible));
                }
            }
        }

        public bool IsProfileEmailVisible => !string.IsNullOrWhiteSpace(ProfileEmail);

        public string? ProfileAvatarUrl
        {
            get => _profileAvatarUrl;
            private set => SetProperty(ref _profileAvatarUrl, value);
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

        public AppNavigationDestination SelectedDestination
        {
            get => _selectedDestination;
            private set
            {
                if (SetProperty(ref _selectedDestination, value))
                {
                    OnPropertyChanged(nameof(IsSyncDestinationVisible));
                    OnPropertyChanged(nameof(IsProfileDestinationVisible));
                }
            }
        }

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

        public bool IsLoadingIndicatorRunning => IsLoadingVisible;

        public bool IsSignInVisible => _state == MainPageViewState.SignIn;

        public bool IsAuthorizationProgressVisible => _state == MainPageViewState.AuthorizationProgress;

        public bool IsAuthorizationProgressIndicatorRunning =>
            IsAuthorizationProgressVisible && IsCancelAuthorizationEnabled;

        public bool IsAuthenticatedVisible => _state == MainPageViewState.Authenticated;

        public bool IsBrandHeaderVisible => !IsAuthenticatedVisible;

        public bool IsLegalFooterVisible => IsSignInVisible;

        public bool IsSyncDestinationVisible =>
            IsAuthenticatedVisible && SelectedDestination == AppNavigationDestination.Sync;

        public bool IsProfileDestinationVisible =>
            IsAuthenticatedVisible && SelectedDestination == AppNavigationDestination.Profile;

        public void ShowLoading(string message)
        {
            LoadingMessage = message ?? string.Empty;
            Status = null;
            IsInputEnabled = false;
            IsCancelAuthorizationEnabled = false;
            IsLogoutEnabled = false;
            SetState(MainPageViewState.Loading);
        }

        public void ShowSignIn(string? status)
        {
            LoadingMessage = string.Empty;
            Status = status;
            ProfileStatus = null;
            ProfileAvatarUrl = null;
            IsInputEnabled = true;
            IsCancelAuthorizationEnabled = false;
            IsLogoutEnabled = false;
            SetState(MainPageViewState.SignIn);
        }

        public void ShowAuthorizationProgress()
        {
            LoadingMessage = string.Empty;
            Status = null;
            AuthorizationProgressMessage = CoreResources.AuthorizationInstruction;
            IsInputEnabled = false;
            IsCancelAuthorizationEnabled = true;
            IsLogoutEnabled = false;
            SetState(MainPageViewState.AuthorizationProgress);
        }

        public void ShowAuthorizationCancelling()
        {
            AuthorizationProgressMessage = CoreResources.CancellingAuthorization;
            IsCancelAuthorizationEnabled = false;
        }

        public void ShowAuthenticated(MainPageProfile profile, string? status = null)
        {
            ArgumentNullException.ThrowIfNull(profile);

            LoadingMessage = string.Empty;
            Status = null;
            ProfileName = profile.Name;
            ProfileEmail = profile.Email;
            ProfileInstance = profile.Instance;
            ProfileAvatarUrl = profile.AvatarUrl?.AbsoluteUri;
            ProfileStatus = status;
            IsInputEnabled = false;
            IsCancelAuthorizationEnabled = false;
            IsLogoutEnabled = true;
            SelectedDestination = AppNavigationDestination.Sync;
            SetState(MainPageViewState.Authenticated);
        }

        public void ShowProfileStatus(string? status)
        {
            ProfileStatus = status;
        }

        public void ShowDestination(AppNavigationDestination destination)
        {
            if (!Enum.IsDefined(destination))
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            if (!IsAuthenticatedVisible)
            {
                return;
            }

            SelectedDestination = destination;
        }

        private void SetState(MainPageViewState state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            OnPropertyChanged(nameof(IsLoadingVisible));
            OnPropertyChanged(nameof(IsLoadingIndicatorRunning));
            OnPropertyChanged(nameof(IsSignInVisible));
            OnPropertyChanged(nameof(IsAuthorizationProgressVisible));
            OnPropertyChanged(nameof(IsAuthorizationProgressIndicatorRunning));
            OnPropertyChanged(nameof(IsAuthenticatedVisible));
            OnPropertyChanged(nameof(IsBrandHeaderVisible));
            OnPropertyChanged(nameof(IsLegalFooterVisible));
            OnPropertyChanged(nameof(IsSyncDestinationVisible));
            OnPropertyChanged(nameof(IsProfileDestinationVisible));
        }
    }
}
