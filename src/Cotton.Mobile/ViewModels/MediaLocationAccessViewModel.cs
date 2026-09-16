// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cotton.Mobile.Commands;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class MediaLocationAccessViewModel : ObservableObject, IDisposable
    {
        private readonly IApplicationForegroundService _foregroundService;
        private readonly ICottonMediaLocationPermissionService _permissionService;
        private readonly ILogger<MediaLocationAccessViewModel> _logger;
        private bool _isGranted;

        public MediaLocationAccessViewModel(
            IApplicationForegroundService foregroundService,
            ICottonMediaLocationPermissionService permissionService,
            ILogger<MediaLocationAccessViewModel> logger)
        {
            _foregroundService = foregroundService;
            _permissionService = permissionService;
            _logger = logger;
            RequestCommand = new AsyncRelayCommand(cancellationToken => AsyncCommandExecution.RunAsync(
                RequestAsync,
                LogFailure,
                cancellationToken));
            _foregroundService.Resumed += OnApplicationResumed;
            Refresh();
        }

        public IAsyncRelayCommand RequestCommand { get; }

        public bool IsSupported => _permissionService.IsSupported;

        public bool IsGranted => _isGranted;

        public bool IsPermissionNeeded => IsSupported && !IsGranted;

        public string ActionText => _permissionService.CanRequest
            ? MediaLocationResources.AllowText
            : MediaLocationResources.SettingsText;

        public void Dispose()
        {
            _foregroundService.Resumed -= OnApplicationResumed;
            GC.SuppressFinalize(this);
        }

        public void Refresh()
        {
            SetProperty(ref _isGranted, _permissionService.IsGranted, nameof(IsGranted));
            OnPropertyChanged(nameof(IsPermissionNeeded));
            OnPropertyChanged(nameof(ActionText));
        }

        private void OnApplicationResumed(object? sender, EventArgs eventArgs)
        {
            Refresh();
        }

        private async Task RequestAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _permissionService.RequestAsync(cancellationToken);
            }
            finally
            {
                Refresh();
            }
        }

        private void LogFailure(Exception exception)
        {
            CottonLog.Warning(_logger, "Failed to request access to media location data.", exception);
        }
    }
}
