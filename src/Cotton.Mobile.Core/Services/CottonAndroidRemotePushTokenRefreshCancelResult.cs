// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAndroidRemotePushTokenRefreshCancelResult
    {
        private CottonAndroidRemotePushTokenRefreshCancelResult(
            bool isCancelled,
            string statusText)
        {
            if (string.IsNullOrWhiteSpace(statusText))
            {
                throw new ArgumentException("Remote push token refresh cancel status text is required.", nameof(statusText));
            }

            IsCancelled = isCancelled;
            StatusText = statusText.Trim();
        }

        public bool IsCancelled { get; }

        public string StatusText { get; }

        public static CottonAndroidRemotePushTokenRefreshCancelResult Cancelled()
        {
            return new CottonAndroidRemotePushTokenRefreshCancelResult(
                isCancelled: true,
                "Cancelled Android remote push token refresh.");
        }

        public static CottonAndroidRemotePushTokenRefreshCancelResult Unsupported()
        {
            return new CottonAndroidRemotePushTokenRefreshCancelResult(
                isCancelled: false,
                "Android remote push token refresh cancellation is unavailable on this platform.");
        }
    }
}
