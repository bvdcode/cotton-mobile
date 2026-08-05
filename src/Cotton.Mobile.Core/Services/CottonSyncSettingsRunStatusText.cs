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

            int rootCount = cloudToDeviceSummary.RootCount
                + deviceToCloudSummary.RootCount
                + (bidirectionalSummary?.RootCount ?? 0);
            if (rootCount == 0)
            {
                return CoreResources.NoSyncFolders;
            }

            List<string> parts = [];
            AddCount(parts, cloudToDeviceSummary.DownloadedCount, CoreResources.DownloadedLabel);
            AddCount(parts, cloudToDeviceSummary.RefreshedCount, CoreResources.RefreshedLabel);
            AddCount(parts, cloudToDeviceSummary.RenamedCount, CoreResources.RenamedLabel);
            AddCount(parts, cloudToDeviceSummary.RemovedCount, CoreResources.RemovedLabel);
            AddCount(parts, deviceToCloudSummary.UploadedCount, CoreResources.UploadedLabel);
            AddCount(parts, deviceToCloudSummary.ConfirmedUploadCount, CoreResources.UploadConfirmedSingular, CoreResources.UploadConfirmedPlural);
            AddCount(parts, deviceToCloudSummary.DeletedLocalFileCount, CoreResources.OriginalRemovedSingular, CoreResources.OriginalRemovedPlural);
            AddCount(parts, bidirectionalSummary?.DownloadedCount ?? 0, CoreResources.BidirectionalDownloadedLabel);
            AddCount(parts, bidirectionalSummary?.RefreshedLocalCount ?? 0, CoreResources.BidirectionalRefreshedLocallyLabel);
            AddCount(parts, bidirectionalSummary?.RenamedLocalCount ?? 0, CoreResources.BidirectionalRenamedLocallyLabel);
            AddCount(parts, bidirectionalSummary?.RemovedLocalCount ?? 0, CoreResources.BidirectionalRemovedLocallyLabel);
            AddCount(parts, bidirectionalSummary?.UploadedCount ?? 0, CoreResources.BidirectionalUploadedLabel);
            AddCount(parts, bidirectionalSummary?.RefreshedRemoteCount ?? 0, CoreResources.BidirectionalUpdatedInCloudLabel);
            AddCount(
                parts,
                deviceToCloudSummary.CreatedFolderCount + (bidirectionalSummary?.CreatedFolderCount ?? 0),
                CoreResources.FolderCreatedSingular,
                CoreResources.FolderCreatedPlural);
            AddCount(
                parts,
                bidirectionalSummary?.DeletedRemoteFileCount ?? 0,
                CoreResources.RemoteFileRemovedSingular,
                CoreResources.RemoteFileRemovedPlural);
            AddCount(
                parts,
                bidirectionalSummary?.RemovedManifestCount ?? 0,
                CoreResources.RecordCleanedSingular,
                CoreResources.RecordCleanedPlural);
            AddCount(
                parts,
                bidirectionalSummary?.ConflictReviewCount ?? 0,
                CoreResources.BidirectionalConflictReviewSingular,
                CoreResources.BidirectionalConflictReviewPlural);
            AddCount(
                parts,
                bidirectionalSummary?.DestructiveReviewLocalDeleteCount ?? 0,
                CoreResources.BidirectionalLocalRemovalReviewSingular,
                CoreResources.BidirectionalLocalRemovalReviewPlural);
            AddCount(
                parts,
                bidirectionalSummary?.DestructiveReviewRemoteDeleteCount ?? 0,
                CoreResources.BidirectionalCloudRemovalReviewSingular,
                CoreResources.BidirectionalCloudRemovalReviewPlural);
            AddCount(
                parts,
                cloudToDeviceSummary.BlockedItemCount
                    + deviceToCloudSummary.BlockedItemCount
                    + (bidirectionalSummary?.BlockedItemCount ?? 0),
                CoreResources.BlockedLabel);
            AddRootCount(
                parts,
                cloudToDeviceSummary.SkippedRootCount
                    + deviceToCloudSummary.SkippedRootCount
                    + (bidirectionalSummary?.SkippedRootCount ?? 0));

            if (parts.Count == 0)
            {
                return CoreResources.SyncCurrent;
            }

            return CoreResources.Format(CoreResources.SyncCompletedFormat, string.Join(", ", parts));
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
