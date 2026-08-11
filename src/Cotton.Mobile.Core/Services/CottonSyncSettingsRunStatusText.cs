// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonSyncSettingsRunStatusText
    {
        public static string StartingAllStatus => CoreResources.SyncingFolders;

        public static string OfflineUnavailableStatus { get; } =
            CottonCloudToDeviceSyncStatusText.OfflineUnavailableStatus;

        public static string FailedStatus { get; } =
            CottonCloudToDeviceSyncStatusText.FailedStatus;

        public static string CreateCompletedStatus(
            CottonCloudToDeviceSyncRunSummary cloudToDeviceSummary,
            CottonDeviceToCloudSyncRunSummary deviceToCloudSummary,
            CottonBidirectionalSyncRunSummary? bidirectionalSummary = null)
        {
            ArgumentNullException.ThrowIfNull(cloudToDeviceSummary);
            ArgumentNullException.ThrowIfNull(deviceToCloudSummary);

            int rootCount = cloudToDeviceSummary.RootCount + deviceToCloudSummary.RootCount;
            if (bidirectionalSummary is not null)
            {
                rootCount += bidirectionalSummary.RootCount;
            }

            if (rootCount == 0)
            {
                return CoreResources.NoSyncFolders;
            }

            List<string> parts = [];
            AddCloudToDeviceCounts(parts, cloudToDeviceSummary);
            AddDeviceToCloudCounts(parts, deviceToCloudSummary);
            if (bidirectionalSummary is not null)
            {
                AddBidirectionalCounts(parts, bidirectionalSummary);
            }

            AddAggregateCounts(parts, cloudToDeviceSummary, deviceToCloudSummary, bidirectionalSummary);

            if (parts.Count == 0)
            {
                return CoreResources.SyncCurrent;
            }

            return CoreResources.Format(CoreResources.SyncCompletedFormat, string.Join(", ", parts));
        }

        private static void AddCloudToDeviceCounts(
            List<string> parts,
            CottonCloudToDeviceSyncRunSummary summary)
        {
            AddCount(parts, summary.DownloadedCount, CoreResources.DownloadedLabel);
            AddCount(parts, summary.RefreshedCount, CoreResources.RefreshedLabel);
            AddCount(parts, summary.RenamedCount, CoreResources.RenamedLabel);
            AddCount(parts, summary.RemovedCount, CoreResources.RemovedLabel);
        }

        private static void AddDeviceToCloudCounts(
            List<string> parts,
            CottonDeviceToCloudSyncRunSummary summary)
        {
            AddCount(parts, summary.UploadedCount, CoreResources.UploadedLabel);
            AddCount(
                parts,
                summary.ConfirmedUploadCount,
                CoreResources.UploadConfirmedSingular,
                CoreResources.UploadConfirmedPlural);
            AddCount(
                parts,
                summary.DeletedLocalFileCount,
                CoreResources.OriginalRemovedSingular,
                CoreResources.OriginalRemovedPlural);
        }

        private static void AddBidirectionalCounts(
            List<string> parts,
            CottonBidirectionalSyncRunSummary summary)
        {
            AddCount(parts, summary.DownloadedCount, CoreResources.BidirectionalDownloadedLabel);
            AddCount(parts, summary.RefreshedLocalCount, CoreResources.BidirectionalRefreshedLocallyLabel);
            AddCount(parts, summary.RenamedLocalCount, CoreResources.BidirectionalRenamedLocallyLabel);
            AddCount(parts, summary.RemovedLocalCount, CoreResources.BidirectionalRemovedLocallyLabel);
            AddCount(parts, summary.UploadedCount, CoreResources.BidirectionalUploadedLabel);
            AddCount(parts, summary.RefreshedRemoteCount, CoreResources.BidirectionalUpdatedInCloudLabel);
            AddCount(
                parts,
                summary.DeletedRemoteFileCount,
                CoreResources.RemoteFileRemovedSingular,
                CoreResources.RemoteFileRemovedPlural);
            AddCount(
                parts,
                summary.RemovedManifestCount,
                CoreResources.RecordCleanedSingular,
                CoreResources.RecordCleanedPlural);
            AddCount(
                parts,
                summary.ConflictReviewCount,
                CoreResources.BidirectionalConflictReviewSingular,
                CoreResources.BidirectionalConflictReviewPlural);
            AddCount(
                parts,
                summary.DestructiveReviewLocalDeleteCount,
                CoreResources.BidirectionalLocalRemovalReviewSingular,
                CoreResources.BidirectionalLocalRemovalReviewPlural);
            AddCount(
                parts,
                summary.DestructiveReviewRemoteDeleteCount,
                CoreResources.BidirectionalCloudRemovalReviewSingular,
                CoreResources.BidirectionalCloudRemovalReviewPlural);
        }

        private static void AddAggregateCounts(
            List<string> parts,
            CottonCloudToDeviceSyncRunSummary cloudToDeviceSummary,
            CottonDeviceToCloudSyncRunSummary deviceToCloudSummary,
            CottonBidirectionalSyncRunSummary? bidirectionalSummary)
        {
            int bidirectionalCreatedFolderCount = bidirectionalSummary?.CreatedFolderCount ?? 0;
            AddCount(
                parts,
                deviceToCloudSummary.CreatedFolderCount + bidirectionalCreatedFolderCount,
                CoreResources.FolderCreatedSingular,
                CoreResources.FolderCreatedPlural);

            int bidirectionalBlockedItemCount = bidirectionalSummary?.BlockedItemCount ?? 0;
            AddCount(
                parts,
                cloudToDeviceSummary.BlockedItemCount
                    + deviceToCloudSummary.BlockedItemCount
                    + bidirectionalBlockedItemCount,
                CoreResources.BlockedLabel);

            int bidirectionalSkippedRootCount = bidirectionalSummary?.SkippedRootCount ?? 0;
            AddRootCount(
                parts,
                cloudToDeviceSummary.SkippedRootCount
                    + deviceToCloudSummary.SkippedRootCount
                    + bidirectionalSkippedRootCount);
        }

        private static void AddCount(List<string> parts, int count, string label)
        {
            if (count <= 0)
            {
                return;
            }

            parts.Add($"{count} {label}");
        }

        private static void AddCount(List<string> parts, int count, string singularLabel, string pluralLabel)
        {
            if (count <= 0)
            {
                return;
            }

            parts.Add(count == 1 ? $"1 {singularLabel}" : $"{count} {pluralLabel}");
        }

        private static void AddRootCount(List<string> parts, int count)
        {
            if (count <= 0)
            {
                return;
            }

            parts.Add(count == 1
                ? $"1 {CoreResources.RootSkippedSingular}"
                : $"{count} {CoreResources.RootSkippedPlural}");
        }
    }
}
