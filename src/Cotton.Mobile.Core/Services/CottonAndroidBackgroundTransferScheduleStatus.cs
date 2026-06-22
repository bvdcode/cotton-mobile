// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonAndroidBackgroundTransferScheduleStatus
    {
        Scheduled = 0,
        ForegroundRequired = 1,
        Unsupported = 2,
        NoQueuedTransfer = 3,
    }
}
