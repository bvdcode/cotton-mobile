// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.ViewModels
{
    using Cotton.Mobile.Services;

    public static class ViewerFileActionFailureStatus
    {
        private const string MissingFileStatus = "File no longer available.";
        private const string OpenUnavailableStatus = CottonFileOpenRouter.OpenUnavailableStatus;

        public static string Create(Exception exception, string fallbackStatus)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentException.ThrowIfNullOrWhiteSpace(fallbackStatus);

            return exception switch
            {
                FileNotFoundException => MissingFileStatus,
                FileOpenUnavailableException => OpenUnavailableStatus,
                _ => fallbackStatus,
            };
        }
    }
}
