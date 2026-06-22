// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonNotificationPermissionService
    {
        Task<CottonNotificationPermissionState> GetPermissionStateAsync(
            CancellationToken cancellationToken = default);

        Task<CottonNotificationPermissionState> RequestPermissionAsync(
            CancellationToken cancellationToken = default);

        Task OpenSettingsAsync(CancellationToken cancellationToken = default);
    }
}
