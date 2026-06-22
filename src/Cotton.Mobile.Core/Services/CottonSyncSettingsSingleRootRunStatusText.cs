// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncSettingsSingleRootRunStatusText
    {
        public static string CreateFinishedStatus(CottonCloudToDeviceSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            return CottonCloudToDeviceSyncStatusText.CreateCompletedStatus(summary);
        }

        public static string CreateFinishedStatus(CottonDeviceToCloudSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            return summary.NeedsDestructiveReview
                ? CottonDeviceToCloudSyncStatusText.CancelledStatus
                : CottonDeviceToCloudSyncStatusText.CreateCompletedStatus(summary);
        }

        public static string CreateFinishedStatus(CottonBidirectionalSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            if (summary.NeedsConflictReview)
            {
                return CottonBidirectionalSyncStatusText.ConflictReviewRequiredStatus;
            }

            if (summary.NeedsDestructiveReview)
            {
                return CottonBidirectionalSyncStatusText.CancelledStatus;
            }

            return summary.BlockedItemCount > 0
                ? CottonBidirectionalSyncStatusText.BlockedReviewRequiredStatus
                : CottonBidirectionalSyncStatusText.CreateCompletedStatus(summary);
        }
    }
}
