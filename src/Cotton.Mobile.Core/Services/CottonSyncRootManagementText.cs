// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonSyncRootManagementText
    {
        public static string StopAction => CoreResources.StopSyncingAction;
        public static string PauseAction => CoreResources.PauseAction;
        public static string ResumeAction => CoreResources.ResumeAction;
        public static string CancelAction => CoreResources.CancelAction;
        public static string StopMessage => CoreResources.StopSyncingMessage;
        public static string PausedStatusText => CoreResources.PausedStatus;
        public static string RootPausedStatus => CoreResources.RootPausedStatus;
        public static string RootMissingStatus => CoreResources.RootMissingStatus;
        public static string PauseFailedStatus => CoreResources.PauseFailedStatus;
        public static string ResumeFailedStatus => CoreResources.ResumeFailedStatus;
        public static string StopFailedStatus => CoreResources.StopFailedStatus;

        public static string CreateStopTitle(string folderName)
        {
            return CoreResources.Format(CoreResources.StopSyncingTitleFormat, NormalizeFolderName(folderName));
        }

        public static string CreateStoppedStatus(string folderName)
        {
            return CoreResources.Format(CoreResources.StoppedSyncingFormat, NormalizeFolderName(folderName));
        }

        public static string CreatePausedStatus(string folderName)
        {
            return CoreResources.Format(CoreResources.PausedSyncingFormat, NormalizeFolderName(folderName));
        }

        public static string CreateResumedStatus(string folderName)
        {
            return CoreResources.Format(CoreResources.ResumedSyncingFormat, NormalizeFolderName(folderName));
        }

        private static string NormalizeFolderName(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName)
                ? CoreResources.DefaultFolderReference
                : folderName.Trim();
        }
    }
}
