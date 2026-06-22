// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonSessionResultStatus
    {
        Unauthenticated = 0,
        Authenticated = 1,
        AuthorizationDenied = 2,
        AuthorizationExpired = 3,
        AuthorizationNotFound = 4,
        BrowserUnavailable = 5,
        TimedOut = 6,
        AuthorizationFailed = 7,
        SessionExpired = 8,
        AuthorizationPending = 9,
    }
}
