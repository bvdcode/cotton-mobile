// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonRemotePushPreferenceService
    {
        Task<CottonRemotePushPreferences> GetCurrentAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task<CottonRemotePushPreferences> UpdateCurrentAsync(
            Uri instanceUri,
            CottonRemotePushPreferences preferences,
            CancellationToken cancellationToken = default);
    }
}
