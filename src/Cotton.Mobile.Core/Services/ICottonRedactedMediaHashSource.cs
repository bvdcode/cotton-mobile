// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonRedactedMediaHashSource
    {
        bool IsSupported { get; }

        bool CanReadOriginals { get; }

        Task<string?> ComputeAsync(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalItemSnapshot file,
            CancellationToken cancellationToken = default);
    }
}
