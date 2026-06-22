// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidBackgroundSyncHost
    {
        Task<CottonAndroidBackgroundSyncScheduleResult> ScheduleAsync(
            CottonAndroidBackgroundSyncRequest request,
            CancellationToken cancellationToken = default);
    }
}
