// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonDeviceUnlockService
    {
        Task<CottonDeviceUnlockAvailabilitySnapshot> GetAvailabilityAsync(
            CancellationToken cancellationToken = default);

        Task<CottonDeviceUnlockResult> RequestUnlockAsync(
            CancellationToken cancellationToken = default);
    }
}
