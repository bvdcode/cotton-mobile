// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupPlanningService
    {
        Task<CottonCameraBackupPlanSnapshot> PlanAsync(
            Uri instanceUri,
            CottonCameraBackupSettings settings,
            CancellationToken cancellationToken = default);
    }
}
