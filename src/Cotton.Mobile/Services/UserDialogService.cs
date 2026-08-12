// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using UraniumUI.Dialogs;

namespace Cotton.Mobile.Services
{
    public class UserDialogService : IUserDialogService
    {
        private readonly IDialogService _dialogService;
        private readonly ILogger<UserDialogService> _logger;

        public UserDialogService(
            IDialogService dialogService,
            ILogger<UserDialogService> logger)
        {
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(logger);

            _dialogService = dialogService;
            _logger = logger;
        }

        public async Task ShowAlertAsync(string title, string message, string cancel)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Label content = new()
                    {
                        Text = message,
                        Margin = 20,
                    };

                    await _dialogService.DisplayViewAsync(title, content, cancel);
                });
            }
            catch (Exception exception)
            {
                CottonLog.WarningWithContext(
                    _logger,
                    "Failed to show a Cotton mobile alert dialog.",
                    title,
                    exception);
            }
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    return await _dialogService.ConfirmAsync(title, message, accept, cancel);
                });
            }
            catch (Exception exception)
            {
                CottonLog.WarningWithContext(
                    _logger,
                    "Failed to show a Cotton mobile confirmation dialog.",
                    title,
                    exception);
                return false;
            }
        }
    }
}
