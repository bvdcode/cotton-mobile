// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class UnavailableCottonDeviceUnlockService : ICottonDeviceUnlockService
    {
        private const string UnavailableDetailText = "Device unlock is not available on this platform.";

        public Task<CottonDeviceUnlockAvailabilitySnapshot> GetAvailabilityAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(CottonDeviceUnlockAvailabilitySnapshot.Unavailable(UnavailableDetailText));
        }

        public Task<CottonDeviceUnlockResult> RequestUnlockAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(CottonDeviceUnlockResult.Unavailable(UnavailableDetailText));
        }
    }
}
