namespace Cotton.Mobile.Services
{
    public static class CottonRecentFileRemoveStatusText
    {
        public const string AlreadyRemovedStatus = "Recent file was already removed.";

        public const string FailedStatus = "Could not remove recent file.";

        public static string CreateRemovingStatus(string fileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            return $"Removing {fileName.Trim()}...";
        }

        public static string CreateRemovedStatus(string fileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            return $"Removed {fileName.Trim()} from Recent files.";
        }
    }
}
