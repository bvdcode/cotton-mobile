// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushDeviceTokenRevocationResult
    {
        public CottonRemotePushDeviceTokenRevocationResult(int revokedTokens)
        {
            if (revokedTokens < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revokedTokens));
            }

            RevokedTokens = revokedTokens;
        }

        public int RevokedTokens { get; }
    }
}
