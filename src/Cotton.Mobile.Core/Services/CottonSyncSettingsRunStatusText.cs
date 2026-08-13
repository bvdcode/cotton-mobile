// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonSyncSettingsRunStatusText
    {
        public static string StartingAllStatus => CoreResources.SyncingFolders;

        public static string OfflineUnavailableStatus { get; } =
            CottonDeviceToCloudSyncStatusText.OfflineUnavailableStatus;

        public static string FailedStatus { get; } =
            CottonDeviceToCloudSyncStatusText.FailedStatus;

        public static string CreateCompletedStatus(CottonDeviceToCloudSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);
            if (summary.RootCount == 0)
            {
                return CoreResources.NoSyncFolders;
            }

            List<string> parts = [];
            AddDeviceToCloudCounts(parts, summary);
            AddCount(parts, summary.CreatedFolderCount, CoreResources.FolderCreatedSingular, CoreResources.FolderCreatedPlural);
            AddCount(parts, summary.BlockedItemCount, CoreResources.BlockedLabel);
            AddRootCount(parts, summary.SkippedRootCount);

            if (parts.Count == 0)
            {
                return CoreResources.SyncCurrent;
            }

            return CoreResources.Format(CoreResources.SyncCompletedFormat, string.Join(", ", parts));
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
