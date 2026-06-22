// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonCloudShareLinkPolicy
    {
        public const int DefaultExpireAfterMinutes = 1440;
        public const int MaxExpireAfterMinutes = 60 * 24 * 365;

        public static void EnsureValidExpireAfterMinutes(int expireAfterMinutes)
        {
            if (expireAfterMinutes <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expireAfterMinutes),
                    "Share link lifetime must be positive.");
            }

            if (expireAfterMinutes > MaxExpireAfterMinutes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expireAfterMinutes),
                    "Share link lifetime cannot exceed one year.");
            }
        }
    }
}
