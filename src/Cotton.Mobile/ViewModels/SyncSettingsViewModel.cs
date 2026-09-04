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
    public partial class SyncSettingsViewModel : ObservableObject, ISyncSettingsViewState
    {
        private readonly SyncSettingsLoadingHandler _loadingHandler;
        private readonly SyncSettingsExecutionHandler _executionHandler;
        private readonly SyncSettingsSetupHandler _setupHandler;
        private readonly SyncSettingsManagementHandler _managementHandler;
        private readonly SyncSettingsStatusObserver _statusObserver;
        private readonly ILogger<SyncSettingsViewModel> _logger;
        private Uri? _instanceUri;
        private string? _accountScopeKey;
        private bool _isBusy;
        private bool _canRunAll;
        private string? _status;
        private long _statusRevision;
        private bool _isEmptyVisible = true;
        private bool _isEditMode;

        public SyncSettingsViewModel(
            SyncSettingsLoadingHandler loadingHandler,
            SyncSettingsExecutionHandler executionHandler,
            SyncSettingsSetupHandler setupHandler,
            SyncSettingsManagementHandler managementHandler,
            SyncSettingsStatusObserver statusObserver,
            BackgroundSyncRestrictionViewModel backgroundRestriction,
            ILogger<SyncSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(statusObserver);

            _loadingHandler = loadingHandler ?? throw new ArgumentNullException(nameof(loadingHandler));
            _executionHandler = executionHandler ?? throw new ArgumentNullException(nameof(executionHandler));
            _setupHandler = setupHandler ?? throw new ArgumentNullException(nameof(setupHandler));
            _managementHandler = managementHandler ?? throw new ArgumentNullException(nameof(managementHandler));
            _statusObserver = statusObserver;
            BackgroundRestriction = backgroundRestriction
                ?? throw new ArgumentNullException(nameof(backgroundRestriction));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statusObserver.Attach(this);
            AddRootCommand = CreateAddRootCommand();
            RunAllCommand = CreateRunAllCommand();
            RootActionCommand = CreateRootActionCommand();
            PauseRootCommand = CreatePauseRootCommand();
            EnterEditModeCommand = new RelayCommand(EnterEditMode, CanEnterEditMode);
            ExitEditModeCommand = new RelayCommand(ExitEditMode, CanExitEditMode);
        }

        public IAsyncRelayCommand AddRootCommand { get; }

        public IAsyncRelayCommand RunAllCommand { get; }

        public IAsyncRelayCommand<CottonSyncRootActionRequest> RootActionCommand { get; }

        public IAsyncRelayCommand<CottonSyncRootActionRequest> PauseRootCommand { get; }

        public IRelayCommand EnterEditModeCommand { get; }

        public IRelayCommand ExitEditModeCommand { get; }

        public BackgroundSyncRestrictionViewModel BackgroundRestriction { get; }

        public RangeObservableCollection<CottonSyncRootListItem> Roots { get; } = [];

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    AddRootCommand.NotifyCanExecuteChanged();
                    RunAllCommand.NotifyCanExecuteChanged();
                    RootActionCommand.NotifyCanExecuteChanged();
                    PauseRootCommand.NotifyCanExecuteChanged();
                    EnterEditModeCommand.NotifyCanExecuteChanged();
                    ExitEditModeCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string? Status
        {
            get => _status;
            private set
            {
                _ = Interlocked.Increment(ref _statusRevision);
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(IsStatusVisible));
                }
            }
        }

        public bool IsStatusVisible => !string.IsNullOrWhiteSpace(Status);

        public bool IsEmptyVisible
        {
            get => _isEmptyVisible;
            private set
            {
                if (SetProperty(ref _isEmptyVisible, value))
                {
                    OnPropertyChanged(nameof(IsListVisible));
                    OnPropertyChanged(nameof(IsEditModeAvailable));
                    EnterEditModeCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsListVisible => !IsEmptyVisible;

        public bool IsRunAllVisible => _canRunAll && !IsEditMode;

        public bool ArePrimaryActionsVisible => !IsEditMode;

        public bool IsEditModeAvailable => IsListVisible && !IsEditMode;

        public bool IsEditMode
        {
            get => _isEditMode;
            private set
            {
                if (!SetProperty(ref _isEditMode, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(TopBarTitle));
                OnPropertyChanged(nameof(ArePrimaryActionsVisible));
                OnPropertyChanged(nameof(IsEditModeAvailable));
                OnPropertyChanged(nameof(IsRunAllVisible));
                EnterEditModeCommand.NotifyCanExecuteChanged();
                ExitEditModeCommand.NotifyCanExecuteChanged();
            }
        }

        public string TopBarTitle => IsEditMode
            ? AppResources.EditSyncsModeTitle
            : AppResources.SyncTitle;

        public void Configure(Uri instanceUri, string accountScopeKey)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);

            _instanceUri = instanceUri;
            _accountScopeKey = accountScopeKey.Trim();
            AddRootCommand.NotifyCanExecuteChanged();
        }

        public async Task LoadForInstanceAsync(
            Uri instanceUri,
            string accountScopeKey,
            CancellationToken cancellationToken = default)
        {
            Configure(instanceUri, accountScopeKey);
            await _loadingHandler.LoadAsync(this, cancellationToken);
            await _setupHandler.ResumePendingSetupAsync(this, cancellationToken);
        }

        public void Clear()
        {
            _instanceUri = null;
            _accountScopeKey = null;
            ReplaceRoots([]);
            Status = null;
            IsEmptyVisible = true;
            IsEditMode = false;
            _canRunAll = false;
            BackgroundRestriction.SetAutomaticSyncEnabled(isEnabled: false);
            OnPropertyChanged(nameof(IsRunAllVisible));
            RunAllCommand.NotifyCanExecuteChanged();
            AddRootCommand.NotifyCanExecuteChanged();
        }

        private void ShowRoots(SyncRootCollectionSnapshot collection)
        {
            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create(
                collection.Roots,
                collection.PausedRootIds,
                collection.AutomaticSyncStatuses);
            ReplaceRoots(state.Items);
            _statusObserver.RefreshProgress();
            BackgroundRestriction.SetAutomaticSyncEnabled(state.CanRunAny);
            if (!state.HasItems)
            {
                IsEditMode = false;
            }

            IsEmptyVisible = state.IsEmptyVisible;
            bool canRunAllChanged = _canRunAll != state.CanRunAny;
            _canRunAll = state.CanRunAny;
            if (canRunAllChanged)
            {
                OnPropertyChanged(nameof(IsRunAllVisible));
            }

            RunAllCommand.NotifyCanExecuteChanged();
        }

        private void ReplaceRoots(IEnumerable<CottonSyncRootListItem> items)
        {
            foreach (CottonSyncRootListItem item in Roots)
            {
                item.PropertyChanged -= OnRootPropertyChanged;
            }

            Roots.ReplaceWith(items);
            foreach (CottonSyncRootListItem item in Roots)
            {
                item.PropertyChanged += OnRootPropertyChanged;
            }
        }

        Uri? ISyncSettingsViewState.InstanceUri => _instanceUri;

        string? ISyncSettingsViewState.AccountScopeKey => _accountScopeKey;

        bool ISyncSettingsViewState.IsBusy
        {
            get => IsBusy;
            set => IsBusy = value;
        }

        string? ISyncSettingsViewState.Status
        {
            get => Status;
            set => Status = value;
        }

        long ISyncSettingsViewState.StatusRevision => Interlocked.Read(ref _statusRevision);

        IReadOnlyList<CottonSyncRootListItem> ISyncSettingsViewState.Roots => Roots;

        void ISyncSettingsViewState.ShowRoots(SyncRootCollectionSnapshot collection)
        {
            ShowRoots(collection);
        }
    }
}
