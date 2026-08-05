// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonBidirectionalSyncStatusText
    {
        public static string ActionLabel => CoreResources.SyncBothWaysAction;

        public static string ConflictReviewRequiredStatus => CoreResources.ConflictReviewRequired;
        public static string BlockedReviewRequiredStatus => CoreResources.BlockedReviewRequired;
        public static string DestructiveReviewRequiredStatus => CoreResources.DestructiveReviewRequired;
        public static string ConfirmDestructiveTitle => CoreResources.ConfirmBidirectionalSyncTitle;
        public static string ConfirmDestructiveAction => CoreResources.SyncAction;

        public static string AccountUnavailableStatus { get; } =
            CottonCloudToDeviceSyncStatusText.AccountUnavailableStatus;

        public static string OfflineUnavailableStatus { get; } =
            CottonCloudToDeviceSyncStatusText.OfflineUnavailableStatus;

        public static string CancelledStatus { get; } =
            CottonCloudToDeviceSyncStatusText.CancelledStatus;

        public static string FailedStatus { get; } =
            CottonCloudToDeviceSyncStatusText.FailedStatus;

        public static string CreateStartingStatus(string folderName)
        {
            string name = string.IsNullOrWhiteSpace(folderName)
                ? CoreResources.DefaultFolderNameLower
                : folderName.Trim();
            return CoreResources.Format(CoreResources.SyncingBothWaysFormat, name);
        }

        public static string CreateConfirmDestructiveMessage(int localDeleteCount, int remoteDeleteCount)
        {
            if (localDeleteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(localDeleteCount), "Local delete count cannot be negative.");
            }

            if (remoteDeleteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remoteDeleteCount), "Remote delete count cannot be negative.");
            }

            List<string> parts = [];
            AddConfirmAction(
                parts,
                localDeleteCount,
                CoreResources.RemoveOneLocalFile,
                CoreResources.Format(CoreResources.RemoveLocalFilesFormat, localDeleteCount));
            AddConfirmAction(
                parts,
                remoteDeleteCount,
                CoreResources.TrashOneCloudFile,
                CoreResources.Format(CoreResources.TrashCloudFilesFormat, remoteDeleteCount));

            return parts.Count == 0
                ? CoreResources.NoDestructiveChanges
                : CoreResources.Format(CoreResources.DestructiveChangesFormat, string.Join(" and ", parts));
        }

        public static string CreateCompletedStatus(CottonBidirectionalSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            if (summary.RootCount == 0)
            {
                return CoreResources.NoBidirectionalFolders;
            }

            List<string> parts = [];
            AddCount(parts, summary.DownloadedCount, CoreResources.DownloadedLabel);
            AddCount(parts, summary.RefreshedLocalCount, CoreResources.RefreshedLocallyLabel);
            AddCount(parts, summary.RenamedLocalCount, CoreResources.RenamedLocallyLabel);
            AddCount(parts, summary.RemovedLocalCount, CoreResources.RemovedLocallyLabel);
            AddCount(parts, summary.UploadedCount, CoreResources.UploadedLabel);
            AddCount(parts, summary.RefreshedRemoteCount, CoreResources.UpdatedInCloudLabel);
            AddCount(parts, summary.CreatedFolderCount, CoreResources.FolderCreatedSingular, CoreResources.FolderCreatedPlural);
            AddCount(parts, summary.DeletedRemoteFileCount, CoreResources.RemoteFileRemovedSingular, CoreResources.RemoteFileRemovedPlural);
            AddCount(parts, summary.RemovedManifestCount, CoreResources.RecordCleanedSingular, CoreResources.RecordCleanedPlural);
            AddCount(parts, summary.ConflictReviewCount, CoreResources.ConflictReviewSingular, CoreResources.ConflictReviewPlural);
            AddCount(
                parts,
                summary.DestructiveReviewLocalDeleteCount,
                CoreResources.LocalRemovalReviewSingular,
                CoreResources.LocalRemovalReviewPlural);
            AddCount(
                parts,
                summary.DestructiveReviewRemoteDeleteCount,
                CoreResources.CloudRemovalReviewSingular,
                CoreResources.CloudRemovalReviewPlural);
            AddCount(parts, summary.BlockedItemCount, CoreResources.BlockedLabel);
            AddRootCount(parts, summary.SkippedRootCount);

            if (parts.Count == 0)
            {
                return CoreResources.BidirectionalSyncCurrent;
            }

            return CoreResources.Format(
                CoreResources.BidirectionalSyncCompletedFormat,
                string.Join(", ", parts));
        }

        private static void AddCount(List<string> parts, int count, string label)
        {
            if (count <= 0)
            {
                return;
            }

            parts.Add($"{count} {label}");
        }

        private static void AddConfirmAction(List<string> parts, int count, string singularLabel, string pluralLabel)
        {
            if (count <= 0)
            {
                return;
            }

            parts.Add(count == 1 ? singularLabel : pluralLabel);
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
