// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonAutomaticSyncStatusText
    {
        public static string? Create(
            IReadOnlyCollection<CottonAutomaticSyncRootStatusSnapshot> statuses)
        {
            ArgumentNullException.ThrowIfNull(statuses);
            CottonAutomaticSyncRootStatusSnapshot[] failedStatuses = [.. statuses
                .Where(status => status.Outcome == CottonAutomaticSyncOutcome.Failed)];
            if (failedStatuses.Length > 0)
            {
                DateTime latestFailure = failedStatuses.Max(status => status.CompletedAtUtc).ToLocalTime();
                return failedStatuses.Length == 1
                    ? CoreResources.Format(CoreResources.LastSyncFailedFormat, latestFailure)
                    : CoreResources.Format(
                        CoreResources.SyncFailuresFormat,
                        failedStatuses.Length,
                        latestFailure);
            }

            if (statuses.Count == 0)
            {
                return null;
            }

            DateTime latestSuccess = statuses.Max(status => status.CompletedAtUtc).ToLocalTime();
            return CoreResources.Format(CoreResources.LastSyncSucceededFormat, latestSuccess);
        }
    }
}
