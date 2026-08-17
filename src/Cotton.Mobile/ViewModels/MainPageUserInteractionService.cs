// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
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
                AppResources.SignOutQuestion,
                AppResources.SignOutConfirmation,
                AppResources.SignOutText,
                AppResources.CancelText);
        }

        public Task<bool> ConfirmInsecureConnectionAsync(Uri instanceUri)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            return _dialogService.ShowConfirmationAsync(
                AppResources.InsecureConnectionTitle,
                AppResources.CreateInsecureConnectionMessage(instanceUri),
                AppResources.ContinueText,
                AppResources.ChangeServerText);
        }

        public async Task OpenPrivacyPolicyAsync()
        {
            await OpenExternalUriAsync(
                _options.PrivacyPolicyUri,
                "Failed to open the Cotton Cloud privacy policy.",
                AppResources.PrivacyPolicyTitle,
                AppResources.PrivacyPolicyOpenFailed);
        }

        public async Task OpenRepositoryAsync()
        {
            await OpenExternalUriAsync(
                _options.RepositoryUri,
                "Failed to open the Cotton Cloud source repository.",
                AppResources.RepositoryTitle,
                AppResources.RepositoryOpenFailed);
        }

        private async Task OpenExternalUriAsync(
            Uri uri,
            string logMessage,
            string failureTitle,
            string failureMessage)
        {
            try
            {
                bool opened = await MainThread.InvokeOnMainThreadAsync(
                    () => _browser.OpenAsync(
                        uri,
                        CottonBrowserLaunchOptions.SystemPreferred()));
                if (opened)
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, logMessage, exception);
            }

            await _dialogService.ShowAlertAsync(
                failureTitle,
                failureMessage,
                AppResources.OkText);
        }
    }
}
