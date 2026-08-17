// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonAutomaticSyncStatusText
    {
        public static string Create(CottonAutomaticSyncRootStatusSnapshot status)
        {
            ArgumentNullException.ThrowIfNull(status);
            DateTime completedAt = status.CompletedAtUtc.ToLocalTime();
            return status.Outcome switch
            {
                CottonAutomaticSyncOutcome.Succeeded =>
                    CoreResources.Format(CoreResources.LastSyncSucceededFormat, completedAt),
                CottonAutomaticSyncOutcome.Failed =>
                    CoreResources.Format(CoreResources.LastSyncFailedFormat, completedAt),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status.Outcome,
                    "Automatic sync outcome is not supported."),
            };
        }
    }
}
