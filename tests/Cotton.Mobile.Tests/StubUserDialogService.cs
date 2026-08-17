// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class StubUserDialogService : IUserDialogService
    {
        public Task ShowAlertAsync(string title, string message, string cancel)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            return Task.FromResult(false);
        }

        public Task<string?> ShowActionSheetAsync(
            string title,
            string cancel,
            string? destruction,
            IReadOnlyList<string> actions)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
