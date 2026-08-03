// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface IUserDialogService
    {
        Task ShowAlertAsync(string title, string message, string cancel);

        Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);
    }
}
