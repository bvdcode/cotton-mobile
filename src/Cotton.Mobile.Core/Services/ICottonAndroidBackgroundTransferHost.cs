// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidBackgroundTransferHost
    {
        Task<CottonAndroidBackgroundTransferScheduleResult> ScheduleAsync(
            CottonAndroidBackgroundTransferRequest request,
            CancellationToken cancellationToken = default);
    }
}
