// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncSettingsSingleRootRunStatusText
    {
        public static string CreateFinishedStatus(CottonDeviceToCloudSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            return CottonDeviceToCloudSyncStatusText.CreateCompletedStatus(summary);
        }
    }
}
