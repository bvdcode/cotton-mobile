namespace Cotton.Mobile.Services
{
    public static class CottonFolderCreationStatusText
    {
        public const string OfflineStatus = "Offline. New folder needs internet.";
        public const string CancelledStatus = "New folder cancelled.";
        public const string FailedStatus = "Could not create folder.";
        public const string DuplicateStatus = "An item with that name already exists.";

        public static string CreateCreatingStatus(string folderName)
        {
            return $"Creating {Normalize(folderName)}...";
        }

        public static string CreateCreatedStatus(string folderName)
        {
            return $"Created folder {Normalize(folderName)}.";
        }

        private static string Normalize(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName) ? "folder" : folderName.Trim();
        }
    }
}
