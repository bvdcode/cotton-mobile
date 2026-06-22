// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonRemotePushDeviceTokenService
    {
        Task<CottonRemotePushDeviceTokenSnapshot> RegisterCurrentAsync(
            Uri instanceUri,
            CottonRemotePushDeviceTokenRegistrationRequest request,
            CancellationToken cancellationToken = default);

        Task<CottonRemotePushDeviceTokenRevocationResult> RevokeCurrentSessionAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
