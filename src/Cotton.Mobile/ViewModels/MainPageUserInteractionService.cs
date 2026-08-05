// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.ViewModels
{
    public class MainPageUserInteractionService
    {
        private readonly IBrowser _browser;
        private readonly CottonMobileOptions _options;
        private readonly IUserDialogService _dialogService;
        private readonly ILogger<MainPageUserInteractionService> _logger;

        public MainPageUserInteractionService(
            IBrowser browser,
            CottonMobileOptions options,
            IUserDialogService dialogService,
            ILogger<MainPageUserInteractionService> logger)
        {
            ArgumentNullException.ThrowIfNull(browser);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _browser = browser;
            _options = options;
            _dialogService = dialogService;
            _logger = logger;
        }

        public Task<bool> ConfirmSignOutAsync()
        {
            return _dialogService.ShowConfirmationAsync(
                "Sign out?",
                "You will need to approve this device again to reconnect.",
                "Sign out",
                "Cancel");
        }

        public async Task OpenPrivacyPolicyAsync()
        {
            try
            {
                bool opened = await MainThread.InvokeOnMainThreadAsync(
                    () => _browser.OpenAsync(
                        _options.PrivacyPolicyUri,
                        CottonBrowserLaunchOptions.SystemPreferred()));
                if (opened)
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open the Cotton Cloud privacy policy.");
            }

            await _dialogService.ShowAlertAsync(
                "Privacy Policy",
                "Could not open the privacy policy.",
                "OK");
        }
    }
}
