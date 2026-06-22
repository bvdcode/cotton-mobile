// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonFileVersionHistoryService
    {
        Task<CottonFileVersionListSnapshot> GetVersionsAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            TimeZoneInfo displayTimeZone,
            CancellationToken cancellationToken = default);
    }
}
