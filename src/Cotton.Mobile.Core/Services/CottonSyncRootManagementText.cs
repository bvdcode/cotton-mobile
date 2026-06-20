namespace Cotton.Mobile.Services
{
    public static class CottonSyncRootManagementText
    {
        public const string StopAction = "Stop syncing";
        public const string PauseAction = "Pause";
        public const string ResumeAction = "Resume";
        public const string CancelAction = "Cancel";
        public const string StopMessage = "This stops future sync for this folder. Files already on this device are not deleted.";
        public const string PausedStatusText = "Paused";
        public const string RootPausedStatus = "Sync paused. Resume this folder first.";
        public const string RootMissingStatus = "Sync folder is no longer configured.";
        public const string PauseFailedStatus = "Could not pause syncing this folder.";
        public const string ResumeFailedStatus = "Could not resume syncing this folder.";
        public const string StopFailedStatus = "Could not stop syncing this folder.";

        public static string CreateStopTitle(string folderName)
        {
            return $"Stop syncing {NormalizeFolderName(folderName)}?";
        }

        public static string CreateStoppedStatus(string folderName)
        {
            return $"Stopped syncing {NormalizeFolderName(folderName)}.";
        }

        public static string CreatePausedStatus(string folderName)
        {
            return $"Paused syncing {NormalizeFolderName(folderName)}.";
        }

        public static string CreateResumedStatus(string folderName)
        {
            return $"Resumed syncing {NormalizeFolderName(folderName)}.";
        }

        private static string NormalizeFolderName(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName)
                ? "this folder"
                : folderName.Trim();
        }
    }
}
