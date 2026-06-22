// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class UnavailableCottonAppLockCapabilityService : ICottonAppLockCapabilityService
    {
        private const string UnavailableDetailText = "This version cannot require device unlock.";

        public Task<CottonAppLockCapabilitySnapshot> GetCapabilityAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(CottonAppLockCapabilitySnapshot.Unavailable(UnavailableDetailText));
        }
    }
}
