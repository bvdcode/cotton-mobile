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
    public class SyncSettingsViewModel : ObservableObject, ISyncSettingsViewState
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
        private string _summaryText = AppResources.NoFoldersSyncing;
        private string? _status;
        private long _statusRevision;
        private string? _automaticStatus;
        private bool _isEmptyVisible = true;

        public SyncSettingsViewModel(
            SyncSettingsLoadingHandler loadingHandler,
            SyncSettingsExecutionHandler executionHandler,
            SyncSettingsSetupHandler setupHandler,
            SyncSettingsManagementHandler managementHandler,
            SyncSettingsStatusObserver statusObserver,
            ILogger<SyncSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(statusObserver);

            _loadingHandler = loadingHandler ?? throw new ArgumentNullException(nameof(loadingHandler));
            _executionHandler = executionHandler ?? throw new ArgumentNullException(nameof(executionHandler));
            _setupHandler = setupHandler ?? throw new ArgumentNullException(nameof(setupHandler));
            _managementHandler = managementHandler ?? throw new ArgumentNullException(nameof(managementHandler));
            _statusObserver = statusObserver;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statusObserver.Attach(this);
            LoadCommand = CreateLoadCommand();
            AddRootCommand = CreateAddRootCommand();
            RunAllCommand = CreateRunAllCommand();
            RootPrimaryActionCommand = CreateRootPrimaryActionCommand();
            StopRootCommand = CreateStopRootCommand();
            PauseRootCommand = CreatePauseRootCommand();
            ResumeRootCommand = CreateResumeRootCommand();
        }

        private AsyncRelayCommand CreateLoadCommand()
        {
            return new AsyncRelayCommand(
                cancellationToken => AsyncCommandExecution.RunAsync(
                    token => _loadingHandler.LoadAsync(this, token),
                    LogUnhandledCommandException,
                    cancellationToken),
                () => !IsBusy);
        }

        private AsyncRelayCommand CreateAddRootCommand()
        {
            return new AsyncRelayCommand(
                cancellationToken => AsyncCommandExecution.RunAsync(
                    token => _setupHandler.AddRootAsync(this, token),
                    LogUnhandledCommandException,
                    cancellationToken),
                CanAddRoot);
        }

        private AsyncRelayCommand CreateRunAllCommand()
        {
            return new AsyncRelayCommand(
                cancellationToken => AsyncCommandExecution.RunAsync(
                    token => _executionHandler.RunAllAsync(this, token),
                    LogUnhandledCommandException,
                    cancellationToken),
                CanRunAll);
        }

        private AsyncRelayCommand<CottonSyncRootListItem> CreateRootPrimaryActionCommand()
        {
            return new AsyncRelayCommand<CottonSyncRootListItem>(
                (item, cancellationToken) => AsyncCommandExecution.RunAsync(
                    item,
                    ExecuteRootPrimaryActionAsync,
                    LogUnhandledCommandException,
                    cancellationToken),
                item => !IsBusy && item is not null && item.CanUsePrimaryAction);
        }

        private AsyncRelayCommand<CottonSyncRootListItem> CreateStopRootCommand()
        {
            return new AsyncRelayCommand<CottonSyncRootListItem>(
                (item, cancellationToken) => AsyncCommandExecution.RunAsync(
                    item,
                    (root, token) => _managementHandler.StopRootAsync(this, root, token),
                    LogUnhandledCommandException,
                    cancellationToken),
                item => !IsBusy && item is not null && item.CanStopSync);
        }

        private AsyncRelayCommand<CottonSyncRootListItem> CreatePauseRootCommand()
        {
            return new AsyncRelayCommand<CottonSyncRootListItem>(
                (item, cancellationToken) => AsyncCommandExecution.RunAsync(
                    item,
                    (root, token) => _managementHandler.SetRootPausedAsync(
                        this,
                        root,
                        isPaused: true,
                        token),
                    LogUnhandledCommandException,
                    cancellationToken),
                item => !IsBusy && item is not null && item.CanPauseSync);
        }

        private AsyncRelayCommand<CottonSyncRootListItem> CreateResumeRootCommand()
        {
            return new AsyncRelayCommand<CottonSyncRootListItem>(
                (item, cancellationToken) => AsyncCommandExecution.RunAsync(
                    item,
                    (root, token) => _managementHandler.SetRootPausedAsync(
                        this,
                        root,
                        isPaused: false,
                        token),
                    LogUnhandledCommandException,
                    cancellationToken),
                item => !IsBusy && item is not null && item.CanResumeSync);
        }

        public IAsyncRelayCommand LoadCommand { get; }

        public IAsyncRelayCommand AddRootCommand { get; }

        public IAsyncRelayCommand RunAllCommand { get; }

        public IAsyncRelayCommand<CottonSyncRootListItem> RootPrimaryActionCommand { get; }

        public IAsyncRelayCommand<CottonSyncRootListItem> StopRootCommand { get; }

        public IAsyncRelayCommand<CottonSyncRootListItem> PauseRootCommand { get; }

        public IAsyncRelayCommand<CottonSyncRootListItem> ResumeRootCommand { get; }

        public RangeObservableCollection<CottonSyncRootListItem> Roots { get; } = [];

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoadCommand.NotifyCanExecuteChanged();
                    AddRootCommand.NotifyCanExecuteChanged();
                    RunAllCommand.NotifyCanExecuteChanged();
                    RootPrimaryActionCommand.NotifyCanExecuteChanged();
                    StopRootCommand.NotifyCanExecuteChanged();
                    PauseRootCommand.NotifyCanExecuteChanged();
                    ResumeRootCommand.NotifyCanExecuteChanged();
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
                    OnPropertyChanged(nameof(HeaderSupportingText));
                    OnPropertyChanged(nameof(IsHeaderSupportingTextVisible));
                }
            }
        }

        public string HeaderSupportingText
        {
            get
            {
                string? status = Status;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    return status;
                }

                return string.IsNullOrWhiteSpace(_automaticStatus) ? _summaryText : _automaticStatus;
            }
        }

        public bool IsHeaderSupportingTextVisible =>
            !IsEmptyVisible || !string.IsNullOrWhiteSpace(Status) || !string.IsNullOrWhiteSpace(_automaticStatus);

        public bool IsEmptyVisible
        {
            get => _isEmptyVisible;
            private set
            {
                if (SetProperty(ref _isEmptyVisible, value))
                {
                    OnPropertyChanged(nameof(IsListVisible));
                    OnPropertyChanged(nameof(IsHeaderSupportingTextVisible));
                }
            }
        }

        public bool IsListVisible => !IsEmptyVisible;

        public bool IsRunAllVisible => _canRunAll;

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
        }

        public void Clear()
        {
            _instanceUri = null;
            _accountScopeKey = null;
            Roots.ReplaceWith([]);
            _summaryText = AppResources.NoFoldersSyncing;
            Status = null;
            SetAutomaticStatus(null);
            IsEmptyVisible = true;
            _canRunAll = false;
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
            Roots.ReplaceWith(state.Items);
            _statusObserver.RefreshProgress();

            _summaryText = state.SummaryText;
            OnPropertyChanged(nameof(HeaderSupportingText));
            SetAutomaticStatus(CottonAutomaticSyncStatusText.Create([.. collection.AutomaticSyncStatuses.Values]));
            IsEmptyVisible = state.IsEmptyVisible;
            bool canRunAllChanged = _canRunAll != state.CanRunAny;
            _canRunAll = state.CanRunAny;
            if (canRunAllChanged)
            {
                OnPropertyChanged(nameof(IsRunAllVisible));
            }

            RunAllCommand.NotifyCanExecuteChanged();
        }

        private bool CanRunAll()
        {
            return !IsBusy && _canRunAll;
        }

        private bool CanAddRoot()
        {
            return !IsBusy && _instanceUri is not null && !string.IsNullOrWhiteSpace(_accountScopeKey);
        }

        private Task ExecuteRootPrimaryActionAsync(
            CottonSyncRootListItem item,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.CanReconnect)
            {
                return _setupHandler.ReconnectRootAsync(this, item, cancellationToken);
            }

            return _executionHandler.ExecutePrimaryActionAsync(this, item, cancellationToken);
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            CottonLog.Error(_logger, "Unhandled Cotton mobile sync settings command failure.", exception);
            Status = AppResources.SyncSettingsUpdateFailed;
        }

        private void SetAutomaticStatus(string? status)
        {
            if (string.Equals(_automaticStatus, status, StringComparison.Ordinal))
            {
                return;
            }

            _automaticStatus = status;
            OnPropertyChanged(nameof(HeaderSupportingText));
            OnPropertyChanged(nameof(IsHeaderSupportingTextVisible));
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

        string? ISyncSettingsViewState.AutomaticStatus
        {
            get => _automaticStatus;
            set => SetAutomaticStatus(value);
        }

        IReadOnlyList<CottonSyncRootListItem> ISyncSettingsViewState.Roots => Roots;

        void ISyncSettingsViewState.ShowRoots(SyncRootCollectionSnapshot collection)
        {
            ShowRoots(collection);
        }
    }
}
