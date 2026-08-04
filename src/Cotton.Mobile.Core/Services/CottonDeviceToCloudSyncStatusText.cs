// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonDeviceToCloudSyncStatusText
    {
        private const string DefaultFolderName = "Folder";

        public const string ActionLabel = "Upload new files";

        public static string OfflineUnavailableStatus { get; } =
            CottonCloudToDeviceSyncStatusText.OfflineUnavailableStatus;

        public static string FailedStatus { get; } =
            CottonCloudToDeviceSyncStatusText.FailedStatus;

        public static string UnsupportedDirectionStatus { get; } =
            "Sync root is not configured to upload new files.";

        public static string CreateStartingStatus(string folderName)
        {
            return $"Uploading new files from {NormalizeFolderName(folderName)}...";
        }

        public static string CreateCompletedStatus(CottonDeviceToCloudSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            return CottonSyncSettingsRunStatusText.CreateCompletedStatus(
                new CottonCloudToDeviceSyncRunSummary([]),
                summary);
        }

        private static string NormalizeFolderName(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName) ? DefaultFolderName : folderName.Trim();
        }
    }
}
