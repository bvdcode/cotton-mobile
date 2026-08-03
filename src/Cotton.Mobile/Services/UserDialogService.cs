// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class UserDialogService : IUserDialogService
    {
        private readonly ILogger<UserDialogService> _logger;

        public UserDialogService(ILogger<UserDialogService> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public async Task ShowAlertAsync(string title, string message, string cancel)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Page? page = GetCurrentPage();
                    if (page is null)
                    {
                        return;
                    }

                    MaterialDialogPage dialog = MaterialDialogPage.Alert(title, message, cancel);
                    await page.Navigation.PushModalAsync(dialog, animated: false);
                    await dialog.WaitForResultAsync();
                });
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to show Cotton mobile alert dialog {Title}.", title);
            }
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Page? page = GetCurrentPage();
                    if (page is null)
                    {
                        return false;
                    }

                    MaterialDialogPage dialog = MaterialDialogPage.Confirmation(title, message, accept, cancel);
                    await page.Navigation.PushModalAsync(dialog, animated: false);
                    string? result = await dialog.WaitForResultAsync();
                    return result is not null;
                });
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to show Cotton mobile confirmation dialog {Title}.", title);
                return false;
            }
        }

        private static Page? GetCurrentPage()
        {
            return Application.Current?.Windows.FirstOrDefault()?.Page;
        }
    }
}
