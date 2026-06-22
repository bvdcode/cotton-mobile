// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonCameraBackupMediaAccessRules
    {
        public static bool CanReadAnyMedia(CottonCameraBackupMediaAccessState state)
        {
            return state is CottonCameraBackupMediaAccessState.Allowed
                or CottonCameraBackupMediaAccessState.Limited;
        }

        public static bool CanScanFullLibrary(CottonCameraBackupMediaAccessState state)
        {
            return state == CottonCameraBackupMediaAccessState.Allowed;
        }
    }
}
