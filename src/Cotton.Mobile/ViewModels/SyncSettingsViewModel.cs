// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsViewModel : ViewModelBase, ISyncSettingsViewState
    {
        private readonly SyncSettingsLoadingHandler _loadingHandler;
        private readonly SyncSettingsExecutionHandler _executionHandler;
        private readonly SyncSettingsSetupHandler _setupHandler;
        private readonly SyncSettingsManagementHandler _managementHandler;
        private readonly ILogger<SyncSettingsViewModel> _logger;
        private Uri? _instanceUri;
        private string? _accountScopeKey;
        private bool _isBusy;
        private bool _canRunAll;
        private string _summaryText = "No folders syncing";
        private string? _status;
        private bool _isEmptyVisible = true;

        public SyncSettingsViewModel(
            SyncSettingsLoadingHandler loadingHandler,
            SyncSettingsExecutionHandler executionHandler,
            SyncSettingsSetupHandler setupHandler,
            SyncSettingsManagementHandler managementHandler,
            ILogger<SyncSettingsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(loadingHandler);
            ArgumentNullException.ThrowIfNull(executionHandler);
            ArgumentNullException.ThrowIfNull(setupHandler);
            ArgumentNullException.ThrowIfNull(managementHandler);
            ArgumentNullException.ThrowIfNull(logger);

            _loadingHandler = loadingHandler;
            _executionHandler = executionHandler;
            _setupHandler = setupHandler;
            _managementHandler = managementHandler;
            _logger = logger;
            LoadCommand = new AsyncCommand(
                () => _loadingHandler.LoadAsync(this),
                LogUnhandledCommandException,
                () => !IsBusy);
            AddRootCommand = new AsyncCommand(
                () => _setupHandler.AddRootAsync(this),
                LogUnhandledCommandException,
                CanAddRoot);
            RunAllCommand = new AsyncCommand(
                () => _executionHandler.RunAllAsync(this),
                LogUnhandledCommandException,
                CanRunAll);
            RootPrimaryActionCommand = new AsyncCommand<CottonSyncRootListItem>(
                ExecuteRootPrimaryActionAsync,
                LogUnhandledCommandException,
                item => !IsBusy && item.CanUsePrimaryAction);
            StopRootCommand = new AsyncCommand<CottonSyncRootListItem>(
                item => _managementHandler.StopRootAsync(this, item),
                LogUnhandledCommandException,
                item => !IsBusy && item.CanStopSync);
            PauseRootCommand = new AsyncCommand<CottonSyncRootListItem>(
                item => _managementHandler.SetRootPausedAsync(this, item, isPaused: true),
                LogUnhandledCommandException,
                item => !IsBusy && item.CanPauseSync);
            ResumeRootCommand = new AsyncCommand<CottonSyncRootListItem>(
                item => _managementHandler.SetRootPausedAsync(this, item, isPaused: false),
                LogUnhandledCommandException,
                item => !IsBusy && item.CanResumeSync);
        }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand AddRootCommand { get; }

        public AsyncCommand RunAllCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> RootPrimaryActionCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> StopRootCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> PauseRootCommand { get; }

        public AsyncCommand<CottonSyncRootListItem> ResumeRootCommand { get; }

        public RangeObservableCollection<CottonSyncRootListItem> Roots { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    LoadCommand.RaiseCanExecuteChanged();
                    AddRootCommand.RaiseCanExecuteChanged();
                    RunAllCommand.RaiseCanExecuteChanged();
                    RootPrimaryActionCommand.RaiseCanExecuteChanged();
                    StopRootCommand.RaiseCanExecuteChanged();
                    PauseRootCommand.RaiseCanExecuteChanged();
                    ResumeRootCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SummaryText
        {
            get => _summaryText;
            private set
            {
                if (SetProperty(ref _summaryText, value))
                {
                    OnPropertyChanged(nameof(HeaderSupportingText));
                }
            }
        }

        public string? Status
        {
            get => _status;
            private set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(IsStatusVisible));
                    OnPropertyChanged(nameof(HeaderSupportingText));
                    OnPropertyChanged(nameof(IsHeaderSupportingTextVisible));
                }
            }
        }

        public bool IsStatusVisible => !string.IsNullOrWhiteSpace(Status);

        public string HeaderSupportingText
        {
            get
            {
                string? status = Status;
                return string.IsNullOrWhiteSpace(status) ? SummaryText : status;
            }
        }

        public bool IsHeaderSupportingTextVisible => IsStatusVisible || IsSummaryVisible;

        public bool IsEmptyVisible
        {
            get => _isEmptyVisible;
            private set
            {
                if (SetProperty(ref _isEmptyVisible, value))
                {
                    OnPropertyChanged(nameof(IsSummaryVisible));
                    OnPropertyChanged(nameof(IsListVisible));
                    OnPropertyChanged(nameof(IsHeaderSupportingTextVisible));
                }
            }
        }

        public bool IsSummaryVisible => !IsEmptyVisible;

        public bool IsListVisible => !IsEmptyVisible;

        public bool IsRunAllVisible => _canRunAll;

        public void Configure(Uri instanceUri, string accountScopeKey)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);

            _instanceUri = instanceUri;
            _accountScopeKey = accountScopeKey.Trim();
            AddRootCommand.RaiseCanExecuteChanged();
        }

        public async Task LoadForInstanceAsync(Uri instanceUri, string accountScopeKey)
        {
            Configure(instanceUri, accountScopeKey);
            await _loadingHandler.LoadAsync(this);
        }

        public void Clear()
        {
            _instanceUri = null;
            _accountScopeKey = null;
            Roots.ReplaceWith([]);
            SummaryText = "No folders syncing";
            Status = null;
            IsEmptyVisible = true;
            _canRunAll = false;
            OnPropertyChanged(nameof(IsRunAllVisible));
            RunAllCommand.RaiseCanExecuteChanged();
            AddRootCommand.RaiseCanExecuteChanged();
        }

        private void ShowRoots(SyncRootCollectionSnapshot collection)
        {
            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create(
                collection.Roots,
                collection.PausedRootIds);
            Roots.ReplaceWith(state.Items);

            SummaryText = state.SummaryText;
            IsEmptyVisible = state.IsEmptyVisible;
            bool canRunAllChanged = _canRunAll != state.CanRunAny;
            _canRunAll = state.CanRunAny;
            if (canRunAllChanged)
            {
                OnPropertyChanged(nameof(IsRunAllVisible));
            }

            RunAllCommand.RaiseCanExecuteChanged();
        }

        private bool CanRunAll()
        {
            return !IsBusy && _canRunAll;
        }

        private bool CanAddRoot()
        {
            return !IsBusy && _instanceUri is not null && !string.IsNullOrWhiteSpace(_accountScopeKey);
        }

        private Task ExecuteRootPrimaryActionAsync(CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.CanReconnect)
            {
                return _setupHandler.ReconnectRootAsync(this, item);
            }

            return _executionHandler.ExecutePrimaryActionAsync(this, item);
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile sync settings command failure.");
            Status = "Could not update sync settings.";
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

        IReadOnlyList<CottonSyncRootListItem> ISyncSettingsViewState.Roots => Roots;

        void ISyncSettingsViewState.ShowRoots(SyncRootCollectionSnapshot collection)
        {
            ShowRoots(collection);
        }
    }
}
