// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonDeviceToCloudSyncStatusText
    {
        public static string ActionLabel => CoreResources.UploadNewFilesAction;

        public static string OfflineUnavailableStatus => CoreResources.SyncOffline;

        public static string FailedStatus => CoreResources.SyncFailed;

        public static string CreateStartingStatus(string folderName)
        {
            return CoreResources.Format(
                CoreResources.UploadingFolderFormat,
                NormalizeFolderName(folderName));
        }

        public static string CreateCompletedStatus(CottonDeviceToCloudSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            return CottonSyncSettingsRunStatusText.CreateCompletedStatus(summary);
        }

        private static string NormalizeFolderName(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName) ? CoreResources.DefaultFolderName : folderName.Trim();
        }
    }
}
