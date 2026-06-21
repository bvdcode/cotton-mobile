namespace Cotton.Mobile.Services
{
    public static class CottonFolderTrashStatusText
    {
        public const string CancelledStatus = "Move to trash cancelled.";

        public const string ConfirmAction = "Move to trash";

        public const string ConfirmTitle = "Move folder to trash?";

        public const string FailedStatus = "Could not move folder to trash.";

        public const string OfflineUnavailableStatus = "Offline. Move to trash needs internet.";

        public const string TimedOutStatus = "Move to trash is taking longer than expected. Refresh and try again.";

        public static string CreateConfirmMessage(string folderName)
        {
            string name = NormalizeFolderName(folderName);
            return $"{name} and its contents will be removed from this folder and can be restored from trash.";
        }

        public static string CreateMovedStatus(string folderName)
        {
            string name = NormalizeFolderName(folderName);
            return $"{name} moved to trash.";
        }

        public static string CreateMovingStatus(string folderName)
        {
            string name = NormalizeFolderName(folderName);
            return $"Moving {name} to trash...";
        }

        private static string NormalizeFolderName(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName) ? "Folder" : folderName.Trim();
        }
    }
}
