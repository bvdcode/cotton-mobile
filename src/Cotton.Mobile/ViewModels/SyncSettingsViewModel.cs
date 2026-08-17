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
        private string? _status;
        private long _statusRevision;
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
            RootActionCommand = CreateRootActionCommand();
            ToggleRootDeleteModeCommand = new RelayCommand<CottonSyncRootListItem>(
                ToggleRootDeleteMode,
                CanToggleRootDeleteMode);
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

        private AsyncRelayCommand<CottonSyncRootActionRequest> CreateRootActionCommand()
        {
            return new AsyncRelayCommand<CottonSyncRootActionRequest>(
                (request, cancellationToken) => AsyncCommandExecution.RunAsync(
                    request,
                    ExecuteRootActionAsync,
                    LogUnhandledCommandException,
                    cancellationToken),
                request => !IsBusy && request is not null && CanExecuteRootAction(request));
        }

        public IAsyncRelayCommand LoadCommand { get; }

        public IAsyncRelayCommand AddRootCommand { get; }

        public IAsyncRelayCommand RunAllCommand { get; }

        public IAsyncRelayCommand<CottonSyncRootActionRequest> RootActionCommand { get; }

        public IRelayCommand<CottonSyncRootListItem> ToggleRootDeleteModeCommand { get; }

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
                    RootActionCommand.NotifyCanExecuteChanged();
                    ToggleRootDeleteModeCommand.NotifyCanExecuteChanged();
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

        public string HeaderSupportingText => Status ?? string.Empty;

        public bool IsHeaderSupportingTextVisible => !string.IsNullOrWhiteSpace(Status);

        public bool IsEmptyVisible
        {
            get => _isEmptyVisible;
            private set
            {
                if (SetProperty(ref _isEmptyVisible, value))
                {
                    OnPropertyChanged(nameof(IsListVisible));
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
            Status = null;
            IsEmptyVisible = true;
            _canRunAll = false;
            OnPropertyChanged(nameof(IsRunAllVisible));
            RunAllCommand.NotifyCanExecuteChanged();
            AddRootCommand.NotifyCanExecuteChanged();
            ToggleRootDeleteModeCommand.NotifyCanExecuteChanged();
        }

        private void ShowRoots(SyncRootCollectionSnapshot collection)
        {
            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create(
                collection.Roots,
                collection.PausedRootIds,
                collection.AutomaticSyncStatuses);
            Roots.ReplaceWith(state.Items);
            _statusObserver.RefreshProgress();

            IsEmptyVisible = state.IsEmptyVisible;
            bool canRunAllChanged = _canRunAll != state.CanRunAny;
            _canRunAll = state.CanRunAny;
            if (canRunAllChanged)
            {
                OnPropertyChanged(nameof(IsRunAllVisible));
            }

            RunAllCommand.NotifyCanExecuteChanged();
            ToggleRootDeleteModeCommand.NotifyCanExecuteChanged();
        }

        private bool CanRunAll()
        {
            return !IsBusy && _canRunAll;
        }

        private bool CanAddRoot()
        {
            return !IsBusy && _instanceUri is not null && !string.IsNullOrWhiteSpace(_accountScopeKey);
        }

        private static bool CanExecuteRootAction(CottonSyncRootActionRequest request)
        {
            return request.Action switch
            {
                CottonSyncRootAction.ShowFailureDetails => request.Item.CanShowFailureDetails,
                CottonSyncRootAction.UsePrimaryAction => request.Item.CanUsePrimaryAction,
                CottonSyncRootAction.Pause => request.Item.CanPauseSync,
                CottonSyncRootAction.Resume => request.Item.CanResumeSync,
                CottonSyncRootAction.Delete => request.Item.CanDeleteSync,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.Action,
                    "Sync-root action is not supported."),
            };
        }

        private async Task ExecuteRootActionAsync(
            CottonSyncRootActionRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            CottonSyncRootListItem item = request.Item;
            ClearRootDeleteModes();
            switch (request.Action)
            {
                case CottonSyncRootAction.ShowFailureDetails:
                    await _managementHandler.ShowFailureDetailsAsync(item);
                    break;

                case CottonSyncRootAction.UsePrimaryAction when item.CanReconnect:
                    await _setupHandler.ReconnectRootAsync(this, item, cancellationToken);
                    break;

                case CottonSyncRootAction.UsePrimaryAction:
                    await _executionHandler.ExecutePrimaryActionAsync(this, item, cancellationToken);
                    break;

                case CottonSyncRootAction.Pause:
                    await _managementHandler.SetRootPausedAsync(this, item, isPaused: true, cancellationToken);
                    break;

                case CottonSyncRootAction.Resume:
                    await _managementHandler.SetRootPausedAsync(this, item, isPaused: false, cancellationToken);
                    break;

                case CottonSyncRootAction.Delete:
                    await _managementHandler.DeleteRootAsync(this, item, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException("Sync-root action is not supported.");
            }
        }

        private bool CanToggleRootDeleteMode(CottonSyncRootListItem? item)
        {
            return !IsBusy && item is not null && Roots.Contains(item);
        }

        private void ToggleRootDeleteMode(CottonSyncRootListItem? item)
        {
            ArgumentNullException.ThrowIfNull(item);
            bool enableDeleteMode = !item.IsDeleteMode;
            foreach (CottonSyncRootListItem root in Roots)
            {
                root.SetDeleteMode(enableDeleteMode && ReferenceEquals(root, item));
            }
        }

        private void ClearRootDeleteModes()
        {
            foreach (CottonSyncRootListItem root in Roots)
            {
                root.SetDeleteMode(false);
            }
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            CottonLog.Error(_logger, "Unhandled Cotton mobile sync settings command failure.", exception);
            Status = AppResources.SyncSettingsUpdateFailed;
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
