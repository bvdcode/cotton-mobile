// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface IUserDialogService
    {
        Task ShowAlertAsync(string title, string message, string cancel);

        Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);

        Task<string?> ShowPromptAsync(
            string title,
            string message,
            string accept,
            string cancel,
            string? initialValue = null,
            int maxLength = -1);

        Task<string?> ShowActionSheetAsync(
            string title,
            string cancel,
            string? destruction,
            params string[] buttons);
    }
}
