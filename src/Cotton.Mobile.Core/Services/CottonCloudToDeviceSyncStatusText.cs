// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonCloudToDeviceSyncStatusText
    {
        public static string ActionLabel => CoreResources.SyncToDeviceAction;

        public static string ChooseFolderActionLabel => CoreResources.SyncToFolderAction;

        public static string StartingAllStatus => CoreResources.SyncingFolders;

        public static string AccountUnavailableStatus => CoreResources.AccountSessionRequired;

        public static string OfflineUnavailableStatus => CoreResources.SyncOffline;

        public static string CancelledStatus => CoreResources.SyncCancelled;

        public static string FailedStatus => CoreResources.SyncFailed;

        public static string CreateStartingStatus(string folderName)
        {
            return CoreResources.Format(
                CoreResources.SyncingFolderFormat,
                NormalizeFolderName(folderName));
        }

        public static string CreateCompletedStatus(CottonCloudToDeviceSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            if (summary.RootCount == 0)
            {
                return CoreResources.NoSyncFolders;
            }

            List<string> parts = [];
            AddCount(parts, summary.DownloadedCount, CoreResources.DownloadedLabel);
            AddCount(parts, summary.RefreshedCount, CoreResources.RefreshedLabel);
            AddCount(parts, summary.RenamedCount, CoreResources.RenamedLabel);
            AddCount(parts, summary.RemovedCount, CoreResources.RemovedLabel);
            AddCount(parts, summary.BlockedItemCount, CoreResources.BlockedLabel);
            AddRootCount(parts, summary.SkippedRootCount);

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

        private static string NormalizeFolderName(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName) ? CoreResources.DefaultFolderName : folderName.Trim();
        }
    }
}
