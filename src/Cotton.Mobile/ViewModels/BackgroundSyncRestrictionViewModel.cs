// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class BackgroundSyncRestrictionViewModel : ObservableObject, IDisposable
    {
        private readonly IApplicationForegroundService _foregroundService;
        private readonly IBackgroundSyncRestrictionService _restrictionService;
        private readonly ILogger<BackgroundSyncRestrictionViewModel> _logger;
        private bool _isRestricted;
        private bool _isAutomaticSyncEnabled;

        public BackgroundSyncRestrictionViewModel(
            IApplicationForegroundService foregroundService,
            IBackgroundSyncRestrictionService restrictionService,
            ILogger<BackgroundSyncRestrictionViewModel> logger)
        {
            _foregroundService = foregroundService
                ?? throw new ArgumentNullException(nameof(foregroundService));
            _restrictionService = restrictionService
                ?? throw new ArgumentNullException(nameof(restrictionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            OpenSettingsCommand = new AsyncRelayCommand(
                cancellationToken => AsyncCommandExecution.RunAsync(
                    _restrictionService.OpenSettingsAsync,
                    LogSettingsFailure,
                    cancellationToken));
            _foregroundService.Resumed += OnApplicationResumed;
            Refresh();
        }

        public IAsyncRelayCommand OpenSettingsCommand { get; }

        public bool IsVisible => _isRestricted && _isAutomaticSyncEnabled;

        public void SetAutomaticSyncEnabled(bool isEnabled)
        {
            if (_isAutomaticSyncEnabled == isEnabled)
            {
                return;
            }

            _isAutomaticSyncEnabled = isEnabled;
            OnPropertyChanged(nameof(IsVisible));
        }

        public void Dispose()
        {
            _foregroundService.Resumed -= OnApplicationResumed;
            GC.SuppressFinalize(this);
        }

        private void OnApplicationResumed(object? sender, EventArgs eventArgs)
        {
            Refresh();
        }

        private void Refresh()
        {
            try
            {
                SetRestrictionState(_restrictionService.IsRestricted);
            }
            catch (Exception exception)
            {
                SetRestrictionState(isRestricted: false);
                CottonLog.Warning(
                    _logger,
                    "Failed to inspect Android background sync restrictions.",
                    exception);
            }
        }

        private void SetRestrictionState(bool isRestricted)
        {
            if (_isRestricted == isRestricted)
            {
                return;
            }

            _isRestricted = isRestricted;
            OnPropertyChanged(nameof(IsVisible));
        }

        private void LogSettingsFailure(Exception exception)
        {
            CottonLog.Warning(
                _logger,
                "Failed to open Android background sync settings.",
                exception);
        }
    }
}
